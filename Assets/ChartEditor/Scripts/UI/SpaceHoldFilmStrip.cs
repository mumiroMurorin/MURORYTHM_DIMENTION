using System;
using System.Collections.Generic;
using TMPro;
using UndoRedo.Notes;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// 頂点レイヤーの下の、フィルムストリップ (SpaceHold の節を、時間の流れに沿って並べた帯)
    ///
    /// SpaceHold の節の頂点を編集している間、同じ SpaceHold の節 (始点・中継点・メッシュ中継点・終点) の形を、
    /// 小さな絵 (サムネイル) にして、時間の流れ (タイムライン) に沿って並べる。今編集している節は、青い枠で示す。
    /// ・帯の下の目盛り: 分線ごとに線を引き、節のある分線には「#1-8」(1小節目の8番の分線) のように書く。小節の頭の線は太い。
    ///   節の無い分線は、細く詰めて並べる (長い SpaceHold でも、帯が長くなりすぎないように)
    /// ・サムネイルを押す: その節の頂点を編集する
    /// ・サムネイルを左右にドラッグ: その節の時刻を、離した所の分線へずらす (前後の節は追い越さない)
    /// ・節の無い分線の「+」を押す: その分線に、補間した形 (今の帯の形のまま) のメッシュ中継点を足す
    /// (この SpaceHold を3D編集で開く「3Dで編集」のボタンは、ミニプレビューの「プレビュー」のボタンの横にある: SpaceHold3DOpenButtonView)
    /// ・ホイール・下のスクロールバー: 帯を左右に送る
    /// ・背景をダブルクリック: 小さいマスにしまう (マスを押すと戻る。ほかの欄と同じ MinimizableWindow)
    /// どの操作も、元に戻せる (1回の操作が、1回ぶん)。
    ///
    /// 帯の枠 (キャンバス・見出し・並べる場所・スクロールバー・しまったときのマス) は、シーンに置いてある (SpaceHoldFilmStripView)。
    /// サムネイル・「+」・目盛りは、数が節の並びで変わるので、シーンのひな形を複製して作る。
    /// </summary>
    public class SpaceHoldFilmStrip : ITickable, IDisposable
    {
        // 帯の大きさと位置は、シーンに置いてある (940 × 166、画面の下のまんなか、下から 8)
        // (録音 20261117_033817: 背を低くした (204 → 166)。頂点レイヤーの円が、帯の後ろに隠れないように)
        // 目盛り (分線の名前) の高さ
        const float RULER_HEIGHT = 18f;
        // サムネイル1つぶん (大きさ 80 × 84 は、シーンのひな形に書いてある)
        const float ITEM_WIDTH = 80f;
        const float ITEM_HEIGHT = 84f;
        const float ITEM_TOP = 4f;
        // 分線1本ぶんの幅 (節のある分線は、節の数 × SLOT、節の無い分線は EMPTY_SLOT)
        const float ITEM_SLOT_WIDTH = 92f;
        const float EMPTY_SLOT_WIDTH = 28f;
        const float CONTENT_PADDING = 10f;

        static readonly Color itemColor = new Color(0.19f, 0.19f, 0.22f, 1f);
        static readonly Color itemHoverColor = new Color(0.26f, 0.26f, 0.30f, 1f);
        static readonly Color itemEditingColor = new Color(0.22f, 0.46f, 0.85f, 1f);
        static readonly Color dragLabelColor = new Color(1f, 0.85f, 0.35f, 1f);
        static readonly Color tickColor = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color barTickColor = new Color(1f, 1f, 1f, 0.42f);
        // 目盛りの名前の色 (小節の頭でないもの)
        static readonly Color subTextColor = new Color(0.72f, 0.72f, 0.76f, 1f);

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;

        // シーンに置いてある画面部品
        SpaceHoldFilmStripView view;
        Canvas canvas;
        TextMeshProUGUI titleLabel;
        RectTransform viewport;
        RectTransform content;
        Image dropHighlight;
        readonly List<FilmStripItem> items = new List<FilmStripItem>();
        readonly List<EditorUiButton> plusButtons = new List<EditorUiButton>();
        readonly List<int> plusLines = new List<int>();
        readonly List<Image> ticks = new List<Image>();
        readonly List<TextMeshProUGUI> rulerLabels = new List<TextMeshProUGUI>();

        readonly List<NoteData_SpaceHold> chain = new List<NoteData_SpaceHold>();
        readonly List<SpaceHoldSection> sections = new List<SpaceHoldSection>();
        NoteData_SpaceHold shownEditing;
        bool isBuildFailed;

        // 並べ方 (分線ごとの、左の位置と幅)。最初の分線から最後の分線まで
        int firstLine;
        int lastLine;
        readonly List<float> slotX = new List<float>();
        readonly List<float> slotWidths = new List<float>();
        int layoutSignature;

        // ドラッグしているサムネイル (していないときは -1) と、動かす先の分線
        int dragIndex = -1;
        int dragTargetLine = -1;

        public SpaceHoldFilmStrip(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        void IDisposable.Dispose()
        {
            // (画面部品はシーンのものなので、消さない)
            view = null;
            canvas = null;
        }

        void ITickable.Tick()
        {
            if (isBuildFailed) { return; }

            try
            {
                if (view == null && !Connect()) { return; }
                Refresh();
            }
            catch (Exception exception)
            {
                // 失敗し続けてログがあふれないよう、つなげなかったときは、それ以降は何もしない
                Debug.LogException(exception);
                if (view == null) { isBuildFailed = true; }
            }
        }

        #region 表示

        private void Refresh()
        {
            var editing = notesGetter.EditingVertices.Value as NoteData_SpaceHold;
            var chartData = dataGetter.ChartData.Value;
            bool isVisible =
                editing != null &&
                chartData != null &&
                !dataGetter.IsLoadingChart.Value &&
                dataGetter.EditNoteType.Value == EditNoteType.Vertices &&
                !SpaceHold3DEditor.IsOpen &&
                notesGetter.GetNoteObject(editing) != null;

            var timeline = isVisible ? SpaceHoldTimeline.Get(chartData) : null;
            if (isVisible)
            {
                SpaceHoldSections.GetChain(notesGetter, editing, chain);
                isVisible = SpaceHoldSections.TryBuildSections(timeline, chain, sections) && sections.Count > 0;
            }

            if (canvas.gameObject.activeSelf != isVisible) { canvas.gameObject.SetActive(isVisible); }
            if (!isVisible)
            {
                dragIndex = -1;
                shownEditing = null;
                return;
            }

            // 節の並び (分線・種類) が変わったときだけ、並べ直す
            int signature = ComputeLayoutSignature();
            if (signature != layoutSignature)
            {
                layoutSignature = signature;
                Relayout(timeline);
            }

            int editingIndex = chain.IndexOf(editing);
            for (int i = 0; i < sections.Count; i++)
            {
                SpaceHoldSection section = sections[i];
                FilmStripItem item = items[i];
                item.Index = i;

                item.SetShape(section.Vertices, GetTypeColor(section.Note.NoteType));
                item.SetBackground(i == editingIndex ? itemEditingColor : item.IsHovered ? itemHoverColor : itemColor);

                bool isDragging = i == dragIndex && dragTargetLine >= 0 && dragTargetLine != section.Line;
                item.SetLabel(isDragging ? $"→ {FormatLine(timeline, dragTargetLine)}" : GetTypeShortName(section.Note.NoteType),
                              isDragging ? dragLabelColor : EditorUiButton.TextColor);
            }

            RefreshDropHighlight();

            titleLabel.text = chain.Count <= 1
                ? "SpaceHold の断面 (どこにもつながっていません)"
                : $"SpaceHold の断面  {editingIndex + 1} / {chain.Count}";

            // 編集する節が替わったら、その節が見えるところまで送る
            if (!ReferenceEquals(shownEditing, editing))
            {
                shownEditing = editing;
                ScrollTo(editingIndex);
            }
        }

        private int ComputeLayoutSignature()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + sections.Count;
                foreach (var section in sections)
                {
                    hash = hash * 31 + section.Line;
                    hash = hash * 31 + (int)section.Note.NoteType;
                    hash = hash * 31 + section.Note.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// 分線ごとの位置を決めて、サムネイル・目盛り・「+」を置き直す
        /// </summary>
        private void Relayout(SpaceHoldTimeline timeline)
        {
            firstLine = sections[0].Line;
            lastLine = sections[sections.Count - 1].Line;

            // 分線ごとの節の数
            int slotCount = lastLine - firstLine + 1;
            var counts = new int[slotCount];
            foreach (var section in sections) { counts[section.Line - firstLine]++; }

            slotX.Clear();
            slotWidths.Clear();
            float x = CONTENT_PADDING;
            for (int i = 0; i < slotCount; i++)
            {
                float width = counts[i] > 0 ? counts[i] * ITEM_SLOT_WIDTH : EMPTY_SLOT_WIDTH;
                slotX.Add(x);
                slotWidths.Add(width);
                x += width;
            }
            content.sizeDelta = new Vector2(x + CONTENT_PADDING, 0f);

            // 目盛りの線 (サムネイルより奥に描く)
            EnsureCount(ticks, slotCount, CreateTick);
            for (int i = 0; i < ticks.Count; i++)
            {
                bool isUsed = i < slotCount;
                SetActive(ticks[i].gameObject, isUsed);
                if (!isUsed) { continue; }

                bool isBarHead = timeline.GetSub(firstLine + i) == 0;
                var rect = ticks[i].rectTransform;
                rect.anchoredPosition = new Vector2(slotX[i], 0f);
                rect.sizeDelta = new Vector2(isBarHead ? 2f : 1f, 0f);
                ticks[i].color = isBarHead ? barTickColor : tickColor;
            }

            // 目盛りの名前 (節のある分線と、節の無い小節の頭)
            int labelCount = 0;
            for (int i = 0; i < slotCount; i++)
            {
                int line = firstLine + i;
                bool isBarHead = timeline.GetSub(line) == 0;
                if (counts[i] == 0 && !isBarHead) { continue; }

                if (labelCount >= rulerLabels.Count) { rulerLabels.Add(CreateRulerLabel()); }
                var label = rulerLabels[labelCount++];
                SetActive(label.gameObject, true);
                label.text = counts[i] > 0 ? FormatLine(timeline, line) : $"#{timeline.GetBar(line) + 1}";
                label.color = isBarHead ? new Color(1f, 1f, 1f, 0.85f) : subTextColor;
                label.rectTransform.anchoredPosition = new Vector2(slotX[i] + 3f, 1f);
                label.rectTransform.sizeDelta = new Vector2(Mathf.Max(slotWidths[i] - 4f, 40f), RULER_HEIGHT - 2f);
            }
            for (int i = labelCount; i < rulerLabels.Count; i++) { SetActive(rulerLabels[i].gameObject, false); }

            // サムネイル (同じ分線に2つ以上あるときは、横に並べる)
            EnsureCount(items, sections.Count, () => CreateItem(items.Count));
            var placedOnLine = new int[slotCount];
            for (int i = 0; i < items.Count; i++)
            {
                bool isUsed = i < sections.Count;
                SetActive(items[i].gameObject, isUsed);
                if (!isUsed) { continue; }

                int slot = sections[i].Line - firstLine;
                float itemX = slotX[slot] + placedOnLine[slot] * ITEM_SLOT_WIDTH + (ITEM_SLOT_WIDTH - ITEM_WIDTH) * 0.5f;
                placedOnLine[slot]++;
                ((RectTransform)items[i].transform).anchoredPosition = new Vector2(itemX, -ITEM_TOP);
            }

            // 節の無い分線の「+」
            plusLines.Clear();
            for (int i = 0; i < slotCount; i++)
            {
                if (counts[i] == 0) { plusLines.Add(firstLine + i); }
            }
            EnsureCount(plusButtons, plusLines.Count, () => CreatePlusButton(plusButtons.Count));
            for (int i = 0; i < plusButtons.Count; i++)
            {
                bool isUsed = i < plusLines.Count;
                SetActive(plusButtons[i].gameObject, isUsed);
                if (!isUsed) { continue; }

                int slot = plusLines[i] - firstLine;
                plusButtons[i].RectTransform.anchoredPosition = new Vector2(slotX[slot] + slotWidths[slot] * 0.5f, -(ITEM_TOP + ITEM_HEIGHT * 0.5f));
            }

            // 線を奥、サムネイルと「+」を手前に
            foreach (var tick in ticks) { tick.transform.SetAsFirstSibling(); }
            dropHighlight.transform.SetAsFirstSibling();
        }

        private void ScrollTo(int index)
        {
            if (index < 0 || index >= items.Count) { return; }

            float viewWidth = viewport.rect.width;
            float contentWidth = content.sizeDelta.x;
            if (contentWidth <= viewWidth)
            {
                content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);
                return;
            }

            float itemLeft = ((RectTransform)items[index].transform).anchoredPosition.x;
            float scroll = -content.anchoredPosition.x;
            if (itemLeft < scroll || itemLeft + ITEM_WIDTH > scroll + viewWidth)
            {
                scroll = Mathf.Clamp(itemLeft + ITEM_WIDTH * 0.5f - viewWidth * 0.5f, 0f, contentWidth - viewWidth);
                content.anchoredPosition = new Vector2(-scroll, content.anchoredPosition.y);
            }
        }

        /// <summary>
        /// 分線の、帯の中での左の位置と幅 (帯に並んでいない分線は、節の無い分線の幅で、端から先へ伸ばして求める)
        /// </summary>
        private void GetLineRange(int line, out float x, out float width)
        {
            int slot = line - firstLine;
            if (slot >= 0 && slot < slotX.Count)
            {
                x = slotX[slot];
                width = slotWidths[slot];
                return;
            }

            width = EMPTY_SLOT_WIDTH;
            x = slot < 0
                ? CONTENT_PADDING + slot * EMPTY_SLOT_WIDTH
                : slotX[slotX.Count - 1] + slotWidths[slotWidths.Count - 1] + (slot - slotX.Count) * EMPTY_SLOT_WIDTH;
        }

        /// <summary>
        /// 帯の中の位置にある分線
        /// </summary>
        private int GetLineAt(float x)
        {
            if (slotX.Count == 0) { return firstLine; }

            if (x < slotX[0]) { return firstLine - Mathf.CeilToInt((slotX[0] - x) / EMPTY_SLOT_WIDTH); }

            for (int i = 0; i < slotX.Count; i++)
            {
                if (x < slotX[i] + slotWidths[i]) { return firstLine + i; }
            }

            float end = slotX[slotX.Count - 1] + slotWidths[slotWidths.Count - 1];
            return lastLine + 1 + Mathf.FloorToInt((x - end) / EMPTY_SLOT_WIDTH);
        }

        /// <summary>
        /// ドラッグしている間、動かす先の分線に色を付ける
        /// </summary>
        private void RefreshDropHighlight()
        {
            bool isShown = dragIndex >= 0 && dragIndex < sections.Count && dragTargetLine >= 0 && dragTargetLine != sections[dragIndex].Line;
            SetActive(dropHighlight.gameObject, isShown);
            if (!isShown) { return; }

            GetLineRange(dragTargetLine, out float x, out float width);
            dropHighlight.rectTransform.anchoredPosition = new Vector2(x, 0f);
            dropHighlight.rectTransform.sizeDelta = new Vector2(width, 0f);
        }

        /// <summary>
        /// 分線の場所の表示 (「#12-3」 = 12小節目の3番の分線。ログの書き方と同じ)
        /// </summary>
        public static string FormatLine(SpaceHoldTimeline timeline, int line)
        {
            if (timeline == null || line < 0 || line >= timeline.LineCount) { return "-"; }
            return $"#{timeline.GetBar(line) + 1}-{timeline.GetSub(line)}";
        }

        public static string GetTypeShortName(DeploymentNoteType type)
        {
            switch (type)
            {
                case DeploymentNoteType.SpaceHoldStart: return "始点";
                case DeploymentNoteType.SpaceHoldRelay: return "中継";
                case DeploymentNoteType.SpaceHoldMeshRelay: return "メッシュ";
                case DeploymentNoteType.SpaceHoldEnd: return "終点";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 節の種類ごとの色 (始点: 緑、中継点: 水色、メッシュ中継点: 灰色、終点: 赤)
        /// </summary>
        public static Color GetTypeColor(DeploymentNoteType type)
        {
            switch (type)
            {
                case DeploymentNoteType.SpaceHoldStart: return new Color(0.45f, 0.92f, 0.55f, 1f);
                case DeploymentNoteType.SpaceHoldRelay: return new Color(0.45f, 0.78f, 1f, 1f);
                case DeploymentNoteType.SpaceHoldMeshRelay: return new Color(0.72f, 0.72f, 0.78f, 1f);
                case DeploymentNoteType.SpaceHoldEnd: return new Color(1f, 0.52f, 0.45f, 1f);
                default: return Color.white;
            }
        }

        #endregion

        #region 操作

        private void OnItemClicked(int index)
        {
            if (index < 0 || index >= sections.Count) { return; }

            notesSetter.SetEditingVertices(sections[index].Note);
        }

        private void OnItemBeginDrag(int index, Vector2 screenPosition)
        {
            if (index < 0 || index >= sections.Count) { return; }
            if (!IsOperable()) { return; }
            // ロックした節は、時刻をずらさない
            if (NoteLock.IsLocked(sections[index].Note))
            {
                NoteLock.LogRefused(1, "動かし");
                return;
            }

            dragIndex = index;
            dragTargetLine = sections[index].Line;

            // ドラッグした節を編集する節にする
            notesSetter.SetEditingVertices(sections[index].Note);
        }

        private void OnItemDrag(int index, Vector2 screenPosition)
        {
            if (index != dragIndex || index >= sections.Count) { return; }

            var timeline = SpaceHoldTimeline.Get(dataGetter.ChartData.Value);
            if (timeline == null) { return; }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPosition, null, out Vector2 local)) { return; }

            // (帯の中の位置は、左端が 0。帯の基準点は左端にしてある)
            int line = GetLineAt(local.x);
            GetMovableRange(timeline, index, out int first, out int last);
            dragTargetLine = Mathf.Clamp(line, first, last);
        }

        private void OnItemEndDrag(int index)
        {
            if (index != dragIndex) { return; }

            int target = dragTargetLine;
            dragIndex = -1;
            dragTargetLine = -1;

            if (index >= sections.Count || target < 0 || target == sections[index].Line) { return; }
            if (!IsOperable()) { return; }

            var timeline = SpaceHoldTimeline.Get(dataGetter.ChartData.Value);
            if (timeline == null) { return; }

            NoteData_SpaceHold note = sections[index].Note;
            var previous = new List<NoteDataToAddress> { new NoteDataToAddress(note, note.Address) };
            var current = new List<NoteDataToAddress> { new NoteDataToAddress(note, timeline.CreateSpaceAddress(target)) };
            if (NotesMoveRecord.RecordNotesMovingIfChanged(previous, current))
            {
                Debug.Log($"【フィルムストリップ】節を {FormatLine(timeline, sections[index].Line)} から {FormatLine(timeline, target)} へ動かしました");
            }
        }

        /// <summary>
        /// 節を動かせる分線の範囲 (前後の節は追い越さない。始点・終点は、譜面の端まで)
        /// </summary>
        private void GetMovableRange(SpaceHoldTimeline timeline, int index, out int first, out int last)
        {
            int line = sections[index].Line;
            first = index > 0 ? Mathf.Min(line, sections[index - 1].Line + 1) : 0;
            last = index + 1 < sections.Count ? Mathf.Max(line, sections[index + 1].Line - 1) : timeline.LineCount - 1;
        }

        /// <summary>
        /// 節の無い分線に、補間した形のメッシュ中継点を足す
        /// </summary>
        private void OnPlusClicked(int buttonIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= plusLines.Count) { return; }
            if (!IsOperable()) { return; }

            var chartData = dataGetter.ChartData.Value;
            var timeline = SpaceHoldTimeline.Get(chartData);
            if (timeline == null) { return; }

            int line = plusLines[buttonIndex];
            if (!TryInsertSectionAt(chartData, timeline, sections, line, out NoteData_SpaceHold inserted)) { return; }

            notesSetter.SetEditingVertices(inserted);
            Debug.Log($"【フィルムストリップ】{FormatLine(timeline, line)} に、補間した形のメッシュ中継点を足しました");
        }

        /// <summary>
        /// 節 gapIndex と、その次の節の間のまんなかの分線に、補間した形のメッシュ中継点を足す (元に戻せる)
        /// (3D編集の右クリックのメニュー「この節の後ろに節を足す」から使う)
        /// </summary>
        public static bool TryInsertInterpolatedSection(ChartData chartData, SpaceHoldTimeline timeline, IReadOnlyList<SpaceHoldSection> sections, int gapIndex, out NoteData_SpaceHold inserted)
        {
            inserted = null;
            if (chartData == null || timeline == null || gapIndex < 0 || gapIndex + 1 >= sections.Count) { return false; }

            SpaceHoldSection a = sections[gapIndex];
            SpaceHoldSection b = sections[gapIndex + 1];
            if (b.Line - a.Line < 2)
            {
                Debug.Log("【SpaceHold】2つの節の間に分線が無いので、節を足せません");
                return false;
            }

            int line = timeline.FindNearestLineByDepth((a.Depth + b.Depth) * 0.5f, a.Line + 1, b.Line - 1);
            return TryInsertSectionAt(chartData, timeline, sections, line, out inserted);
        }

        /// <summary>
        /// 分線 line に、補間した形のメッシュ中継点を足す (元に戻せる)。SpaceHold の始点と終点の間の、節の無い分線であること
        /// </summary>
        public static bool TryInsertSectionAt(ChartData chartData, SpaceHoldTimeline timeline, IReadOnlyList<SpaceHoldSection> sections, int line, out NoteData_SpaceHold inserted)
        {
            inserted = null;
            if (chartData == null || timeline == null || sections == null || sections.Count < 2) { return false; }
            if (line <= sections[0].Line || line >= sections[sections.Count - 1].Line) { return false; }

            int chainIndex = sections[0].Note.ChainIndex.Value;
            if (chainIndex < 0) { return false; }

            if (!SpaceHoldSections.TryInterpolate(sections, timeline.GetTime(line), timeline.GetDepth(line), out Vector2[] vertices)) { return false; }

            var note = new NoteData_SpaceHold();
            note.SetAddress(timeline.CreateSpaceAddress(line));
            note.SetNoteType(DeploymentNoteType.SpaceHoldMeshRelay);
            note.SpaceVertices.SetVertices(vertices);
            note.SetChainIndex(chainIndex);

            Record(() => chartData.AddNote(note), () => chartData.RemoveNote(note, false));

            inserted = note;
            return true;
        }

        private bool IsOperable()
        {
            return !dataGetter.IsLoadingChart.Value && !EditorMenuBar.IsBlockingChartInput && !InputFieldFocus.IsTyping;
        }

        #endregion

        #region 画面部品 (シーンの枠につなぐ・ひな形を複製する)

        /// <summary>
        /// シーンに置いてある帯の枠を探す (最初の1回だけ)。見つからないときは false (それ以降は何もしない)
        /// (しまう・戻すは、シーンの MinimizableWindow がする。帯の背景のダブルクリックで、小さいマスにしまう)
        /// </summary>
        private bool Connect()
        {
            var found = UnityEngine.Object.FindObjectOfType<SpaceHoldFilmStripView>(true);
            if (found == null || found.Canvas == null || found.Content == null || found.ItemTemplate == null || found.PlusTemplate == null ||
                found.TickTemplate == null || found.RulerLabelTemplate == null || found.DropHighlight == null ||
                found.TitleLabel == null || found.Viewport == null)
            {
                Debug.LogWarning("【フィルムストリップ】シーンに SpaceHoldFilmStripView がありません (フィルムストリップは出ません)");
                isBuildFailed = true;
                return false;
            }

            view = found;
            canvas = found.Canvas;
            titleLabel = found.TitleLabel;
            viewport = found.Viewport;
            content = found.Content;
            dropHighlight = found.DropHighlight;
            return true;
        }

        private Image CreateTick()
        {
            var tick = UnityEngine.Object.Instantiate(view.TickTemplate, content);
            tick.name = "Tick";
            return tick;
        }

        private TextMeshProUGUI CreateRulerLabel()
        {
            var label = UnityEngine.Object.Instantiate(view.RulerLabelTemplate, content);
            label.name = "RulerLabel";
            return label;
        }

        private FilmStripItem CreateItem(int index)
        {
            var item = UnityEngine.Object.Instantiate(view.ItemTemplate, content);
            item.name = $"Item{index}";
            item.Index = index;
            item.OnClicked = OnItemClicked;
            item.OnBeginDragged = OnItemBeginDrag;
            item.OnDragged = OnItemDrag;
            item.OnEndDragged = OnItemEndDrag;
            return item;
        }

        private EditorUiButton CreatePlusButton(int buttonIndex)
        {
            var button = UnityEngine.Object.Instantiate(view.PlusTemplate, content);
            button.name = $"Plus{buttonIndex}";
            button.SetOnClick(() => OnPlusClicked(buttonIndex));
            return button;
        }

        private static void EnsureCount<T>(List<T> list, int count, Func<T> create)
        {
            while (list.Count < count) { list.Add(create()); }
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target.activeSelf != isActive) { target.SetActive(isActive); }
        }

        #endregion
    }
}
