using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    /// <summary>
    /// メニューやクイックメニューの中に表示する、設定の今の値の種類
    /// (番号はシーンに保存されているので、後から変えない)
    /// </summary>
    public enum EditorMenuValue
    {
        NoteSpeed = 1,
        ScrollSensitivity = 2,
        HitVolume = 3,
        MusicVolume = 4,
        BackupInterval = 5,
        VisibleDistance = 6,
        CurveRadius = 7,
    }

    /// <summary>
    /// 画面上のメニューバー (シーンの MenuCanvas) とクイックメニュー (編集画面の左下の設定欄) の項目と、譜面エディタの機能をつなぐ
    ///
    /// メニューバーとクイックメニューは、同じ部品 (EditorMenuItem) でできていて、同じ設定を見ている。
    /// ・項目が押されたら、対応する機能を呼ぶ。機能そのものは、キー操作と同じものを使う
    ///   (保存なら ChartDataExporter、元に戻すなら InputHandler、など)
    /// ・項目の状態を今の状態に合わせる (押せるかどうか、どの値が選ばれているか、チェックが入っているか、今の値)
    ///   メニューバーの項目は一覧を開いている間だけ、クイックメニューの項目はいつも
    /// ・メニューバーは、編集画面でも全画面プレビューでも、いつも出しておく。
    ///   譜面の読み込み中と、設定のダイアログなどを開いている間は、押せなくする
    /// </summary>
    public class EditorMenuPresenter : MonoBehaviour
    {
        [Serializable]
        class ValueLabel
        {
            public EditorMenuValue value;
            public TextMeshProUGUI label;
        }

        [Header("Views")]
        [SerializeField] EditorMenuBar menuBar_view;
        [Tooltip("メニューバーの一覧にある項目")]
        [SerializeField] EditorMenuItem[] items_view;
        [Tooltip("クイックメニュー (左下の設定欄) にある項目")]
        [SerializeField] EditorMenuItem[] quickItems_view;
        [Tooltip("設定の今の値を表示する文字 (ノートスピード、スクロール感度、判定音量、楽曲音量)")]
        [SerializeField] ValueLabel[] valueLabels_view;
        [Tooltip("延長する小節数・延長分の拍子・延長分の分割数の入力欄 (メニューの「譜面」と、クイックメニューの「譜面長さ」)")]
        [SerializeField] ChartExtendSettingsView[] chartExtendSettings_view;
        [Tooltip("「ファイル」の「バックアップから復元」を押すと、横に開く一覧。いちばん下に、バックアップの行が並んでいる (バックアップが少ないときは、余った行を隠して、枠を縮める)")]
        [SerializeField] RectTransform backupList_view;
        [Tooltip("全画面プレビューの、頂点の道具の列 (頂点レイヤーの道具の列と同じ見た目。頂点を持つノーツを1つ選んでいる間だけ出す)")]
        [SerializeField] PreviewVertexToolsView previewVertexTools_view;
        [Tooltip("「譜面」の一覧の、オフセットの入力欄")]
        [SerializeField] TMP_InputField offsetField_view;

        [Header("Models")]
        [SerializeField] ChartDataExporter chartDataExporter_model;
        [SerializeField] ChartDataImporter chartDataImporter_model;
        [SerializeField] LaneExtender laneExtender_model;
        [SerializeField] InputHandler inputHandler_model;
        [SerializeField] NotesCopier notesCopier_model;
        [SerializeField] NoteDestroyer noteDestroyer_model;

        [Header("Presenters")]
        [Tooltip("楽曲選択・譜面の延長を呼ぶ")]
        [SerializeField] EditorUIPresenter editorUI_presenter;
        [Tooltip("全画面プレビュー・編集画面へ戻る・プレビューの作り直しを呼ぶ")]
        [SerializeField] PreviewUIPresenter previewUI_presenter;

        // スクロール感度・音量を、ボタン1回で変える量
        const float STEP_UNIT = 0.1f;
        // 拡大・縮小1回で、拡大率を何倍にするか
        const float ZOOM_FACTOR = 1.25f;

        // メニューを押せなくするモード (設定のダイアログや説明書を開いている間)
        static readonly EditMode[] disabledEditModes =
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        IChartEditorOptionGetter optionGetter;
        IChartEditorOptionSetter optionSetter;
        INoteSpawnDataOptionGetter noteSpawnDataOptionGetter;
        INoteSpawnDataOptionSetter noteSpawnDataOptionSetter;
        INotesDataGetter notesGetter;

        // 復元できるバックアップ (新しい順。メニューを開いたときに調べ直す)
        readonly List<ChartBackups.Entry> backups = new List<ChartBackups.Entry>();
        // バックアップの一覧の枠の、バックアップの行をすべて出したときの高さ
        float fullBackupListHeight = -1f;

        // 今表示している値 (変わったときだけ、文字を作り直す)
        int displayedExtendBarCount = -1;
        string extendChartLabel;
        float[] displayedValues;

        [Inject]
        public void Construct(
            IChartEditorDataGetter dataGetter,
            IChartEditorDataSetter dataSetter,
            IChartEditorOptionGetter optionGetter,
            IChartEditorOptionSetter optionSetter,
            INoteSpawnDataOptionGetter noteSpawnDataOptionGetter,
            INoteSpawnDataOptionSetter noteSpawnDataOptionSetter,
            INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.optionGetter = optionGetter;
            this.optionSetter = optionSetter;
            this.noteSpawnDataOptionGetter = noteSpawnDataOptionGetter;
            this.noteSpawnDataOptionSetter = noteSpawnDataOptionSetter;
        }

        private void Start()
        {
            // 「表示」の一覧に、オニオンスキンの枚数と、SpaceHold の3D編集の行を足す (シーンの行を複製して作る)
            items_view = EditorMenuRuntimeRows.AddViewRows(items_view);

            SubscribeClicks(items_view);
            SubscribeClicks(quickItems_view);

            if (menuBar_view != null)
            {
                menuBar_view.OnOpenedListener += () =>
                {
                    // 横に開くバックアップの一覧は、メニューを開き直したときには、閉じた状態から始める
                    SetBackupListOpen(false);
                    RefreshItems(items_view);
                };
            }
            SetBackupListOpen(false);

            // 全画面プレビューの、頂点の道具の列
            if (previewVertexTools_view != null)
            {
                // 選んでいる道具をもう一度押すと、選ぶのをやめる (ふつうの操作に戻る)
                previewVertexTools_view.OnToolClickedListener += tool =>
                {
                    PreviewVertexTools.Select(PreviewVertexTools.Current == tool ? PreviewVertexTool.Move : tool);
                    RefreshPreviewVertexTools();
                };
                previewVertexTools_view.OnSlideClickedListener += delta =>
                {
                    if (PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out _, out SpaceVertices vertices)) { PreviewVertexTools.Slide(vertices, delta); }
                };
                previewVertexTools_view.OnMirrorClickedListener += isLeftRight =>
                {
                    if (PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out _, out SpaceVertices vertices)) { PreviewVertexTools.Mirror(vertices, isLeftRight); }
                };
            }

            // オフセットの入力欄 (入力を終えたときに反映する)
            if (offsetField_view != null)
            {
                offsetField = new NumberInputField(offsetField_view, value => dataSetter.SetOffset(value));
                if (menuBar_view != null) { menuBar_view.OnOpenedListener += RefreshOffsetField; }
                RefreshOffsetField();
            }

            // 入力欄を、延長の設定とつなぐ
            if (chartExtendSettings_view != null)
            {
                foreach (var view in chartExtendSettings_view)
                {
                    ChartExtendSettings.Bind(view, optionGetter, optionSetter, this);
                }
            }
        }

        NumberInputField offsetField;

        private void RefreshOffsetField()
        {
            if (offsetField != null && dataGetter != null) { offsetField.SetText(dataGetter.Offset.Value.ToString("0.##")); }
        }

        // 一覧の中の部品 (ノーツの接続・反転など)。メニューから使うときに探す
        NoteConnecter connecter;
        NotesMirror groundMirror;
        SpaceNotesMirror spaceMirror;
        ShortcutListView shortcutList;
        T Find<T>(ref T cache) where T : UnityEngine.Object
        {
            if (cache == null) { cache = FindObjectOfType<T>(true); }
            return cache;
        }

        private void SubscribeClicks(EditorMenuItem[] items)
        {
            if (items == null) { return; }

            foreach (var item in items)
            {
                if (item != null) { item.OnClickedListener += OnItemClicked; }
            }
        }

        private void Update()
        {
            if (menuBar_view == null || dataGetter == null) { return; }

            // 譜面の読み込み中と、設定のダイアログ・説明書を開いている間は、押せなくする
            bool isInteractable =
                !dataGetter.IsLoadingChart.Value &&
                !dataGetter.CurrentEditMode.Value.IsInEditModeList(disabledEditModes);
            menuBar_view.SetInteractable(isInteractable);

            // メニューバーの項目は、一覧を開いている間だけ、状態を更新し続ける (再生の状態や選択の変化に合わせる)
            if (menuBar_view.IsOpen) { RefreshItems(items_view); }
            // 全画面プレビューの頂点の道具の列を、使えるときだけ出す
            RefreshPreviewVertexTools();

            // クイックメニューの項目は、いつも見えているので、いつも更新する
            RefreshItems(quickItems_view);

            // オフセットの入力欄: ほかの所を押して入力を終えたとき
            offsetField?.CommitIfClickedElsewhere();

            RefreshValueLabels();
        }

        /// <summary>
        /// 全画面プレビューの頂点の道具の列を、使えるとき (全画面プレビューで、頂点を持つノーツを1つだけ選んでいるとき) だけ出す
        /// 使えなくなったら (選択を変えた、編集画面へ戻った)、道具を選んでいない状態に戻す
        /// </summary>
        private void RefreshPreviewVertexTools()
        {
            bool isAvailable = PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out _, out _);

            if (!isAvailable && PreviewVertexTools.Current != PreviewVertexTool.Move)
            {
                PreviewVertexTools.Select(PreviewVertexTool.Move);
            }

            // (ほかの欄 (譜面情報) が、列と重ならない位置へ動くのに使う)
            PreviewVertexTools.IsListShown = isAvailable && previewVertexTools_view != null;

            if (previewVertexTools_view != null)
            {
                previewVertexTools_view.SetVisible(isAvailable);
                previewVertexTools_view.SetTool(PreviewVertexTools.Current);
            }
        }

        /// <summary>
        /// 全画面プレビューの中にいるか
        /// </summary>
        private bool IsInFullScreenPreview => dataGetter.EditNoteType.Value == EditNoteType.Preview;

        /// <summary>
        /// 項目が押されたとき
        /// </summary>
        private void OnItemClicked(EditorMenuItem item)
        {
            // 押せない状態の項目は、何もしない (見た目が切り替わる前に押されたときのため、もう一度確かめる)
            if (!IsEnabled(item)) { return; }

            // 先にメニューを閉じる (ファイルを選ぶ画面などが、メニューを開いたまま出ないように)
            if (item.ClosesMenu) { menuBar_view?.Close(); }

            Execute(item.Command, item.Value);

            // 変えた結果を、すぐに見せる
            RefreshItems(quickItems_view);
            if (menuBar_view != null && menuBar_view.IsOpen) { RefreshItems(items_view); }
            RefreshValueLabels();
        }

        private void Execute(EditorMenuCommand command, int value)
        {
            switch (command)
            {
                // ---- ファイル
                case EditorMenuCommand.OpenChart:
                    chartDataImporter_model?.Import();
                    break;
                case EditorMenuCommand.SaveChart:
                    chartDataExporter_model?.Export();
                    break;
                case EditorMenuCommand.SaveChartAs:
                    chartDataExporter_model?.ExportNewFile();
                    break;
                case EditorMenuCommand.NewChart:
                    chartDataImporter_model?.CreateNewChart(() => editorUI_presenter?.ExtendChart());
                    break;
                case EditorMenuCommand.SelectMusic:
                    editorUI_presenter?.BrowseAudioFile();
                    break;
                case EditorMenuCommand.RestoreBackup:
                    if (value >= 0 && value < backups.Count) { chartDataImporter_model?.ImportBackup(backups[value].Path); }
                    break;
                case EditorMenuCommand.RestoreBackupFromFolder:
                    chartDataImporter_model?.ImportBackupFromDialog();
                    break;
                case EditorMenuCommand.ToggleBackupList:
                    SetBackupListOpen(!IsBackupListOpen);
                    break;

                // ---- 編集
                case EditorMenuCommand.Undo:
                    inputHandler_model?.UndoOnce();
                    break;
                case EditorMenuCommand.Redo:
                    inputHandler_model?.RedoOnce();
                    break;
                case EditorMenuCommand.CopyNotes:
                    notesCopier_model?.CopySelectedNotes();
                    break;
                case EditorMenuCommand.DeleteNotes:
                    noteDestroyer_model?.DestroySelectedNotes();
                    break;
                case EditorMenuCommand.PasteNotes:
                    notesCopier_model?.PasteAtPlaybackLine();
                    break;
                case EditorMenuCommand.CutNotes:
                    notesCopier_model?.CutSelectedNotes();
                    break;
                case EditorMenuCommand.ConnectNotes:
                    if (Find(ref connecter) != null && !connecter.ConnectSelectingNotes())
                    {
                        Debug.Log("【接続】接続できるノーツがありません (すでにつながっているか、種類が違います)");
                    }
                    break;
                case EditorMenuCommand.MirrorNotes:
                    if (dataGetter.EditNoteType.Value == EditNoteType.Space) { Find(ref spaceMirror)?.MirrorSelectingSpaceNotes(); }
                    else { Find(ref groundMirror)?.MirrorSelectingNotes(); }
                    break;
                case EditorMenuCommand.LockNotes:
                    NoteLock.Lock(GetSelection(false));
                    break;
                case EditorMenuCommand.UnlockNotes:
                    NoteLock.Unlock(GetSelection(true));
                    break;

                // ---- 譜面
                case EditorMenuCommand.ExtendChart:
                    editorUI_presenter?.ExtendChart();
                    break;
                case EditorMenuCommand.RemoveLastBar:
                    laneExtender_model?.ChangeChartLength(-1);
                    break;
                case EditorMenuCommand.UnlockAllNotes:
                    NoteLock.UnlockAll();
                    break;

                // ---- 表示
                case EditorMenuCommand.SelectLaneDivision:
                    optionSetter.SelectLaneDivisionNum(value);
                    break;
                case EditorMenuCommand.SelectWindowSize:
                    if (Enum.IsDefined(typeof(Resolution), value)) { optionSetter.SetResolution((Resolution)value); }
                    break;
                case EditorMenuCommand.StartPreview:
                    // 全画面プレビューの中では、この項目は「編集画面へ戻る」になっている
                    if (IsInFullScreenPreview) { previewUI_presenter?.BackToEditor(); }
                    else { previewUI_presenter?.StartFullScreenPreview(); }
                    break;
                case EditorMenuCommand.RebuildPreview:
                    previewUI_presenter?.RebuildPreview();
                    break;
                case EditorMenuCommand.ToggleSpaceHold3DView:
                    SpaceHold3DEditor.Instance?.ToggleFromMenu();
                    break;
                case EditorMenuCommand.SelectOnionSkinCount:
                    optionSetter.SetOnionSkinCount(value);
                    break;
                case EditorMenuCommand.ZoomIn:
                    optionSetter.SetChartViewScale(optionGetter.ChartViewScale.Value * ZOOM_FACTOR);
                    break;
                case EditorMenuCommand.ZoomOut:
                    optionSetter.SetChartViewScale(optionGetter.ChartViewScale.Value / ZOOM_FACTOR);
                    break;
                case EditorMenuCommand.ZoomReset:
                    optionSetter.SetChartViewScale(ChartEditorOptionHolder.DEFAULT_CHART_VIEW_SCALE);
                    break;

                // ---- 再生
                case EditorMenuCommand.TogglePlay:
                    inputHandler_model?.TogglePlay();
                    break;
                case EditorMenuCommand.SelectPlaybackSpeed:
                    optionSetter.SelectPlaybackSpeed(value / 100f);
                    break;
                case EditorMenuCommand.SelectNoteSound:
                    optionSetter.SelectNoteSoundMode(value == 1);
                    break;
                case EditorMenuCommand.StepNoteSpeed:
                    noteSpawnDataOptionSetter.SetNoteSpeed(noteSpawnDataOptionGetter.NoteSpeed.Value + value);
                    break;
                case EditorMenuCommand.StepHitVolume:
                    optionSetter.SetJudgementSEVolume(Step(optionGetter.JudgementSEVolume.Value, value));
                    break;
                case EditorMenuCommand.StepVisibleDistance:
                    optionSetter.SetNoteVisibleDistance(optionGetter.NoteVisibleDistance.Value + value);
                    break;
                case EditorMenuCommand.StepCurveRadius:
                    noteSpawnDataOptionSetter.SetNoteCurveRadius(Mathf.Clamp(noteSpawnDataOptionGetter.NoteCurveRadius.Value + value,
                        ChartEditorOptionHolder.MENU_MIN_NOTE_CURVE_RADIUS, ChartEditorOptionHolder.MENU_MAX_NOTE_CURVE_RADIUS));
                    break;

                // ---- 設定
                case EditorMenuCommand.StepScrollSensitivity:
                    optionSetter.SetScrollSensitivity(Step(optionGetter.ScrollSensitivity.Value, value));
                    break;
                case EditorMenuCommand.StepBackupInterval:
                    ChartSaveGuard.AutoSaveIntervalMinutes += value;
                    break;
                case EditorMenuCommand.StepMusicVolume:
                    optionSetter.SetBGMVolume(Step(optionGetter.BGMVolume.Value, value));
                    break;

                // ---- ヘルプ
                case EditorMenuCommand.OpenManual:
                    dataSetter.SetEditMode(EditMode.Explanation);
                    break;
                case EditorMenuCommand.ShowShortcuts:
                    Find(ref shortcutList)?.Show();
                    break;

                // ---- 表示・クイックメニュー: 画面部品を、表示する / しない
                case EditorMenuCommand.TogglePanel:
                    if (Enum.IsDefined(typeof(EditorPanel), value))
                    {
                        var panel = (EditorPanel)value;
                        optionSetter.SetPanelVisible(panel, !optionGetter.GetPanelVisibility(panel).Value);
                    }
                    break;
            }
        }

        /// <summary>
        /// 0〜1 の値を、10分の1刻みで steps 段階ぶん動かす
        /// (半端な値になっていても、ボタンを押すと 0.1 刻みの値にそろう)
        /// </summary>
        private static float Step(float current, int steps)
        {
            float stepped = Mathf.Round(current / STEP_UNIT) + steps;
            return Mathf.Clamp01(stepped * STEP_UNIT);
        }

        /// <summary>
        /// 項目を、今の状態に合わせる
        /// </summary>
        private void RefreshItems(EditorMenuItem[] items)
        {
            if (items == null || dataGetter == null) { return; }

            bool isPlaying = dataGetter.PlayMode.Value == PlayMode.Play;
            bool isInPreview = IsInFullScreenPreview;

            foreach (var item in items)
            {
                if (item == null) { continue; }

                item.SetEnabled(IsEnabled(item));
                item.SetChecked(IsChecked(item));

                // 状態で名前が変わる項目
                switch (item.Command)
                {
                    case EditorMenuCommand.TogglePlay:
                        item.SetLabel(isPlaying ? "停止" : "再生");
                        break;
                    case EditorMenuCommand.StartPreview:
                        item.SetLabel(isInPreview ? "編集画面へ戻る" : "全画面プレビュー");
                        break;
                    case EditorMenuCommand.ExtendChart:
                        item.SetLabel(GetExtendChartLabel());
                        break;
                    case EditorMenuCommand.ToggleSpaceHold3DView:
                        item.SetLabel(SpaceHold3DEditor.IsOpen ? "SpaceHoldの3D編集を閉じる" : "SpaceHoldを3Dで編集");
                        break;
                    case EditorMenuCommand.UnlockAllNotes:
                        item.SetLabel(NoteLock.HasAny ? $"すべてのロックを外す ({NoteLock.Count} 個)" : "すべてのロックを外す");
                        break;
                    case EditorMenuCommand.PasteNotes:
                    {
                        int count = notesCopier_model != null ? notesCopier_model.CopiedCount : 0;
                        item.SetLabel(count > 0 ? $"貼り付け ({count} 個、再生位置へ)" : "貼り付け (再生位置へ)");
                        break;
                    }
                    case EditorMenuCommand.LockNotes:
                    {
                        int count = CountSelection(false);
                        item.SetLabel(count > 1 ? $"選択中のノーツをロックする ({count} 個)" : "選択中のノーツをロックする");
                        break;
                    }
                    case EditorMenuCommand.UnlockNotes:
                    {
                        int count = CountSelection(true);
                        item.SetLabel(count > 1 ? $"選択中のロックを外す ({count} 個)" : "選択中のロックを外す");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 選択中のノーツのうち、ロックしている (isLocked) / していないものの数 (一覧の表示で毎フレーム数えるので、リストは作らない)
        /// </summary>
        private int CountSelection(bool isLocked)
        {
            if (notesGetter == null) { return 0; }
            int count = 0;
            foreach (var note in notesGetter.SelectingNotes)
            {
                if (NoteLock.IsLocked(note) == isLocked) { count++; }
            }
            return count;
        }

        /// <summary>
        /// 選択中のノーツのうち、ロックしている (isLocked) / していないもの
        /// </summary>
        private List<IDeployableNoteData> GetSelection(bool isLocked)
        {
            var result = new List<IDeployableNoteData>();
            if (notesGetter == null) { return result; }
            foreach (var note in notesGetter.SelectingNotes)
            {
                if (NoteLock.IsLocked(note) == isLocked) { result.Add(note); }
            }
            return result;
        }

        /// <summary>
        /// 「ファイル」の一覧の横に、バックアップの一覧を開いているか
        /// </summary>
        private bool IsBackupListOpen => backupList_view != null && backupList_view.gameObject.activeSelf;

        /// <summary>
        /// バックアップの一覧を、開く / 閉じる (開くときに、バックアップを調べ直す)
        /// 一覧は「ファイル」の一覧の子にしてあるので、メニューを閉じれば、一緒に見えなくなる
        /// </summary>
        private void SetBackupListOpen(bool isOpen)
        {
            if (backupList_view == null) { return; }

            if (isOpen) { RefreshBackups(); }
            if (backupList_view.gameObject.activeSelf != isOpen) { backupList_view.gameObject.SetActive(isOpen); }
        }

        /// <summary>
        /// 復元できるバックアップを調べて、バックアップの一覧の行に書く
        ///
        /// 行は、シーンに決まった数だけ置いてある。バックアップがそれより少ないときは、余った行を隠して、一覧の枠を縮める。
        /// 1つも無いときは、1行目に「バックアップはありません」と出す (押せない)。
        /// </summary>
        private void RefreshBackups()
        {
            if (items_view == null) { return; }

            int rowCount = 0;
            foreach (var item in items_view)
            {
                if (item != null && item.Command == EditorMenuCommand.RestoreBackup) { rowCount = Mathf.Max(rowCount, item.Value + 1); }
            }
            if (rowCount == 0) { return; }

            ChartBackups.Collect(ChartSaveGuard.AutoSaveDirectoryPath, ChartSaveGuard.AUTO_SAVE_PREFIX, ChartFilePathCache.CurrentChartFilePath, rowCount, backups);

            float hiddenHeight = 0f;
            foreach (var item in items_view)
            {
                if (item == null || item.Command != EditorMenuCommand.RestoreBackup) { continue; }

                bool hasBackup = item.Value < backups.Count;
                bool isShown = hasBackup || item.Value == 0;
                if (item.gameObject.activeSelf != isShown) { item.gameObject.SetActive(isShown); }

                if (!isShown)
                {
                    hiddenHeight += ((RectTransform)item.transform).sizeDelta.y;
                    continue;
                }

                item.SetLabel(hasBackup ? ChartBackups.FormatLabel(backups[item.Value]) : "バックアップはありません");
            }

            if (backupList_view != null)
            {
                if (fullBackupListHeight < 0f) { fullBackupListHeight = backupList_view.sizeDelta.y; }
                backupList_view.sizeDelta = new Vector2(backupList_view.sizeDelta.x, fullBackupListHeight - hiddenHeight);
            }
        }

        /// <summary>
        /// 「譜面を延長」の項目の名前 (延長する小節数を書き添える。小節数が変わったときだけ、文字を作り直す)
        /// </summary>
        private string GetExtendChartLabel()
        {
            int barCount = optionGetter.ExtendBarCount.Value;
            if (barCount != displayedExtendBarCount || extendChartLabel == null)
            {
                displayedExtendBarCount = barCount;
                extendChartLabel = $"譜面を延長 ({barCount} 小節)";
            }

            return extendChartLabel;
        }

        /// <summary>
        /// 設定の今の値の表示を、値が変わっていたら書き直す
        /// </summary>
        private void RefreshValueLabels()
        {
            if (valueLabels_view == null || optionGetter == null) { return; }

            if (displayedValues == null || displayedValues.Length != valueLabels_view.Length)
            {
                displayedValues = new float[valueLabels_view.Length];
                for (int i = 0; i < displayedValues.Length; i++) { displayedValues[i] = float.NaN; }
            }

            for (int i = 0; i < valueLabels_view.Length; i++)
            {
                var entry = valueLabels_view[i];
                if (entry == null || entry.label == null) { continue; }

                float value = GetValue(entry.value);
                if (value == displayedValues[i]) { continue; }

                displayedValues[i] = value;
                entry.label.text = FormatValue(entry.value, value);
            }
        }

        private float GetValue(EditorMenuValue value)
        {
            switch (value)
            {
                case EditorMenuValue.NoteSpeed: return noteSpawnDataOptionGetter.NoteSpeed.Value;
                case EditorMenuValue.ScrollSensitivity: return optionGetter.ScrollSensitivity.Value;
                case EditorMenuValue.HitVolume: return optionGetter.JudgementSEVolume.Value;
                case EditorMenuValue.MusicVolume: return optionGetter.BGMVolume.Value;
                case EditorMenuValue.BackupInterval: return ChartSaveGuard.AutoSaveIntervalMinutes;
                case EditorMenuValue.VisibleDistance: return optionGetter.NoteVisibleDistance.Value;
                case EditorMenuValue.CurveRadius: return noteSpawnDataOptionGetter.NoteCurveRadius.Value;
                default: return 0f;
            }
        }

        private static string FormatValue(EditorMenuValue kind, float value)
        {
            switch (kind)
            {
                case EditorMenuValue.NoteSpeed: return value.ToString("0");
                case EditorMenuValue.ScrollSensitivity: return value.ToString("0.0");
                case EditorMenuValue.BackupInterval: return value.ToString("0") + " 分";
                case EditorMenuValue.VisibleDistance:
                case EditorMenuValue.CurveRadius: return value.ToString("0");
                // 音量は百分率で表示する
                default: return Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        /// <summary>
        /// 項目を押せる状態か (キー操作と同じ条件にする)
        /// </summary>
        private bool IsEnabled(EditorMenuItem item)
        {
            bool isPlaying = dataGetter.PlayMode.Value == PlayMode.Play;
            bool isInPreview = IsInFullScreenPreview;
            var chartData = dataGetter.ChartData.Value;

            switch (item.Command)
            {
                // 再生中は、譜面や楽曲を入れ替えたり、譜面の長さを変えたりできない
                // 全画面プレビューの中でも受け付けない (読み込みが始まると、プレビューの操作ができなくなるため)
                case EditorMenuCommand.OpenChart:
                case EditorMenuCommand.NewChart:
                case EditorMenuCommand.SelectMusic:
                case EditorMenuCommand.ExtendChart:
                case EditorMenuCommand.RestoreBackupFromFolder:
                case EditorMenuCommand.ToggleBackupList:
                    return !isPlaying && !isInPreview;
                // バックアップからの復元も、譜面を入れ替えるので同じ。バックアップが無い行は押せない
                case EditorMenuCommand.RestoreBackup:
                    return !isPlaying && !isInPreview && item.Value < backups.Count;
                case EditorMenuCommand.RemoveLastBar:
                    return !isPlaying && !isInPreview && chartData != null && chartData.BarDatas.Count > 0;
                // ロックしたノーツが無いときは押せない
                case EditorMenuCommand.UnlockAllNotes:
                    return NoteLock.HasAny;
                case EditorMenuCommand.SaveChartAs:
                    return !isPlaying;

                case EditorMenuCommand.Undo:
                    return inputHandler_model != null && inputHandler_model.CanUndoNow;
                case EditorMenuCommand.Redo:
                    return inputHandler_model != null && inputHandler_model.CanRedoNow;
                case EditorMenuCommand.CopyNotes:
                    return notesCopier_model != null && notesCopier_model.CanCopySelectedNotes;
                case EditorMenuCommand.DeleteNotes:
                    return noteDestroyer_model != null && noteDestroyer_model.CanDestroySelectedNotes;
                case EditorMenuCommand.CutNotes:
                    return notesCopier_model != null && notesCopier_model.CanCutSelectedNotes;
                case EditorMenuCommand.PasteNotes:
                    return !isPlaying && notesCopier_model != null && notesCopier_model.CanPasteFromMenu;
                case EditorMenuCommand.ConnectNotes:
                case EditorMenuCommand.MirrorNotes:
                    return !isPlaying && !isInPreview && CountSelection(false) > 0 &&
                           (dataGetter.EditNoteType.Value == EditNoteType.Ground || dataGetter.EditNoteType.Value == EditNoteType.Space);
                case EditorMenuCommand.LockNotes:
                    return CountSelection(false) > 0;
                case EditorMenuCommand.UnlockNotes:
                    return CountSelection(true) > 0;
                case EditorMenuCommand.ZoomIn:
                case EditorMenuCommand.ZoomOut:
                case EditorMenuCommand.ZoomReset:
                    return !isInPreview;

                // プレビューは、譜面があるときだけ
                case EditorMenuCommand.StartPreview:
                case EditorMenuCommand.RebuildPreview:
                    return chartData != null;

                // 3D編集は、開いているときは閉じられる。閉じているときは、SpaceHold を選んでいる (頂点を編集している) ときだけ開ける
                case EditorMenuCommand.ToggleSpaceHold3DView:
                    return SpaceHold3DEditor.Instance != null && (SpaceHold3DEditor.IsOpen || SpaceHold3DEditor.Instance.CanOpenFromSelection());

                default:
                    return true;
            }
        }

        /// <summary>
        /// 項目の値が、今選ばれている値か (いくつかの値から1つを選ぶ項目)、チェックが入っているか (表示する / しないを切り替える項目)
        /// </summary>
        private bool IsChecked(EditorMenuItem item)
        {
            switch (item.Command)
            {
                case EditorMenuCommand.SelectLaneDivision:
                    return optionGetter.LaneDivisionNum.Value == item.Value;
                case EditorMenuCommand.SelectWindowSize:
                    return (int)optionGetter.Resolution.Value == item.Value;
                case EditorMenuCommand.SelectPlaybackSpeed:
                    return Mathf.RoundToInt(optionGetter.PlaybackSpeed.Value * 100f) == item.Value;
                case EditorMenuCommand.SelectNoteSound:
                    return (optionGetter.IsSingleNoteSound.Value ? 1 : 0) == item.Value;
                case EditorMenuCommand.SelectOnionSkinCount:
                    return optionGetter.OnionSkinCount.Value == item.Value;
                case EditorMenuCommand.TogglePanel:
                    return Enum.IsDefined(typeof(EditorPanel), item.Value) &&
                           optionGetter.GetPanelVisibility((EditorPanel)item.Value).Value;
                // 横にバックアップの一覧を開いている間は、その行に色を付けたままにする
                case EditorMenuCommand.ToggleBackupList:
                    return IsBackupListOpen;
                default:
                    return false;
            }
        }
    }
}
