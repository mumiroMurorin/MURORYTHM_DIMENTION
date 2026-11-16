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
    ///   全体を映す、奥行きの縮尺 (正面・横・上・斜めのボタンは、向きの印で足りるので置かない。キーの 1〜4 は使える)
    /// ・左上: 頂点レイヤーの道具の列と同じ見た目の列。節を1つだけ選んでいるときは、頂点の道具 (編集・回転・拡大縮小・
    ///   頂点スライド・反転。全画面プレビューの列と同じ)、それ以外のときは、節の道具 (節を足す・頂点スライド・反転)
    /// ・右下: ミニプレビュー (編集画面のもの)。その左に「頂点レイヤーで編集」と「編集画面へ戻る」のボタン
    ///   (この2つは、シーンに置いてある画面部品: SpaceHold3DOverlayView)
    /// ・右: 操作説明の欄 (編集画面と同じ欄に、3D編集の操作の行を足して出す)
    /// ・下のまんなか: 選んでいる節と、最後の操作
    ///
    /// 操作:
    /// ・カメラ: 右ドラッグ (または Alt+左ドラッグ、何も無い所の左ドラッグ) で回す、中ドラッグでずらす、ホイールで近づく・離れる、
    ///   F で全体 (節を選んでいれば、その節) を映す、1〜4 で 正面・横・上・斜め から見る
    /// ・節を選ぶ: 節の中心の点か、節の形の線を押す。↑↓で前後の節へ。選んだ節は、頂点レイヤーで編集する節にもなる
    ///   Ctrl を押しながら押すと、選択に加える・外す (複数の節を選ぶと、節の道具が、選んだ節すべてに働く)
    /// ・頂点を選ぶ: 選んだ節の頂点の点をクリック (Ctrl で加える・外す)。Delete で、選んだ頂点を消す。選んだ頂点をドラッグすると、まとめて動く
    /// ・右クリック: 頂点 = 頂点を消すメニュー。◆ = 位置を数値で決める窓。節 (線・中心・選んでいる節) = 節のメニュー
    ///   (頂点レイヤーで編集、この形をホールド全体に反映、節を足す、リレー / メッシュリレーにする、ロック、消す。録音 20261117_023636 / 023744)
    /// ・Delete: 選んでいる頂点を消す。頂点を選んでいなければ、選んでいる節を消す (録音 20261117_023852)
    /// ・選んだ節の、頂点の点をドラッグ: 頂点を動かす。中心の点をドラッグ: 節全体を動かす (どちらも節の面の中で)
    /// ・選んだ節の、矢印の先 (赤: 横、緑: 縦、青: 時間) をドラッグ: その向きにだけ動かす。時間は分線1本ずつ (前後の節は追い越さない)
    /// ・頂点の道具: 編集 = 選んだ節の面の上を押すと、そこに頂点を置く (そのまま動かして調整できる。元に戻すのは1回ぶん)、
    ///   回転 / 拡大縮小 = 頂点をドラッグすると、選んだ頂点 (2つ以上選んでいなければ、すべての頂点) を、その中心のまわりに回す / 拡大・縮小する
    /// ・◆ (制御点) をドラッグ: 2つの節 (メッシュ中継点を飛ばした、隣り合った節) の間を、ベジェ曲線で曲げる。
    ///   離したときに、間にメッシュ中継点を置き直す (手で置いたものも含めて、間のメッシュ中継点はすべて置き直される)。
    ///   ◆は、カメラに向いた面の中で動く (横から見ると、縦と時間の向きに動く)。時間の向きは、2つの節の間から出ない。
    ///   ◆を右クリック・ダブルクリック: 横に小さい窓が出て、位置を数値で決められる (X・Y は断面の中の位置、Z は2つの節の間のどこか:
    ///   0 = 前の節、1 = 次の節)。窓の「まっすぐに戻す」で、2つの節の間をまっすぐに戻す (録音 20261117_005036: メニューを挟まない)
    /// ・Esc: 閉じる (右下のボタンに Esc と書いてある)。どの操作も、元に戻せる (Ctrl+Z)。
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

        // シーンに置いてある画面部品 (道具の列、前後の節、右下のボタン、◆ の位置の窓)
        SpaceHold3DOverlayView overlay;
        bool isOverlaySearched;
        // ◆ の位置の窓で見せている区間 (区間の2つの節。番号は、節が増えたり減ったりすると変わるので、ノーツで覚える)
        NoteData_SpaceHold controlPanelStart;
        NoteData_SpaceHold controlPanelEnd;
        // ◆ のダブルクリック
        float lastControlClickTime = -10f;
        Vector2 lastControlClickPosition;
        int lastControlClickSpan = -1;
        // ◆ を、窓のどれだけ右に出すか (画面部品の単位)
        const float CONTROL_PANEL_OFFSET = 24f;

        // 選んでいる節のほかに、選んでいる節 (Ctrl+クリックで加えたもの)
        readonly HashSet<NoteData_SpaceHold> extraSelected = new HashSet<NoteData_SpaceHold>();
        // 選んでいる頂点 (節を1つだけ選んでいるときの、その節の頂点)
        readonly List<VertexData> selectedVertices = new List<VertexData>();
        NoteData_SpaceHold selectedVerticesOwner;
        // 頂点の道具 (節を1つだけ選んでいる間だけ使える。Move = 道具を選んでいない)
        PreviewVertexTool vertexTool = PreviewVertexTool.Move;
        // 頂点の道具「編集」で置いて、まだ元に戻す履歴に登録していない頂点 (離したときに、置いた操作として1回ぶん登録する)
        SpaceVertices placedVertices;
        VertexData placedVertex;

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
                PruneSelection();
                Draw();
                RefreshControlPanel();
                RefreshOverlayTools();
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
        /// その節を、ロックしているか (ロックした節は、動かせない・形を変えられない)
        /// </summary>
        private bool IsLockedSection(int index)
        {
            return index >= 0 && index < sections.Count && NoteLock.IsLocked(sections[index].Note);
        }

        private void ShowLockedStatus()
        {
            ShowStatus("ロックした節は、動かせません (外すには、編集画面で右クリックのメニューの「ロックを外す」)");
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
            // (ロックした節があっても開く。ロックした節は動かせず、ロックしたメッシュ中継点は、曲線の節になる: 録音 20261117_012711)

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
            ConnectOverlay();
            if (overlay != null) { overlay.SetVisible(true); }
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
            // ◆ の位置の窓で打っている途中の値も、反映してから閉じる
            CloseControlPanel();
            EditorContextMenuView.Hide();
            SetCursor(EditMode.None);
            if (overlay != null) { overlay.SetVisible(false); }
            extraSelected.Clear();
            ClearVertexSelection();
            vertexTool = PreviewVertexTool.Move;

            // 頂点レイヤーを開いていて、編集する節が無くなっていたら (節を消した・元に戻したなど)、残っている節にする
            if (dataGetter.EditNoteType.Value == EditNoteType.Vertices && notesGetter.EditingVertices.Value == null)
            {
                foreach (var note in chain)
                {
                    if (note != null && notesGetter.GetNoteObject(note) != null && !NoteLock.IsLocked(note))
                    {
                        notesSetter.SetEditingVertices(note);
                        break;
                    }
                }
            }

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
            lastControlClickSpan = -1;
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
                // ロックしたメッシュ中継点も節にする (その中継点と隣の節の間を、曲げられるように。グラウンドのホールドと同じ)
                bool isNode = sections[i].Note.NoteType != DeploymentNoteType.SpaceHoldMeshRelay || i == 0 || i == sections.Count - 1 ||
                              NoteLock.IsLocked(sections[i].Note);
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
                extraSelected.Clear();
            }
        }

        /// <summary>
        /// 節を選ぶ (-1 で、選ぶのをやめる)。選んだ節は、頂点レイヤーで編集する節にもなる
        /// </summary>
        private void Select(int index)
        {
            extraSelected.Clear();

            if (index < 0 || index >= sections.Count)
            {
                selectedIndex = -1;
                selectedNote = null;
                return;
            }

            selectedIndex = index;
            selectedNote = sections[index].Note;
            // ロックした節は、頂点レイヤーで編集する節にはしない (編集画面の頂点レイヤーで、形を変えられないように)
            if (NoteLock.IsLocked(selectedNote))
            {
                lastEditing = notesGetter.EditingVertices.Value;
                return;
            }
            notesSetter.SetEditingVertices(selectedNote);
            lastEditing = selectedNote;
        }

        /// <summary>
        /// Ctrl+クリック: 節を、選択に加える (選んでいれば、外す)
        /// </summary>
        private void ToggleSectionSelection(int index)
        {
            if (index < 0 || index >= sections.Count) { return; }

            NoteData_SpaceHold note = sections[index].Note;
            if (selectedNote == null)
            {
                Select(index);
                return;
            }

            if (ReferenceEquals(note, selectedNote))
            {
                // いちばん手前に選んでいた節を外したときは、ほかの選んでいる節のどれかを、それにする
                if (extraSelected.Count == 0) { Select(-1); return; }

                NoteData_SpaceHold next = null;
                foreach (var each in extraSelected) { next = each; break; }
                extraSelected.Remove(next);
                var keep = new List<NoteData_SpaceHold>(extraSelected);
                Select(chain.IndexOf(next));
                foreach (var each in keep) { extraSelected.Add(each); }
                return;
            }

            if (!extraSelected.Remove(note)) { extraSelected.Add(note); }
        }

        /// <summary>
        /// その節を選んでいるか (Ctrl+クリックで加えた節も含む)
        /// </summary>
        private bool IsSectionSelected(NoteData_SpaceHold note)
        {
            return note != null && (ReferenceEquals(note, selectedNote) || extraSelected.Contains(note));
        }

        /// <summary>
        /// 節を、1つだけ選んでいるか (頂点の道具・頂点の選択は、このときだけ使える)
        /// </summary>
        private bool IsSingleSectionSelected => selectedNote != null && selectedIndex >= 0 && extraSelected.Count == 0;

        /// <summary>
        /// 選んでいる節を、並びの順に集める
        /// </summary>
        private List<int> GetSelectedSectionIndices()
        {
            var indices = new List<int>();
            for (int i = 0; i < sections.Count; i++)
            {
                if (IsSectionSelected(sections[i].Note)) { indices.Add(i); }
            }
            return indices;
        }

        /// <summary>
        /// 無くなった節・頂点を、選択から外す。節を1つだけ選んでいないときは、頂点の選択と道具をやめる
        /// </summary>
        private void PruneSelection()
        {
            extraSelected.RemoveWhere(note => !chain.Contains(note) || ReferenceEquals(note, selectedNote));

            if (!IsSingleSectionSelected || !ReferenceEquals(selectedVerticesOwner, selectedNote))
            {
                ClearVertexSelection();
                selectedVerticesOwner = IsSingleSectionSelected ? selectedNote : null;
            }
            else
            {
                var vertices = selectedNote.SpaceVertices.Vertices;
                selectedVertices.RemoveAll(vertex => vertex.VertexIndex < 0 || vertex.VertexIndex >= vertices.Count || !ReferenceEquals(vertices[vertex.VertexIndex], vertex));
            }

            if (!IsSingleSectionSelected) { vertexTool = PreviewVertexTool.Move; }
        }

        private void ClearVertexSelection()
        {
            selectedVertices.Clear();
        }

        private bool IsVertexSelected(VertexData vertex)
        {
            foreach (var selected in selectedVertices)
            {
                if (ReferenceEquals(selected, vertex)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 頂点をクリックしたとき: その頂点を選ぶ (Ctrl を押していれば、加える・外す)
        /// </summary>
        private void ClickVertex(int sectionIndex, int vertexIndex)
        {
            if (!IsSingleSectionSelected || sectionIndex != selectedIndex) { return; }

            var vertices = selectedNote.SpaceVertices.Vertices;
            if (vertexIndex < 0 || vertexIndex >= vertices.Count) { return; }

            VertexData vertex = vertices[vertexIndex];
            selectedVerticesOwner = selectedNote;
            if (ModifierKey.Ctrl)
            {
                if (!selectedVertices.Remove(vertex)) { selectedVertices.Add(vertex); }
            }
            else
            {
                selectedVertices.Clear();
                selectedVertices.Add(vertex);
            }
        }

        /// <summary>
        /// 選んでいる頂点を消す (形を作るのに必要な数より少なくはならない。元に戻すのは1回ぶん)
        /// </summary>
        private void RemoveSelectedVertices()
        {
            if (!IsSingleSectionSelected || selectedVertices.Count == 0) { return; }
            RemoveVertices(selectedNote.SpaceVertices, new List<VertexData>(selectedVertices));
        }

        private void RemoveVertices(SpaceVertices vertices, List<VertexData> targets)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                if (ReferenceEquals(sections[i].Note.SpaceVertices, vertices) && IsLockedSection(i)) { ShowLockedStatus(); return; }
            }

            targets.RemoveAll(vertex => vertex.VertexIndex < 0 || vertex.VertexIndex >= vertices.Vertices.Count || !ReferenceEquals(vertices.Vertices[vertex.VertexIndex], vertex));
            if (targets.Count == 0) { return; }
            if (vertices.Vertices.Count - targets.Count < PreviewVertexTools.MIN_VERTEX_COUNT)
            {
                ShowStatus($"これ以上、頂点を消せません (形を作るには、頂点が {PreviewVertexTools.MIN_VERTEX_COUNT} つ以上必要です)");
                return;
            }

            // 番号の大きい順に消して、小さい順に戻す (戻すとき、それぞれが、消したときの番号の場所へ入るように)
            var ascending = new List<VertexData>(targets);
            ascending.Sort((a, b) => a.VertexIndex.CompareTo(b.VertexIndex));
            var descending = new List<VertexData>(ascending);
            descending.Reverse();

            Record(() => { foreach (var vertex in descending) { vertices.RemoveVertex(vertex); } },
                   () => { foreach (var vertex in ascending) { vertices.AddVertex(vertex); } });

            foreach (var vertex in targets) { selectedVertices.Remove(vertex); }
            ShowStatus(targets.Count > 1 ? $"頂点を {targets.Count} 個消しました" : "頂点を消しました");
        }

        #endregion

        #region キー操作

        private void HandleKeys()
        {
            if (InputFieldFocus.IsTyping || EditorMenuBar.IsMenuOpenOrJustClosed) { return; }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // ◆ の位置の窓を出しているときは、まず窓を閉じる。頂点の道具を選んでいるときは、まず道具をやめる
                if (IsControlPanelShown) { CloseControlPanel(); }
                else if (vertexTool != PreviewVertexTool.Move) { vertexTool = PreviewVertexTool.Move; }
                else { Close(); }
                return;
            }

            // Delete: 選んでいる頂点を消す。頂点を選んでいなければ、選んでいる節を消す
            if (Input.GetKeyDown(KeyCode.Delete) && !IsDragging && !EditorContextMenuView.IsOpen)
            {
                if (selectedVertices.Count > 0) { RemoveSelectedVertices(); }
                else { DeleteSections(GetSelectedSectionIndices()); }
            }

            if (Input.GetKeyDown(KeyCode.F)) { FrameSelectionOrAll(); }
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) { SetViewFront(); }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) { SetViewSide(); }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) { SetViewTop(); }
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) { SetViewOblique(); }

            // ↑ / ↓: 前後の節へ (長押しで、くり返す)
            bool isUp = upKeyRepeat.Poll();
            bool isDown = downKeyRepeat.Poll();
            if (!IsDragging)
            {
                if (isUp) { SelectNeighbor(1); }
                if (isDown) { SelectNeighbor(-1); }
            }
        }

        readonly KeyRepeat upKeyRepeat = new KeyRepeat(KeyCode.UpArrow);
        readonly KeyRepeat downKeyRepeat = new KeyRepeat(KeyCode.DownArrow);

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
        const float PANEL_MARGIN = 12f;
        // 向きの印を置く場所の高さ
        const float GIZMO_AREA_HEIGHT = 124f;

        /// <summary>
        /// 欄を作る
        /// ・右上: 視点の欄 (向きの印、透視と平行、全体を映す、奥行きの縮尺)
        /// 節の操作 (節を足す、頂点スライド、反転、前後の節) と、右下のボタン (頂点レイヤーで編集、編集画面へ戻る) は、
        /// シーンに置いてある画面部品 (SpaceHold3DOverlayView) を使う (ConnectOverlay)
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

            var projectionRow = view.AddPanelRow(viewPanel);
            projectionButton = view.AddRowButton(projectionRow, "透視", ToggleProjection);
            view.AddRowButton(projectionRow, "全体を映す (F)", FrameSelectionOrAll, 2f);

            var depthRow = view.AddPanelRow(viewPanel);
            view.AddRowLabel(depthRow, "奥行き", 52f);
            view.AddRowButton(depthRow, "-", () => ChangeDepthScale(-1));
            depthScaleLabel = view.AddRowLabel(depthRow, "x1", 50f);
            view.AddRowButton(depthRow, "+", () => ChangeDepthScale(1));

            BuildDescriptionRows();
        }

        /// <summary>
        /// 画面の右の「操作説明」の欄に、3D編集の操作の行を足す (シーンの行を複製して作る)
        /// 元に戻す・再生は、編集画面と同じなので、シーンの行をそのまま3D編集でも出す
        /// (ウィンドウの最小化と、Esc で閉じるの行は出さない (録音 20261117_005415)。Esc は、右下のボタンに書いてある)
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
                // (◆ のドラッグ・ダブルクリック・右クリックと、右上の XYZ の行は、出さない (録音 20261116_224815: いらない))
                // (F は、右上の「全体を映す (F)」のボタンに書いてあるので出さない。ミニプレビューに重ならない行数にするため)
                ("クリック", "節・頂点を選ぶ"),
                // (キーの欄に収まる長さにする: 幅 114)
                ("線をクリック×2", "節を頂点レイヤーで編集"),
                ("Ctrl+クリック", "節・頂点を複数選択"),
                ("ドラッグ", "頂点・節・矢印を動かす"),
                ("Delete", "選択中の頂点・節を削除"),
                ("右クリック", "メニューを出す"),
                ("右 / Alt+ドラッグ", "視点を回す"),
                ("中ドラッグ", "視点をずらす"),
                ("ホイール", "近づく / 離れる"),
                ("1 / 2 / 3 / 4", "正面 / 横 / 上 / 斜め"),
                ("↑ / ↓", "前後の節を選ぶ"),
            };

            for (int i = 0; i < rows.Length; i++)
            {
                description.AddRuntimeRow(TEMPLATE, $"SpaceHold3D_{i}", rows[i].key, rows[i].text, modes, LAYERS);
            }

            description.AddEditModeToRows(EditMode.SpaceHoldView, "Undo", "Play");
        }

        #region シーンの画面部品 (道具の列、前後の節、右下のボタン、◆ の位置の窓)

        /// <summary>
        /// シーンに置いてある画面部品を探して、ボタンをつなぐ (最初に開いたときに1回だけ)
        /// </summary>
        private void ConnectOverlay()
        {
            if (isOverlaySearched) { return; }
            isOverlaySearched = true;

            overlay = UnityEngine.Object.FindObjectOfType<SpaceHold3DOverlayView>(true);
            if (overlay == null)
            {
                Debug.LogWarning("【SpaceHoldの3D編集】シーンに SpaceHold3DOverlayView がありません (節の道具と、右下のボタンは出ません)");
                return;
            }

            overlay.OnAddSectionClicked += () => { if (isOpen) { InsertSectionsForSelection(); } };
            overlay.OnSlideClicked += delta => { if (isOpen) { SlideSelection(delta); } };
            overlay.OnMirrorClicked += isLeftRight => { if (isOpen) { MirrorSelection(isLeftRight); } };
            overlay.OnToolClicked += tool =>
            {
                if (!isOpen) { return; }
                // 選んでいる道具をもう一度押すと、道具をやめる (全画面プレビューの列と同じ)
                vertexTool = vertexTool == tool ? PreviewVertexTool.Move : tool;
            };
            overlay.OnVertexLayerClicked += () => { if (isOpen) { OpenSelectedInVerticesLayer(); } };
            // 頂点の編集を終える: 節の選択を外す (左上の列は、節の道具に戻る)
            overlay.OnEndVertexEditClicked += () =>
            {
                if (!isOpen) { return; }
                vertexTool = PreviewVertexTool.Move;
                Select(-1);
            };
            overlay.OnCloseClicked += () => { if (isOpen) { Close(); } };
            overlay.OnControlValueEntered += OnControlValueEntered;
            overlay.OnControlPanelCloseClicked += CloseControlPanel;
            overlay.OnControlStraightClicked += () =>
            {
                if (!isOpen || controlPanelStart == null || controlPanelEnd == null) { return; }
                StraightenSpan(controlPanelStart, controlPanelEnd);
                RefreshControlPanel();
            };
        }

        bool IsControlPanelShown => overlay != null && overlay.IsControlPanelShown;

        /// <summary>
        /// 左上の道具の列を、今の選択に合わせる (節を1つだけ選んでいれば、頂点の道具)
        /// </summary>
        private void RefreshOverlayTools()
        {
            if (overlay == null) { return; }

            overlay.SetListMode(IsSingleSectionSelected);
            overlay.SetTool(vertexTool);
            // 頂点レイヤーで編集: 節を1つだけ選んでいる間だけ出す (録音 20261117_024055)
            overlay.SetVertexLayerButtonVisible(IsSingleSectionSelected && !IsLockedSection(selectedIndex));
        }

        /// <summary>
        /// 右クリックのメニューを出す (頂点・節の中心)。◆ は、位置を数値で決める窓を出す
        /// </summary>
        private void ShowHandleMenu(Handle handle)
        {
            var items = new List<EditorContextMenuView.Item>();
            switch (handle.Kind)
            {
                case HandleKind.Vertex:
                {
                    if (handle.Section < 0 || handle.Section >= sections.Count) { return; }
                    var vertices = sections[handle.Section].Note.SpaceVertices;
                    if (handle.Vertex < 0 || handle.Vertex >= vertices.Vertices.Count) { return; }

                    VertexData vertex = vertices.Vertices[handle.Vertex];
                    NoteData_SpaceHold owner = sections[handle.Section].Note;
                    // ロックした節の頂点は消さない (メニューも出さない)
                    if (IsLockedSection(handle.Section)) { ShowLockedStatus(); return; }
                    items.Add(new EditorContextMenuView.Item("この頂点を消す", () => { if (IsLiveSection(owner)) { RemoveVertices(vertices, new List<VertexData> { vertex }); } }));
                    if (selectedVertices.Count > 1 && IsVertexSelected(vertex))
                    {
                        items.Add(new EditorContextMenuView.Item($"選択中の頂点を消す ({selectedVertices.Count} 個) (Delete)", RemoveSelectedVertices));
                    }
                    break;
                }
                case HandleKind.Control:
                {
                    // ◆: メニューを挟まず、ダブルクリックと同じ窓を出す (まっすぐに戻すのは、窓のボタンで)
                    if (handle.Span < 0 || handle.Span >= spans.Count) { return; }
                    OpenControlPanel(handle.Span);
                    return;
                }
                case HandleKind.Center:
                    ShowSectionMenu(handle.Section);
                    return;
            }

            EditorContextMenuView.Show(Input.mousePosition, items);
        }

        /// <summary>
        /// 節のメニューを出す (節の線・中心の点を右クリックしたとき、節を選んでいて何も無い所を右クリックしたとき)
        /// </summary>
        private void ShowSectionMenu(int index)
        {
            if (index < 0 || index >= sections.Count) { return; }

            NoteData_SpaceHold note = sections[index].Note;
            if (!IsSectionSelected(note)) { Select(index); }

            bool isLocked = NoteLock.IsLocked(note);
            bool isEnd = index == 0 || index == sections.Count - 1;
            var items = new List<EditorContextMenuView.Item>();

            // (メニューを開いている間に、元に戻すなどで節が無くなっていたら、何もしない)
            if (!isLocked)
            {
                items.Add(new EditorContextMenuView.Item("この節を頂点レイヤーで編集", () => { if (IsLiveSection(note)) { OpenSectionInVerticesLayer(note); } }));
            }
            items.Add(new EditorContextMenuView.Item("この形を、ホールド全体に反映", () => { if (IsLiveSection(note)) { ApplyShapeToAll(note); } }));
            if (!isLocked)
            {
                items.Add(new EditorContextMenuView.Item("この節の後ろに節を足す (補間)", () => { if (IsLiveSection(note)) { InsertSectionAfter(chain.IndexOf(note)); } }));
            }
            if (!isLocked && !isEnd)
            {
                bool isMeshRelay = note.NoteType == DeploymentNoteType.SpaceHoldMeshRelay;
                items.Add(isMeshRelay
                    // (種類の名前は英語のまま書く: 録音 20261117_031852)
                    ? new EditorContextMenuView.Item("SpaceHoldRelay にする", () => { if (IsLiveSection(note)) { ChangeSectionType(note, DeploymentNoteType.SpaceHoldRelay); } })
                    : new EditorContextMenuView.Item("SpaceHoldMeshRelay にする", () => { if (IsLiveSection(note)) { ChangeSectionType(note, DeploymentNoteType.SpaceHoldMeshRelay); } }));
            }
            items.Add(isLocked
                ? new EditorContextMenuView.Item("ロックを外す", () => { if (IsLiveSection(note)) { NoteLock.Unlock(new[] { note }); RefreshSpans(); } })
                : new EditorContextMenuView.Item("ロックする", () => { if (IsLiveSection(note)) { NoteLock.Lock(new[] { note }); RefreshSpans(); } }));
            if (!isLocked)
            {
                items.Add(new EditorContextMenuView.Item("この節を消す (Delete)", () =>
                {
                    int current = chain.IndexOf(note);
                    if (current >= 0) { DeleteSections(new List<int> { current }); }
                }));
            }

            EditorContextMenuView.Show(Input.mousePosition, items);
        }

        /// <summary>
        /// その節が、まだ開いている SpaceHold の中にあるか (メニューや窓が、消えた節に働かないように)
        /// </summary>
        private bool IsLiveSection(NoteData_SpaceHold note)
        {
            return note != null && chain.Contains(note) && notesGetter.GetNoteObject(note) != null;
        }

        /// <summary>
        /// その節を、頂点レイヤーで編集する (3D編集は閉じる。ロックした節は開かない)
        /// </summary>
        private void OpenSectionInVerticesLayer(NoteData_SpaceHold note)
        {
            if (note == null) { return; }
            if (NoteLock.IsLocked(note)) { ShowLockedStatus(); return; }

            Close();
            dataSetter.SetEditNoteType(EditNoteType.Vertices);
            notesSetter.SetEditingVertices(note);
        }

        /// <summary>
        /// その節の形を、ほかの節すべてに反映する (それぞれの節の位置 (頂点の中心) は、今のまま。ロックした節は変えない。元に戻すのは1回ぶん)
        /// (録音 20261117_023744: ハートの形の節から反映すると、ホールド全体がハートの形のトンネルになる)
        /// </summary>
        private void ApplyShapeToAll(NoteData_SpaceHold source)
        {
            var sourceVertices = source.SpaceVertices.Vertices;
            if (sourceVertices.Count < PreviewVertexTools.MIN_VERTEX_COUNT) { return; }

            Vector2 sourceCenter = GetVertexCenter(source.SpaceVertices);
            var shape = new List<Vector2>();
            foreach (var vertex in sourceVertices) { shape.Add(vertex.Position.Value - sourceCenter); }

            // 元に戻す・やり直すは、同じ頂点のオブジェクトを入れ替える (前の履歴 (頂点の移動など) が、その頂点を探せるように)
            var targets = new List<NoteData_SpaceHold>();
            var before = new List<List<VertexData>>();
            var after = new List<List<VertexData>>();
            int lockedCount = 0;
            int shrunkCount = 0;
            foreach (var section in sections)
            {
                if (ReferenceEquals(section.Note, source)) { continue; }
                if (NoteLock.IsLocked(section.Note)) { lockedCount++; continue; }

                var vertices = section.Note.SpaceVertices;
                Vector2 center = GetVertexCenter(vertices);
                // ノーツの面 (単位円) からはみ出す節は、形ごと縮めて収める (頂点ごとに寄せると、2つが同じ所に重なることがある)
                float scale = GetFitScale(center, shape);
                if (scale < 1f) { shrunkCount++; }

                var next = new List<VertexData>(shape.Count);
                foreach (var point in shape) { next.Add(new VertexData(center + point * scale)); }

                targets.Add(section.Note);
                before.Add(new List<VertexData>(vertices.Vertices));
                after.Add(next);
            }

            if (targets.Count == 0)
            {
                ShowStatus(lockedCount > 0 ? "ほかの節は、すべてロックしてあります" : "ほかの節がありません");
                return;
            }

            ClearVertexSelection();
            Record(() => { for (int i = 0; i < targets.Count; i++) { targets[i].SpaceVertices.ReplaceVertices(after[i]); } },
                   () => { for (int i = 0; i < targets.Count; i++) { targets[i].SpaceVertices.ReplaceVertices(before[i]); } });

            string note = (lockedCount > 0 ? $" (ロックした {lockedCount} 個はそのまま)" : "") + (shrunkCount > 0 ? $" (面に収まるよう縮めた節 {shrunkCount} 個)" : "");
            ShowStatus($"この節の形を、ほかの {targets.Count} 個の節に反映しました" + note);
        }

        /// <summary>
        /// 中心 center に形 shape (中心からの位置) を置くとき、ノーツの面 (単位円) に収まる、いちばん大きい倍率 (1 まで)
        /// </summary>
        private static float GetFitScale(Vector2 center, List<Vector2> shape)
        {
            const float LIMIT = 0.999f;
            float rest = LIMIT * LIMIT - center.sqrMagnitude;
            if (rest <= 0f) { return 0f; }

            float scale = 1f;
            foreach (var point in shape)
            {
                if ((center + point).sqrMagnitude <= LIMIT * LIMIT) { continue; }

                // |center + s * point| = LIMIT を s について解く (s > 0 の方)
                float a = point.sqrMagnitude;
                float b = Vector2.Dot(center, point);
                float s = (-b + Mathf.Sqrt(b * b + a * rest)) / a;
                scale = Mathf.Min(scale, s);
            }
            return Mathf.Max(scale, 0f);
        }

        private static Vector2 GetVertexCenter(SpaceVertices vertices)
        {
            int count = vertices.Vertices.Count;
            if (count == 0) { return Vector2.zero; }

            Vector2 sum = Vector2.zero;
            foreach (var vertex in vertices.Vertices) { sum += vertex.Position.Value; }
            return sum / count;
        }

        /// <summary>
        /// つまみ (頂点・中心・矢印・◆) が属している節 (無ければ null)
        /// </summary>
        private NoteData_SpaceHold GetHandleNote(Handle handle)
        {
            if (handle.Kind == HandleKind.Control)
            {
                return handle.Span >= 0 && handle.Span < spans.Count ? sections[spans[handle.Span].StartIndex].Note : null;
            }
            return handle.Section >= 0 && handle.Section < sections.Count ? sections[handle.Section].Note : null;
        }

        /// <summary>
        /// 途中の節の種類を変える (SpaceHoldRelay ⇔ SpaceHoldMeshRelay。元に戻せる)
        /// </summary>
        private void ChangeSectionType(NoteData_SpaceHold note, DeploymentNoteType type)
        {
            DeploymentNoteType before = note.NoteType;
            if (before == type) { return; }

            Record(() => note.SetNoteType(type), () => note.SetNoteType(before));
            RefreshSpans();
            ShowStatus(type == DeploymentNoteType.SpaceHoldRelay ? "SpaceHoldRelay にしました" : "SpaceHoldMeshRelay にしました");
        }

        /// <summary>
        /// 節を消す (ロックした節は消さない。節は2つ以上残す。元に戻すのは1回ぶん)
        /// </summary>
        private void DeleteSections(List<int> indices)
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || indices == null || indices.Count == 0) { return; }

            int lockedCount = indices.RemoveAll(IsLockedSection);
            if (indices.Count == 0)
            {
                if (lockedCount > 0) { ShowLockedStatus(); }
                return;
            }
            if (sections.Count - indices.Count < 2)
            {
                ShowStatus("節は2つ以上必要です。これ以上、節を消せません");
                return;
            }

            var notes = new List<NoteData_SpaceHold>();
            var addresses = new List<AddressWithinRange>();
            var types = new List<DeploymentNoteType>();
            foreach (int index in indices)
            {
                if (index < 0 || index >= sections.Count) { continue; }
                NoteData_SpaceHold note = sections[index].Note;
                notes.Add(note);
                addresses.Add(new AddressWithinRange(note.Address));
                types.Add(note.NoteType);
            }
            if (notes.Count == 0) { return; }

            // 頂点レイヤーで編集する節を消すときは、先に、残る節 (ロックしていないもの) にしておく
            if (notesGetter.EditingVertices.Value is NoteData_SpaceHold editing && notes.Contains(editing))
            {
                foreach (var section in sections)
                {
                    if (!notes.Contains(section.Note) && !NoteLock.IsLocked(section.Note))
                    {
                        notesSetter.SetEditingVertices(section.Note);
                        break;
                    }
                }
            }

            Select(-1);
            ClearVertexSelection();
            lastEditing = notesGetter.EditingVertices.Value;
            Record(() => { foreach (var note in notes) { chartData.RemoveNote(note, false); } },
                   () =>
                   {
                       for (int i = 0; i < notes.Count; i++)
                       {
                           notes[i].SetAddress(addresses[i]);
                           notes[i].SetNoteType(types[i]);
                           chartData.AddNote(notes[i]);
                       }
                   });

            ShowStatus(notes.Count > 1 ? $"節を {notes.Count} 個消しました" : "節を消しました");
        }

        /// <summary>
        /// 2つの節の間を、まっすぐに戻す (間のメッシュ中継点を消す)
        /// </summary>
        private void StraightenSpan(NoteData_SpaceHold start, NoteData_SpaceHold end)
        {
            for (int i = 0; i < spans.Count; i++)
            {
                if (!ReferenceEquals(sections[spans[i].StartIndex].Note, start) || !ReferenceEquals(sections[spans[i].EndIndex].Note, end)) { continue; }

                Span span = spans[i];
                int count = CommitCurve(span.StartIndex, span.EndIndex, SpaceHoldCurveMath.GetStraightControl(sections[span.StartIndex], sections[span.EndIndex]));
                if (count >= 0) { ShowStatus("2つの節の間を、まっすぐに戻しました"); }
                return;
            }
        }

        /// <summary>
        /// 3D編集を開いていないとき、スペースのレイヤーで、2つ以上の部品からなる SpaceHold (の部品) だけを選んでいるなら、開ける
        /// (ミニプレビューの横の「3Dで編集」。ほかの種類のノーツや、別の SpaceHold も選んでいるときは出さない。録音 20261117_013612)
        /// </summary>
        public bool CanOpenFromSpaceLayerSelection()
        {
            if (isOpen || isBuildFailed) { return false; }
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Space) { return false; }

            return TryGetSelectedSpaceHold(out _);
        }

        /// <summary>
        /// 選んでいる SpaceHold を、3D編集で開く
        /// </summary>
        public void OpenFromSpaceLayerSelection()
        {
            if (!CanOpenFromSpaceLayerSelection()) { return; }
            if (TryGetSelectedSpaceHold(out NoteData_SpaceHold target)) { Open(target); }
        }

        private bool TryGetSelectedSpaceHold(out NoteData_SpaceHold target)
        {
            target = null;
            int chainIndex = -1;
            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (selecting is not NoteData_SpaceHold spaceHold) { return false; }
                if (target == null)
                {
                    target = spaceHold;
                    chainIndex = spaceHold.ChainIndex.Value;
                }
                else if (spaceHold.ChainIndex.Value != chainIndex) { target = null; return false; }
            }
            if (target == null || chainIndex < 0 || notesGetter.GetNoteObject(target) == null) { target = null; return false; }

            var list = notesGetter.GetChainNoteList(chainIndex)?.ChainNoteList;
            if (list == null || list.Count < 2) { target = null; return false; }
            return true;
        }

        /// <summary>
        /// 3D編集を開いていないとき、頂点レイヤーで SpaceHold の節を編集しているなら、開ける (ミニプレビューの横の「3Dで編集」)
        /// </summary>
        public bool CanOpenFromVerticesLayer()
        {
            if (isOpen || isBuildFailed) { return false; }
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return false; }

            return notesGetter.EditingVertices.Value is NoteData_SpaceHold editing && notesGetter.GetNoteObject(editing) != null;
        }

        /// <summary>
        /// 頂点レイヤーで編集している SpaceHold を、3D編集で開く
        /// </summary>
        public void OpenFromVerticesLayer()
        {
            if (!CanOpenFromVerticesLayer()) { return; }
            Open((NoteData_SpaceHold)notesGetter.EditingVertices.Value);
        }

        /// <summary>
        /// ◆ の位置の窓を、その区間について出す
        /// </summary>
        private void OpenControlPanel(int spanIndex)
        {
            if (overlay == null || spanIndex < 0 || spanIndex >= spans.Count) { return; }

            Span span = spans[spanIndex];
            controlPanelStart = sections[span.StartIndex].Note;
            controlPanelEnd = sections[span.EndIndex].Note;

            overlay.ShowControlPanel(GetControlPanelPosition(span));
            RefreshControlPanel();
            ShowStatus("◆ の位置を、数値で決められます (Z: 0 = 前の節、1 = 次の節)");
        }

        private void CloseControlPanel()
        {
            if (overlay != null) { overlay.HideControlPanel(); }
            controlPanelStart = null;
            controlPanelEnd = null;
        }

        /// <summary>
        /// ◆ の位置の窓の、表示と場所を、今の ◆ に合わせる (区間が無くなったら、窓を閉じる)
        /// </summary>
        private void RefreshControlPanel()
        {
            if (!IsControlPanelShown) { return; }

            if (!TryFindControlPanelSpan(out int spanIndex))
            {
                CloseControlPanel();
                return;
            }

            Span span = spans[spanIndex];
            Vector3 control = dragKind == DragKind.Control && dragSpanIndex == spanIndex ? dragControl : span.Control;
            overlay.SetControlValues(control.x, control.y, GetControlRatio(span, control));
            overlay.MoveControlPanel(GetControlPanelPosition(span));
        }

        private bool TryFindControlPanelSpan(out int spanIndex)
        {
            spanIndex = -1;
            if (controlPanelStart == null || controlPanelEnd == null) { return false; }

            for (int i = 0; i < spans.Count; i++)
            {
                if (ReferenceEquals(sections[spans[i].StartIndex].Note, controlPanelStart) && ReferenceEquals(sections[spans[i].EndIndex].Note, controlPanelEnd))
                {
                    spanIndex = i;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 窓を出す場所 (◆ の右。画面部品の単位で、左下からの位置)
        /// </summary>
        private Vector2 GetControlPanelPosition(Span span)
        {
            Vector3 control = span.Control;
            if (!view.TryProject(ToWorld(new Vector2(control.x, control.y), control.z), out Vector2 local)) { local = view.ViewportSize * 0.5f; }
            return local + new Vector2(CONTROL_PANEL_OFFSET, 0f);
        }

        /// <summary>
        /// ◆ の奥行きが、2つの節の間のどこにあるか (0 = 前の節、1 = 次の節)
        /// </summary>
        private float GetControlRatio(Span span, Vector3 control)
        {
            float a = sections[span.StartIndex].Depth;
            float b = sections[span.EndIndex].Depth;
            return Mathf.Abs(b - a) <= 1e-6f ? 0.5f : (control.z - a) / (b - a);
        }

        /// <summary>
        /// ◆ の位置の窓に、数値が入力されたとき: その位置に ◆ を置いて、間のメッシュ中継点を置き直す (元に戻せる)
        /// </summary>
        /// <param name="axis">0 = X (横)、1 = Y (縦)、2 = Z (2つの節の間のどこか。0〜1)</param>
        private void OnControlValueEntered(int axis, float value)
        {
            if (!isOpen || IsDragging) { return; }
            if (!TryFindControlPanelSpan(out int spanIndex)) { return; }

            Span span = spans[spanIndex];
            Vector3 control = span.Control;
            switch (axis)
            {
                case 0: control.x = value; break;
                case 1: control.y = value; break;
                default:
                {
                    // 2つの節そのものの位置には置けない (曲線が時間をさかのぼらないように、少し内側まで)
                    float ratio = Mathf.Clamp(value, SpaceHoldCurveMath.CONTROL_DEPTH_MARGIN, 1f - SpaceHoldCurveMath.CONTROL_DEPTH_MARGIN);
                    float a = sections[span.StartIndex].Depth;
                    float b = sections[span.EndIndex].Depth;
                    control.z = a + (b - a) * ratio;
                    break;
                }
            }

            int count = CommitCurve(span.StartIndex, span.EndIndex, control);
            if (count >= 0) { ShowStatus($"◆ の位置を数値で決めました (メッシュ中継点 {count} 個)"); }
        }

        #endregion

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

        /// <summary>
        /// 選んでいる節すべての、頂点の番号をずらす (元に戻すのは1回ぶん)
        /// </summary>
        private void SlideSelection(int delta)
        {
            var indices = GetSelectedSectionIndices();
            if (indices.Count == 0) { ShowStatus("節を選んでください"); return; }
            // ロックした節は飛ばす
            if (indices.RemoveAll(IsLockedSection) > 0 && indices.Count == 0) { ShowLockedStatus(); return; }

            BeginTransaction();
            try { foreach (int index in indices) { PreviewVertexTools.Slide(sections[index].Note.SpaceVertices, delta); } }
            finally { EndTransaction(); }
        }

        /// <summary>
        /// 選んでいる節すべてを、反転する (元に戻すのは1回ぶん)
        /// </summary>
        private void MirrorSelection(bool isLeftRight)
        {
            var indices = GetSelectedSectionIndices();
            if (indices.Count == 0) { ShowStatus("節を選んでください"); return; }
            if (indices.RemoveAll(IsLockedSection) > 0 && indices.Count == 0) { ShowLockedStatus(); return; }

            BeginTransaction();
            try { foreach (int index in indices) { PreviewVertexTools.Mirror(sections[index].Note.SpaceVertices, isLeftRight); } }
            finally { EndTransaction(); }
        }

        /// <summary>
        /// 選んでいる節それぞれの後ろ (終点なら前) に、補間した形のメッシュ中継点を足す (元に戻すのは1回ぶん)
        /// </summary>
        private void InsertSectionsForSelection()
        {
            var indices = GetSelectedSectionIndices();
            if (indices.Count == 0) { ShowStatus("節を選んでください"); return; }

            // 後ろから足す (前の節の番号が、足したことで変わらないように)
            var gaps = new SortedSet<int>();
            foreach (int index in indices)
            {
                int gap = index < sections.Count - 1 ? index : index - 1;
                if (gap >= 0) { gaps.Add(gap); }
            }
            if (gaps.Count == 0) { ShowStatus("つながっている節がありません"); return; }

            int added = 0;
            var ordered = new List<int>(gaps);
            ordered.Reverse();
            BeginTransaction();
            try
            {
                foreach (int gap in ordered)
                {
                    if (SpaceHoldFilmStrip.TryInsertInterpolatedSection(dataGetter.ChartData.Value, timeline, sections, gap, out _)) { added++; }
                }
            }
            finally { EndTransaction(); }

            ShowStatus(added > 0 ? $"補間した形のメッシュ中継点を {added} 個足しました" : "足せる分線がありません (2つの節の間に、空いた分線が必要です)");
        }

        /// <summary>
        /// その節の後ろ (終点なら前) に、補間した形のメッシュ中継点を足す (右クリックのメニューから)
        /// </summary>
        private void InsertSectionAfter(int index)
        {
            if (index < 0 || index >= sections.Count) { return; }

            int gap = index < sections.Count - 1 ? index : index - 1;
            if (gap < 0) { ShowStatus("つながっている節がありません"); return; }

            if (SpaceHoldFilmStrip.TryInsertInterpolatedSection(dataGetter.ChartData.Value, timeline, sections, gap, out NoteData_SpaceHold inserted))
            {
                selectedNote = inserted;
                extraSelected.Clear();
                notesSetter.SetEditingVertices(inserted);
                lastEditing = inserted;
                ShowStatus("補間した形のメッシュ中継点を足しました");
            }
            else
            {
                ShowStatus("足せる分線がありません (2つの節の間に、空いた分線が必要です)");
            }
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
