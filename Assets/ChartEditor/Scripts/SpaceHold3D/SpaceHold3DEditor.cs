using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold を1本だけ、好きな角度から見て編集する (3D編集)
    ///
    /// 開き方:
    /// ・スペースのレイヤーで、SpaceHold のノーツを Alt を押しながらダブルクリック
    /// ・頂点レイヤーのフィルムストリップの「3Dで編集」
    /// ・メニューの「表示」→「SpaceHoldを3Dで編集」(選んでいる SpaceHold、または頂点を編集している SpaceHold)
    /// 開いている間は、画面全体 (メニューバーより下) が3Dの画面になり、裏の譜面は操作できない (EditMode.SpaceHoldView)。
    /// 開いた SpaceHold は色付きで、ほかのスペースのノーツは灰色で表示する。
    ///
    /// 見え方:
    /// ・帯 (半透明の青い面)、節の形 (始点: 緑、中継点: 水色、メッシュ中継点: 灰色、終点: 赤)
    /// ・軌跡 (橙の線。各節の中心 = 頂点の平均の位置 を通る)、0番の頂点の通り道 (桃色の線。ねじれを見るため)
    /// ・ノーツの面 (下半分の半円の筒) のレーンの線と、小節線の位置の弧
    /// ・ゲームと違い、軌道の曲がり (上り坂) は外して、奥行き (時間) をまっすぐに並べてある。奥行きの縮尺は変えられる
    ///
    /// 画面:
    /// ・右上: 視点の欄。向きの印 (Unity のシーンビューと同じ。X・Y・Z の軸の先を押すと、その向きから見る。まんなかで透視と平行の切り替え)、
    ///   よく使う向き (正面・横・上・斜め)、全体を映す、奥行きの縮尺
    /// ・左: 節の欄 (前後の節、節を足す、頂点の番号をずらす、反転、頂点レイヤーで編集、閉じる)
    /// ・右: 操作説明の欄 (編集画面と同じ欄に、3D編集の操作の行を足して出す)
    /// ・下のまんなか: 選んでいる節と、最後の操作
    ///
    /// 操作:
    /// ・カメラ: 右ドラッグ (または Alt+左ドラッグ、何も無い所の左ドラッグ) で回す、中ドラッグでずらす、ホイールで近づく・離れる、
    ///   F で全体 (節を選んでいれば、その節) を映す、1〜4 で 正面・横・上・斜め から見る
    /// ・節を選ぶ: 節の中心の点か、節の形の線を押す。↑↓で前後の節へ。選んだ節は、頂点レイヤーで編集する節にもなる
    /// ・選んだ節の、頂点の点をドラッグ: 頂点を動かす。中心の点をドラッグ: 節全体を動かす (どちらも節の面の中で)
    /// ・選んだ節の、矢印の先 (赤: 横、緑: 縦、青: 時間) をドラッグ: その向きにだけ動かす。時間は分線1本ずつ (前後の節は追い越さない)
    /// ・Ctrl を押しながら、選んだ節の線を押す: そこに頂点を足す。頂点を右クリック: その頂点を消す
    /// ・◆ (制御点) をドラッグ: 2つの節 (メッシュ中継点を飛ばした、隣り合った節) の間を、ベジェ曲線で曲げる。
    ///   離したときに、間にメッシュ中継点を置き直す (手で置いたものも含めて、間のメッシュ中継点はすべて置き直される)。
    ///   ◆は、カメラに向いた面の中で動く (横から見ると、縦と時間の向きに動く)。時間の向きは、2つの節の間から出ない。
    ///   ◆を右クリック: まっすぐに戻す (間のメッシュ中継点を消す)
    /// ・Esc: 閉じる。どの操作も、元に戻せる (Ctrl+Z)。
    ///
    /// 制御点の位置は、編集している間だけ覚えている (譜面ファイルには保存しない)。
    /// 覚えていない区間 (開き直したとき) は、今あるメッシュ中継点に近くなる位置に◆を出す。
    /// </summary>
    public partial class SpaceHold3DEditor : ITickable, IDisposable
    {
        /// <summary>断面の座標 (単位円の中の値) を、3Dの長さに直す倍率 (ゲームのノーツの面の半径と同じ)</summary>
        public const float RADIUS = 10f;
        /// <summary>奥行き1 (スピード倍率 1 で 1秒) を、縮尺 1 のときに、3Dの長さいくつにするか</summary>
        const float DEPTH_UNIT = 16f;

        static readonly float[] depthScaleSteps = { 0.1f, 0.2f, 0.35f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f, 5f, 8f };
        const int DEFAULT_DEPTH_SCALE_STEP = 5;

        // ダブルクリックとみなす間隔 (秒) と、カーソルのずれ (ピクセル)
        const float DOUBLE_CLICK_SECONDS = 0.35f;
        const float DOUBLE_CLICK_PIXELS = 12f;
        // ほかのスペースのノーツ (灰色) を調べ直す間隔 (秒)
        const float CONTEXT_REFRESH_SECONDS = 0.5f;
        // 最後の操作の表示を出しておく時間 (秒)
        const float STATUS_SECONDS = 6f;

        public static SpaceHold3DEditor Instance { get; private set; }

        /// <summary>
        /// 3D編集を開いているか
        /// </summary>
        public static bool IsOpen => Instance != null && Instance.isOpen;

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;

        readonly SpaceHold3DView view = new SpaceHold3DView();
        readonly SpaceHold3DCameraRig rig = new SpaceHold3DCameraRig();

        bool isOpen;
        bool isBuildFailed;
        ChartData openedChart;
        EditMode modeBeforeOpen;
        // つながりを探す手がかり (開いた SpaceHold のノーツのどれか)
        NoteData_SpaceHold anchor;
        readonly List<NoteData_SpaceHold> chain = new List<NoteData_SpaceHold>();
        readonly List<SpaceHoldSection> sections = new List<SpaceHoldSection>();
        SpaceHoldTimeline timeline;

        // 選んでいる節
        NoteData_SpaceHold selectedNote;
        int selectedIndex = -1;
        // 頂点レイヤーで編集する節 (選んだ節と合わせる。頂点レイヤーの側で替わったら、こちらも替える)
        IVerticesControlableNoteData lastEditing;

        // 奥行きの縮尺と、奥行きの基準 (開いたときの始点の奥行き。3Dの奥行き 0 の場所)
        int depthScaleStep = DEFAULT_DEPTH_SCALE_STEP;
        float depthOrigin;

        // 区間の制御点 (区間の早い方の節が覚えている。譜面ファイルには保存しない)
        readonly Dictionary<NoteData_SpaceHold, StoredControl> controls = new Dictionary<NoteData_SpaceHold, StoredControl>();
        ChartData controlsChart;

        struct StoredControl
        {
            public NoteData_SpaceHold End;
            public Vector3 Control;
        }

        /// <summary>
        /// 曲げられる区間 (隣り合った2つの節。間に分線があるもの)
        /// </summary>
        struct Span
        {
            public int StartIndex;
            public int EndIndex;
            public Vector3 Control;
        }
        readonly List<Span> spans = new List<Span>();

        // ダブルクリック (開く操作)
        float lastClickTime = -10f;
        Vector2 lastClickPosition;
        NoteData_SpaceHold lastClickNote;

        string statusText = string.Empty;
        float statusUntil;

        EditorUiButton projectionButton;
        TMPro.TextMeshProUGUI depthScaleLabel;
        SpaceHold3DViewGizmo gizmo;

        float DepthScale => depthScaleSteps[depthScaleStep];

        public SpaceHold3DEditor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;

            Instance = this;
        }

        void IDisposable.Dispose()
        {
            if (isOpen) { Close(); }
            view.Dispose();
            if (Instance == this) { Instance = null; }
        }

        void ITickable.Tick()
        {
            if (!isOpen)
            {
                HandleOpenGesture();
                return;
            }

            try
            {
                if (ShouldClose())
                {
                    Close();
                    return;
                }

                RefreshChain();
                if (sections.Count == 0)
                {
                    Close();
                    return;
                }

                // ほかの処理がモードを替えていたら (ドラッグの終わりなど)、戻しておく
                if (dataGetter.CurrentEditMode.Value != EditMode.SpaceHoldView && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                {
                    dataSetter.SetEditMode(EditMode.SpaceHoldView);
                }

                SyncSelectionFromEditor();
                HandleKeys();
                if (!isOpen) { return; }

                HandleMouse();
                Draw();
            }
            catch (Exception exception)
            {
                // 途中で失敗しても、裏の譜面を触れないまま残らないように閉じる
                Debug.LogException(exception);
                if (isOpen) { Close(); }
            }
        }

        #region 開く・閉じる

        /// <summary>
        /// 選んでいるもの (頂点を編集している節、または選んでいるノーツ) から、3D編集を開けるか
        /// </summary>
        public bool CanOpenFromSelection()
        {
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return false; }
            return FindSelectionTarget() != null;
        }

        /// <summary>
        /// メニューから: 開いていれば閉じる。閉じていれば、選んでいる SpaceHold を開く
        /// </summary>
        public void ToggleFromMenu()
        {
            if (isOpen)
            {
                Close();
                return;
            }

            var target = FindSelectionTarget();
            if (target == null)
            {
                Debug.Log("【SpaceHoldの3D編集】SpaceHold のノーツを選んでから開いてください");
                return;
            }

            Open(target);
        }

        private NoteData_SpaceHold FindSelectionTarget()
        {
            var editing = notesGetter.EditingVertices.Value as NoteData_SpaceHold;
            if (dataGetter.EditNoteType.Value == EditNoteType.Vertices && editing != null && notesGetter.GetNoteObject(editing) != null) { return editing; }

            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (selecting is NoteData_SpaceHold spaceHold && notesGetter.GetNoteObject(spaceHold) != null) { return spaceHold; }
            }

            return editing != null && notesGetter.GetNoteObject(editing) != null ? editing : null;
        }

        /// <summary>
        /// そのノーツがある SpaceHold を、3D編集で開く (開いているときは、その SpaceHold に切り替える)
        /// </summary>
        public void Open(NoteData_SpaceHold note)
        {
            if (note == null || isBuildFailed) { return; }

            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || dataGetter.IsLoadingChart.Value) { return; }
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return; }
            if (notesGetter.GetNoteObject(note) == null) { return; }

            if (!view.IsBuilt)
            {
                try
                {
                    view.Build();
                    BuildPanels();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    isBuildFailed = true;
                    view.Dispose();
                    return;
                }
            }

            if (!isOpen) { modeBeforeOpen = dataGetter.CurrentEditMode.Value; }

            // 覚えている制御点は、別の譜面のものなら捨てる
            if (!ReferenceEquals(controlsChart, chartData))
            {
                controls.Clear();
                controlsChart = chartData;
            }

            anchor = note;
            selectedNote = note;
            openedChart = chartData;
            isOpen = true;
            waitForMouseRelease = true;
            CancelDrag();

            EditorMenuBar.IsBlockedByOverlay = true;
            dataSetter.SetEditMode(EditMode.SpaceHoldView);

            RefreshChain();
            depthOrigin = sections.Count > 0 ? sections[0].Depth : 0f;
            nextContextRefreshTime = 0f;
            tunnelSignature = 0;

            // 開いたノーツを選ぶ (頂点レイヤーで編集する節にもなる)
            lastEditing = notesGetter.EditingVertices.Value;
            int openedIndex = chain.IndexOf(note);
            if (openedIndex >= 0) { Select(openedIndex); }

            view.SetVisible(true);
            view.UpdateRenderTarget();
            rig.IsOrthographic = false;
            rig.SetAngles(-35f, 22f);
            FrameAll();
            RefreshToolbar();

            ShowStatus("SpaceHold を3Dで開きました");
            Debug.Log("【SpaceHoldの3D編集】開きました。Esc キーで閉じます");
        }

        public void Close()
        {
            if (!isOpen) { return; }

            // ドラッグの途中なら、そこまでの変更を記録してから閉じる
            FinishDrag();

            isOpen = false;
            EditorMenuBar.IsBlockedByOverlay = false;

            if (dataGetter.CurrentEditMode.Value == EditMode.SpaceHoldView)
            {
                dataSetter.SetEditMode(GetModeToRestore());
            }

            view.SetVisible(false);
            anchor = null;
            selectedNote = null;
            selectedIndex = -1;
            openedChart = null;
            chain.Clear();
            sections.Clear();
            spans.Clear();
            lastClickTime = -10f;
        }

        /// <summary>
        /// 閉じたときに戻すモード (ツールの操作待ちのモードだけ戻す。何かの途中のモードは、何もしていない状態にする)
        /// </summary>
        private EditMode GetModeToRestore()
        {
            switch (modeBeforeOpen)
            {
                case EditMode.VertexEdit:
                case EditMode.VerticesRotate:
                case EditMode.VerticesScale:
                    return dataGetter.EditNoteType.Value == EditNoteType.Vertices ? modeBeforeOpen : EditMode.None;
                default:
                    return dataGetter.EditNoteType.Value == EditNoteType.Vertices ? EditMode.VertexEdit : EditMode.None;
            }
        }

        private bool ShouldClose()
        {
            if (dataGetter.IsLoadingChart.Value) { return true; }
            if (!ReferenceEquals(dataGetter.ChartData.Value, openedChart)) { return true; }
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return true; }
            return false;
        }

        /// <summary>
        /// スペースのレイヤーで、SpaceHold のノーツを Alt を押しながらダブルクリックしたら、開く
        /// </summary>
        private void HandleOpenGesture()
        {
            if (!Input.GetMouseButtonDown(0)) { return; }

            bool isAcceptable =
                ModifierKey.Alt &&
                !dataGetter.IsLoadingChart.Value &&
                dataGetter.ChartData.Value != null &&
                dataGetter.EditNoteType.Value == EditNoteType.Space &&
                !EditorMenuBar.IsBlockingChartInput &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            if (!isAcceptable)
            {
                lastClickTime = -10f;
                return;
            }

            var collider = dataGetter.GetInteractableCollider<ISpaceEditableCollider>();
            var note = collider?.Note?.NoteData as NoteData_SpaceHold;
            if (note == null)
            {
                lastClickTime = -10f;
                return;
            }

            Vector2 position = Input.mousePosition;
            bool isDoubleClick =
                Time.unscaledTime - lastClickTime <= DOUBLE_CLICK_SECONDS &&
                (position - lastClickPosition).sqrMagnitude <= DOUBLE_CLICK_PIXELS * DOUBLE_CLICK_PIXELS &&
                lastClickNote != null && IsSameChain(note, lastClickNote);

            lastClickTime = isDoubleClick ? -10f : Time.unscaledTime;
            lastClickPosition = position;
            lastClickNote = note;

            if (isDoubleClick) { Open(note); }
        }

        private static bool IsSameChain(NoteData_SpaceHold a, NoteData_SpaceHold b)
        {
            if (ReferenceEquals(a, b)) { return true; }
            return a.ChainIndex.Value >= 0 && a.ChainIndex.Value == b.ChainIndex.Value;
        }

        #endregion

        #region つながり・選択

        /// <summary>
        /// 開いている SpaceHold の節を、今の譜面の状態で集め直す
        /// </summary>
        private void RefreshChain()
        {
            timeline = SpaceHoldTimeline.Get(dataGetter.ChartData.Value);

            // 手がかりのノーツが消されていたら (元に戻すなど)、前のつながりの中で残っているノーツを手がかりにする
            if (anchor == null || notesGetter.GetNoteObject(anchor) == null)
            {
                anchor = null;
                foreach (var note in chain)
                {
                    if (note != null && notesGetter.GetNoteObject(note) != null)
                    {
                        anchor = note;
                        break;
                    }
                }
            }

            if (anchor == null || timeline == null)
            {
                chain.Clear();
                sections.Clear();
                spans.Clear();
                selectedIndex = -1;
                return;
            }

            // (譜面から消されたノーツは、つながりの一覧からも外れている。NoteObjectsBinder)
            SpaceHoldSections.GetChain(notesGetter, anchor, chain);
            if (!SpaceHoldSections.TryBuildSections(timeline, chain, sections)) { sections.Clear(); }

            selectedIndex = selectedNote != null ? chain.IndexOf(selectedNote) : -1;
            if (selectedIndex < 0) { selectedNote = null; }

            RefreshSpans();
        }

        /// <summary>
        /// 曲げられる区間と、その制御点を求める
        /// </summary>
        private void RefreshSpans()
        {
            spans.Clear();

            int previousNode = -1;
            for (int i = 0; i < sections.Count; i++)
            {
                bool isNode = sections[i].Note.NoteType != DeploymentNoteType.SpaceHoldMeshRelay || i == 0 || i == sections.Count - 1;
                if (!isNode) { continue; }

                if (previousNode >= 0 && sections[i].Line - sections[previousNode].Line >= 2)
                {
                    spans.Add(new Span
                    {
                        StartIndex = previousNode,
                        EndIndex = i,
                        Control = GetControl(previousNode, i),
                    });
                }

                previousNode = i;
            }
        }

        private Vector3 GetControl(int startIndex, int endIndex)
        {
            SpaceHoldSection a = sections[startIndex];
            SpaceHoldSection b = sections[endIndex];

            if (controls.TryGetValue(a.Note, out StoredControl stored) && ReferenceEquals(stored.End, b.Note))
            {
                return SpaceHoldCurveMath.ClampControl(a, b, stored.Control);
            }

            relayBuffer.Clear();
            for (int i = startIndex + 1; i < endIndex; i++) { relayBuffer.Add(sections[i]); }
            return SpaceHoldCurveMath.FitControl(a, b, relayBuffer);
        }
        readonly List<SpaceHoldSection> relayBuffer = new List<SpaceHoldSection>();

        /// <summary>
        /// 頂点レイヤーで編集する節が、ほかの所 (↑↓キー、フィルムストリップ) で替わったら、選ぶ節も替える
        /// </summary>
        private void SyncSelectionFromEditor()
        {
            var editing = notesGetter.EditingVertices.Value;
            if (ReferenceEquals(editing, lastEditing)) { return; }

            lastEditing = editing;
            if (editing is NoteData_SpaceHold spaceHold && chain.Contains(spaceHold))
            {
                selectedNote = spaceHold;
                selectedIndex = chain.IndexOf(spaceHold);
            }
        }

        /// <summary>
        /// 節を選ぶ (-1 で、選ぶのをやめる)。選んだ節は、頂点レイヤーで編集する節にもなる
        /// </summary>
        private void Select(int index)
        {
            if (index < 0 || index >= sections.Count)
            {
                selectedIndex = -1;
                selectedNote = null;
                return;
            }

            selectedIndex = index;
            selectedNote = sections[index].Note;
            notesSetter.SetEditingVertices(selectedNote);
            lastEditing = selectedNote;
        }

        #endregion

        #region キー操作

        private void HandleKeys()
        {
            if (InputFieldFocus.IsTyping || EditorMenuBar.IsMenuOpenOrJustClosed) { return; }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F)) { FrameSelectionOrAll(); }
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) { SetViewFront(); }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) { SetViewSide(); }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) { SetViewTop(); }
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) { SetViewOblique(); }

            if (!IsDragging)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) { SelectNeighbor(1); }
                if (Input.GetKeyDown(KeyCode.DownArrow)) { SelectNeighbor(-1); }
            }
        }

        private void SelectNeighbor(int delta)
        {
            if (sections.Count == 0) { return; }

            int index = selectedIndex < 0 ? (delta > 0 ? 0 : sections.Count - 1) : Mathf.Clamp(selectedIndex + delta, 0, sections.Count - 1);
            Select(index);
        }

        #endregion

        #region 欄 (視点の欄・節の欄・操作説明)

        // 欄の幅と、画面の端からの距離 (1920×1080 基準)
        const float VIEW_PANEL_WIDTH = 236f;
        const float SECTION_PANEL_WIDTH = 176f;
        const float PANEL_MARGIN = 12f;
        // 向きの印を置く場所の高さ
        const float GIZMO_AREA_HEIGHT = 124f;

        /// <summary>
        /// 欄を作る
        /// ・右上: 視点の欄 (向きの印、よく使う向き、透視と平行、全体を映す、奥行きの縮尺)
        /// ・左: 節の欄 (前後の節、節を足す、頂点の番号、反転、頂点レイヤーへ、閉じる)
        /// 操作の説明は、画面の右の「操作説明」の欄に出す (BuildDescriptionRows)
        /// (メニューの文字にない記号 (◀ ▶ など) は四角になるので、使わない)
        /// </summary>
        private void BuildPanels()
        {
            // 視点の欄 (右上)
            var viewPanel = view.AddPanel("ViewPanel", new Vector2(1f, 1f), new Vector2(-PANEL_MARGIN, -PANEL_MARGIN), VIEW_PANEL_WIDTH);
            view.AddPanelTitle(viewPanel, "視点");

            gizmo = new SpaceHold3DViewGizmo(view.AddPanelSpace(viewPanel, GIZMO_AREA_HEIGHT))
            {
                OnAxisClicked = SetViewFromAxis,
                OnCenterClicked = ToggleProjection,
            };

            var axisCaption = view.AddPanelRow(viewPanel, 18f);
            view.AddRowLabel(axisCaption, "X: 横    Y: 縦    Z: 時間 (奥)").fontSize = 12f;

            var presetRow = view.AddPanelRow(viewPanel);
            view.AddRowButton(presetRow, "正面", SetViewFront);
            view.AddRowButton(presetRow, "横", SetViewSide);
            view.AddRowButton(presetRow, "上", SetViewTop);
            view.AddRowButton(presetRow, "斜め", SetViewOblique);

            var projectionRow = view.AddPanelRow(viewPanel);
            projectionButton = view.AddRowButton(projectionRow, "透視", ToggleProjection);
            view.AddRowButton(projectionRow, "全体を映す (F)", FrameSelectionOrAll, 2f);

            var depthRow = view.AddPanelRow(viewPanel);
            view.AddRowLabel(depthRow, "奥行き", 52f);
            view.AddRowButton(depthRow, "-", () => ChangeDepthScale(-1));
            depthScaleLabel = view.AddRowLabel(depthRow, "x1", 50f);
            view.AddRowButton(depthRow, "+", () => ChangeDepthScale(1));

            // 節の欄 (左)
            var sectionPanel = view.AddPanel("SectionPanel", new Vector2(0f, 1f), new Vector2(PANEL_MARGIN, -PANEL_MARGIN), SECTION_PANEL_WIDTH);
            view.AddPanelTitle(sectionPanel, "節");

            var neighborRow = view.AddPanelRow(sectionPanel);
            view.AddRowButton(neighborRow, "前の節", () => SelectNeighbor(-1));
            view.AddRowButton(neighborRow, "次の節", () => SelectNeighbor(1));

            view.AddRowButton(view.AddPanelRow(sectionPanel), "節を足す (補間)", InsertSectionAfterSelected);

            var slideRow = view.AddPanelRow(sectionPanel);
            view.AddRowButton(slideRow, "番号 -1", () => SlideSelected(-1));
            view.AddRowButton(slideRow, "番号 +1", () => SlideSelected(1));

            var mirrorRow = view.AddPanelRow(sectionPanel);
            view.AddRowButton(mirrorRow, "左右反転", () => MirrorSelected(true));
            view.AddRowButton(mirrorRow, "上下反転", () => MirrorSelected(false));

            view.AddPanelSpace(sectionPanel, 4f);
            view.AddRowButton(view.AddPanelRow(sectionPanel), "頂点レイヤーで編集", OpenSelectedInVerticesLayer);
            view.AddRowButton(view.AddPanelRow(sectionPanel), "閉じる (Esc)", Close);

            BuildDescriptionRows();
        }

        /// <summary>
        /// 画面の右の「操作説明」の欄に、3D編集の操作の行を足す (シーンの行を複製して作る)
        /// 元に戻す・再生・欄の最小化は、編集画面と同じなので、シーンの行をそのまま3D編集でも出す
        /// </summary>
        private void BuildDescriptionRows()
        {
            var description = UnityEngine.Object.FindObjectOfType<OperationDescriptionView>(true);
            if (description == null) { return; }

            var modes = new[] { EditMode.SpaceHoldView };
            const DescriptionLayers LAYERS = DescriptionLayers.Ground | DescriptionLayers.Space | DescriptionLayers.Vertices;
            const string TEMPLATE = "Undo";

            (string key, string text)[] rows =
            {
                ("クリック", "節を選ぶ"),
                ("ドラッグ", "頂点・矢印・◆を動かす"),
                ("右ドラッグ", "視点を回す"),
                ("Alt+ドラッグ", "視点を回す"),
                ("中ドラッグ", "視点をずらす"),
                ("ホイール", "近づく / 離れる"),
                ("F", "全体を映す"),
                ("右上の XYZ", "その向きから見る"),
                ("1 / 2 / 3 / 4", "正面 / 横 / 上 / 斜め"),
                ("↑ / ↓", "前後の節を選ぶ"),
                ("Ctrl+クリック", "選んだ節に頂点を足す"),
                ("右クリック (頂点)", "頂点を消す"),
                ("◆ ドラッグ", "節の間を曲線で曲げる"),
                ("右クリック (◆)", "まっすぐに戻す"),
                ("Esc", "3D編集を閉じる"),
            };

            for (int i = 0; i < rows.Length; i++)
            {
                description.AddRuntimeRow(TEMPLATE, $"SpaceHold3D_{i}", rows[i].key, rows[i].text, modes, LAYERS);
            }

            description.AddEditModeToRows(EditMode.SpaceHoldView, "Undo", "Play", "Minimize");
        }

        private void RefreshToolbar()
        {
            if (projectionButton != null) { projectionButton.SetText(rig.IsOrthographic ? "平行" : "透視"); }
            if (depthScaleLabel != null) { depthScaleLabel.text = $"x{DepthScale:0.##}"; }
        }

        /// <summary>
        /// 向きの印の、軸の先を押したとき: その向きから見る
        /// </summary>
        private void SetViewFromAxis(Vector3 direction)
        {
            if (direction == Vector3.right) { rig.SetAngles(-90f, 0f); }
            else if (direction == Vector3.left) { rig.SetAngles(90f, 0f); }
            else if (direction == Vector3.up) { rig.SetAngles(-90f, 89f); }
            else if (direction == Vector3.down) { rig.SetAngles(-90f, -89f); }
            else if (direction == Vector3.forward) { rig.SetAngles(180f, 0f); }
            else if (direction == Vector3.back) { rig.SetAngles(0f, 0f); }
        }

        private void SetViewFront() { rig.SetAngles(0f, 0f); }
        private void SetViewSide() { rig.SetAngles(-90f, 0f); }
        private void SetViewTop() { rig.SetAngles(-90f, 89f); }
        private void SetViewOblique() { rig.SetAngles(-35f, 22f); }

        private void ToggleProjection()
        {
            rig.IsOrthographic = !rig.IsOrthographic;
            RefreshToolbar();
        }

        /// <summary>
        /// 奥行きの縮尺を変える (見ている中心の時刻は、そのまま)
        /// </summary>
        private void ChangeDepthScale(int delta)
        {
            int next = Mathf.Clamp(depthScaleStep + delta, 0, depthScaleSteps.Length - 1);
            if (next == depthScaleStep) { return; }

            float pivotDepth = ToDepth(rig.Pivot);
            depthScaleStep = next;
            Vector3 pivot = rig.Pivot;
            pivot.z = ToWorld(Vector2.zero, pivotDepth).z;
            rig.Pivot = pivot;
            RefreshToolbar();
        }

        private void FrameSelectionOrAll()
        {
            if (selectedIndex >= 0 && selectedIndex < sections.Count) { FrameSection(sections[selectedIndex]); }
            else { FrameAll(); }
        }

        private void FrameAll()
        {
            if (sections.Count == 0) { return; }

            var bounds = new Bounds(ToWorld(sections[0].Center, sections[0].Depth), Vector3.zero);
            foreach (var section in sections)
            {
                foreach (var vertex in section.Vertices) { bounds.Encapsulate(ToWorld(vertex, section.Depth)); }
            }
            bounds.Encapsulate(bounds.center + Vector3.one * RADIUS * 0.25f);

            Vector2 size = view.ViewportSize;
            rig.Frame(bounds.center, bounds.extents.magnitude, size.x / Mathf.Max(1f, size.y));
        }

        private void FrameSection(SpaceHoldSection section)
        {
            var bounds = new Bounds(ToWorld(section.Center, section.Depth), Vector3.zero);
            foreach (var vertex in section.Vertices) { bounds.Encapsulate(ToWorld(vertex, section.Depth)); }

            Vector2 size = view.ViewportSize;
            rig.Frame(bounds.center, Mathf.Max(bounds.extents.magnitude, RADIUS * 0.3f), size.x / Mathf.Max(1f, size.y));
        }

        private bool TryGetSelected(out SpaceHoldSection section)
        {
            section = default;
            if (selectedIndex < 0 || selectedIndex >= sections.Count) { return false; }

            section = sections[selectedIndex];
            return true;
        }

        private void SlideSelected(int delta)
        {
            if (!TryGetSelected(out var section)) { ShowStatus("節を選んでください"); return; }
            PreviewVertexTools.Slide(section.Note.SpaceVertices, delta);
        }

        private void MirrorSelected(bool isLeftRight)
        {
            if (!TryGetSelected(out var section)) { ShowStatus("節を選んでください"); return; }
            PreviewVertexTools.Mirror(section.Note.SpaceVertices, isLeftRight);
        }

        /// <summary>
        /// 選んだ節と次の節の間 (選んだ節が終点なら、前の節との間) に、補間した形のメッシュ中継点を足す
        /// </summary>
        private void InsertSectionAfterSelected()
        {
            if (!TryGetSelected(out _)) { ShowStatus("節を選んでください"); return; }

            int gap = selectedIndex < sections.Count - 1 ? selectedIndex : selectedIndex - 1;
            if (gap < 0) { ShowStatus("つながっている節がありません"); return; }

            if (SpaceHoldFilmStrip.TryInsertInterpolatedSection(dataGetter.ChartData.Value, timeline, sections, gap, out NoteData_SpaceHold inserted))
            {
                selectedNote = inserted;
                notesSetter.SetEditingVertices(inserted);
                lastEditing = inserted;
                ShowStatus("補間した形のメッシュ中継点を足しました");
            }
            else
            {
                ShowStatus("2つの節の間に分線が無いので、節を足せません");
            }
        }

        /// <summary>
        /// 閉じて、選んでいる節を頂点レイヤーで編集する
        /// </summary>
        private void OpenSelectedInVerticesLayer()
        {
            NoteData_SpaceHold target = selectedNote ?? anchor;
            Close();
            if (target == null) { return; }

            dataSetter.SetEditNoteType(EditNoteType.Vertices);
            notesSetter.SetEditingVertices(target);
        }

        #endregion

        #region 座標

        /// <summary>
        /// 断面の座標 (単位円の中の値) と奥行きから、3Dの位置 (3Dのものを置く場所の中の位置) を求める
        /// </summary>
        private Vector3 ToLocal(Vector2 unit, float depth)
        {
            return new Vector3(unit.x * RADIUS, unit.y * RADIUS, (depth - depthOrigin) * DEPTH_UNIT * DepthScale);
        }

        private Vector3 ToWorld(Vector2 unit, float depth)
        {
            return SpaceHold3DView.ROOT_POSITION + ToLocal(unit, depth);
        }

        private Vector2 ToUnit(Vector3 world)
        {
            Vector3 local = world - SpaceHold3DView.ROOT_POSITION;
            return new Vector2(local.x / RADIUS, local.y / RADIUS);
        }

        private float ToDepth(Vector3 world)
        {
            Vector3 local = world - SpaceHold3DView.ROOT_POSITION;
            return local.z / (DEPTH_UNIT * DepthScale) + depthOrigin;
        }

        #endregion

        #region 表示

        private void ShowStatus(string text)
        {
            statusText = text;
            statusUntil = Time.unscaledTime + STATUS_SECONDS;
        }

        #endregion

        #region ベジェ曲線で置いたメッシュ中継点を、譜面に入れる

        /// <summary>
        /// 区間の間のメッシュ中継点を、制御点の曲線のとおりに置き直す (元に戻せる)
        /// </summary>
        /// <returns>置いた中継点の数 (置き直さなかったときは -1)</returns>
        private int CommitCurve(int startIndex, int endIndex, Vector3 control)
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || timeline == null) { return -1; }
            if (startIndex < 0 || endIndex >= sections.Count || startIndex >= endIndex) { return -1; }

            SpaceHoldSection a = sections[startIndex];
            SpaceHoldSection b = sections[endIndex];
            int chainIndex = a.Note.ChainIndex.Value;
            if (chainIndex < 0) { return -1; }

            control = SpaceHoldCurveMath.ClampControl(a, b, control);
            var baked = new List<SpaceHoldCurveMath.BakedSection>();
            SpaceHoldCurveMath.Bake(timeline, a, b, control, SpaceHoldSections.GetNormalizedVertexCount(sections), baked);

            // 外すもの: 2つの節の間のメッシュ中継点 (手で置いたものも含む)
            var removed = new List<NoteData_SpaceHold>();
            var removedAddresses = new List<AddressWithinRange>();
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                removed.Add(sections[i].Note);
                removedAddresses.Add(new AddressWithinRange(sections[i].Note.Address));
            }

            // 足すもの
            var added = new List<NoteData_SpaceHold>();
            foreach (var section in baked)
            {
                var note = new NoteData_SpaceHold();
                note.SetAddress(timeline.CreateSpaceAddress(section.Line));
                note.SetNoteType(DeploymentNoteType.SpaceHoldMeshRelay);
                note.SpaceVertices.SetVertices(section.Vertices);
                note.SetChainIndex(chainIndex);
                added.Add(note);
            }

            if (removed.Count == 0 && added.Count == 0)
            {
                StoreControl(a.Note, b.Note, control);
                return 0;
            }

            NoteData_SpaceHold startNote = a.Note;
            NoteData_SpaceHold endNote = b.Note;
            Vector3? previousControl = controls.TryGetValue(startNote, out StoredControl stored) && ReferenceEquals(stored.End, endNote) ? stored.Control : (Vector3?)null;

            Record(() =>
            {
                foreach (var note in removed) { chartData.RemoveNote(note, false); }
                foreach (var note in added) { chartData.AddNote(note); }
                StoreControl(startNote, endNote, control);
            },
            () =>
            {
                foreach (var note in added) { chartData.RemoveNote(note, false); }
                for (int i = 0; i < removed.Count; i++)
                {
                    removed[i].SetAddress(removedAddresses[i]);
                    removed[i].SetNoteType(DeploymentNoteType.SpaceHoldMeshRelay);
                    chartData.AddNote(removed[i]);
                }

                if (previousControl.HasValue) { StoreControl(startNote, endNote, previousControl.Value); }
                else { controls.Remove(startNote); }
            });

            // 選んでいた節が外れたら、区間の始まりの節を選ぶ
            if (selectedNote != null && removed.Contains(selectedNote))
            {
                selectedNote = startNote;
                notesSetter.SetEditingVertices(startNote);
                lastEditing = startNote;
            }

            return added.Count;
        }

        private void StoreControl(NoteData_SpaceHold start, NoteData_SpaceHold end, Vector3 control)
        {
            controls[start] = new StoredControl { End = end, Control = control };
        }

        #endregion
    }
}
