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
    /// ・軌跡 (橙の線。各節の中心 = 形の重心 を通る)、0番の頂点の通り道 (桃色の線。ねじれを見るため)
    /// ・ノーツの面 (下半分の半円の筒) のレーンの線と、小節線の位置の弧
    /// ・ゲームと違い、軌道の曲がり (上り坂) は外して、奥行き (時間) をまっすぐに並べてある。奥行きの縮尺は変えられる
    ///
    /// 画面:
    /// ・右上: 視点のウィンドウ。向きの印 (Unity のシーンビューと同じ。X・Y・Z の軸の先を押すと、その向きから見る。まんなかで透視と平行の切り替え)、
    ///   全体を映す、奥行きの縮尺 (正面・横・上・斜めのボタンは、向きの印で足りるので置かない。キーの 1〜4 は使える)
    /// ・左上: 頂点レイヤーの道具の列と同じ見た目の、節の形の道具 (編集・回転・拡大縮小・頂点スライド・反転)。いつも出す (録音 20261117_201649)
    ///   「編集」は、道具を選んでいない状態 (節を選ぶ・動かす。開いたときは、これ: 録音 20261117_204809)
    ///   その右に、回転・拡大縮小の動かし方 (通常 / なめらか)、選択モード (通常 / 全体 / Nつおき)、節の選択を外すボタン
    ///   (Nつおきの N は、設定メニューで 1〜4 から選ぶ: 録音 20261117_205010)
    ///   (3D編集では、頂点を選んだり、足したり、動かしたり、消したりしない。頂点の編集は頂点レイヤーで: 録音 20261117_180313)
    /// ・右下: ミニプレビュー (編集画面のもの)。その左に「頂点レイヤーで編集」と「編集画面へ戻る」のボタン
    ///   (道具の列とボタンは、シーンに置いてある画面部品: SpaceHold3DOverlayView)
    /// ・右: 操作説明 (編集画面と同じウィンドウに、3D編集の操作の行を出す。行はシーンに置いてある)
    /// (3Dの絵・視点のウィンドウ・下の文字も、シーンに置いてある画面部品: SpaceHold3DViewUi)
    /// ・下のまんなか: 選んでいる節と、最後の操作
    ///
    /// 操作:
    /// ・カメラ: 右ドラッグ (または Alt+左ドラッグ、何も無い所の左ドラッグ) で回す、中ドラッグでずらす、ホイールで近づく・離れる、
    ///   F で全体 (節を選んでいれば、その節) を映す、1〜4 で 正面・横・上・斜め から見る
    /// ・節を選ぶ: 節の中心の点か、節の形の線を押す。↑↓で前後の節へ。選んだ節は、頂点レイヤーで編集する節にもなる
    ///   Ctrl+クリックで選択に加える・外す、Shift+クリックで間の節をまとめて選ぶ (節の道具は、選んだ節すべてに働く)
    /// ・右クリック: ◆ = 位置を数値で決める窓。節 (線・中心・選んでいる節) = 節のメニュー
    ///   (頂点レイヤーで編集、この形をホールド全体に反映、節を足す、リレー / メッシュリレーにする、ロック、消す。録音 20261117_023636 / 023744)
    /// ・Delete: 選んでいる節を消す (録音 20261117_023852)
    /// ・節の中心の点をドラッグ: 節全体を動かす (節の面の中で)
    /// ・選んだ節の、矢印の先 (赤: 横、緑: 縦、青: 時間) をドラッグ: その向きにだけ動かす。時間は分線1本ずつ (前後の節は追い越さない)
    /// ・道具「回転」「拡大縮小」: 押したところから、選んでいる節の形を丸ごと回す / 拡大・縮小する (動かし方「なめらか」では、ねじれるように)
    /// ・◆ (制御点) をドラッグ: 2つの節 (メッシュ中継点を飛ばした、隣り合った節) の間を、ベジェ曲線で曲げる。
    ///   離したときに、間のメッシュ中継点を、形を保ったまま曲線に合わせて動かす (曲線に近づけるのに要る分線には、中継点を足す: 録音 20261117_194644)。
    ///   ◆は、カメラに向いた面の中で動く (横から見ると、縦と時間の向きに動く)。時間の向きは、2つの節の間から出ない。
    ///   ◆を右クリック: 横に小さい窓が出て、位置を数値で決められる (X・Y は断面の中の位置、Z は2つの節の間のどこか:
    ///   0 = 前の節、1 = 次の節)。窓の「まっすぐに戻す」で、2つの節の間をまっすぐに戻す (メッシュ中継点の形は残す)
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

        // 全画面プレビューへ行くときに閉じた SpaceHold。プレビューから同じレイヤーへ戻ったら、また開く (録音 20261117_161438)
        // (選んでいた節と、つながりの節すべてを覚える。プレビューで、選んでいた節を元に戻して消しても、残っている節で開けるように)
        NoteData_SpaceHold reopenAfterPreview;
        readonly List<NoteData_SpaceHold> reopenChain = new List<NoteData_SpaceHold>();
        ChartData reopenChart;
        EditNoteType reopenLayer;
        // 開いている間の、編集画面のレイヤー (スペース、または頂点)
        EditNoteType layerWhileOpen;
        // つながりを探す手がかり (開いた SpaceHold のノーツのどれか)
        NoteData_SpaceHold anchor;
        readonly List<NoteData_SpaceHold> chain = new List<NoteData_SpaceHold>();
        // chain と同じ節 (毎フレーム「つながりの中にあるか」を調べるので、早く引けるように。RefreshChain で作り直す)
        readonly HashSet<NoteData_SpaceHold> chainSet = new HashSet<NoteData_SpaceHold>();
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

        // シーンに置いてある画面部品 (道具の列、右下のボタン、◆ の位置の窓)
        SpaceHold3DOverlayView overlay;
        bool isOverlaySearched;
        // ◆ の位置の窓で見せている区間 (区間の2つの節。番号は、節が増えたり減ったりすると変わるので、ノーツで覚える)
        NoteData_SpaceHold controlPanelStart;
        NoteData_SpaceHold controlPanelEnd;
        // 窓で、中心の位置を出している節 (右クリックのメニューと一緒に出す。録音 20261118_020916)
        NoteData_SpaceHold centerPanelNote;
        // ◆ を、窓のどれだけ右に出すか (画面部品の単位)
        const float CONTROL_PANEL_OFFSET = 24f;

        // 選んでいる節のほかに、選んでいる節 (Ctrl+クリックで加えたもの)
        readonly HashSet<NoteData_SpaceHold> extraSelected = new HashSet<NoteData_SpaceHold>();
        // 選択から外す節か (PruneSelection が毎フレーム使う。そのたびに作らないように、1回だけ作っておく)
        readonly Predicate<NoteData_SpaceHold> isStaleSelection;
        // 節の道具 (回転・拡大縮小。Move = 道具を選んでいない)。節を選んでいなくても、選んだままにする (録音 20261117_201649)
        PreviewVertexTool vertexTool = PreviewVertexTool.Move;
        // 回転・拡大縮小の動かし方 (録音 20261117_180817 / 190722。閉じても覚えておく)
        SectionTransformRange transformRange = SectionTransformRange.Single;
        // 節をクリックしたときの選び方 (録音 20261117_190401。閉じても覚えておく)
        SectionSelectMode selectMode = SectionSelectMode.Normal;

        // 選択モード「Nつおき」の N (設定メニューで選ぶ。次に起動したときも同じにする: 録音 20261117_205010)
        const string ALTERNATE_INTERVAL_PREFS_KEY = "ChartEditor.SpaceHold3D.AlternateInterval";
        const int DEFAULT_ALTERNATE_INTERVAL = 1;
        static readonly int[] ALTERNATE_INTERVAL_CHOICES = { 1, 2, 3, 4 };
        static int alternateInterval = -1;

        /// <summary>
        /// 選択モード「Nつおき」の N (1 = 1つおき: クリックした節と、そこから2つ目ごとの節を選ぶ)。選べる値は 1〜4
        /// </summary>
        public static int AlternateInterval
        {
            get
            {
                if (alternateInterval < 0)
                {
                    int saved = PlayerPrefs.GetInt(ALTERNATE_INTERVAL_PREFS_KEY, DEFAULT_ALTERNATE_INTERVAL);
                    alternateInterval = Array.IndexOf(ALTERNATE_INTERVAL_CHOICES, saved) >= 0 ? saved : DEFAULT_ALTERNATE_INTERVAL;
                }
                return alternateInterval;
            }
            set
            {
                // 選べる値のほかは受け付けない
                if (Array.IndexOf(ALTERNATE_INTERVAL_CHOICES, value) < 0) { return; }
                alternateInterval = value;
                PlayerPrefs.SetInt(ALTERNATE_INTERVAL_PREFS_KEY, alternateInterval);
                PlayerPrefs.Save();
            }
        }
        // 道具「回転」「拡大縮小」のキー (頂点レイヤーの同じ道具と同じキー。Alt と一緒に押す)
        KeyCode rotateKey = KeyCode.None;
        KeyCode scaleKey = KeyCode.None;

        float DepthScale => depthScaleSteps[depthScaleStep];

        public SpaceHold3DEditor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            isStaleSelection = note => !chainSet.Contains(note) || ReferenceEquals(note, selectedNote);

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
            ForgetControlsOfOtherChart();

            if (!isOpen)
            {
                TryReopenAfterPreview();
                if (isOpen) { return; }
                HandleOpenGesture();
                return;
            }

            try
            {
                if (ShouldClose())
                {
                    RememberForPreview();
                    Close();
                    return;
                }
                layerWhileOpen = dataGetter.EditNoteType.Value;

                RefreshChain();
                if (sections.Count == 0)
                {
                    Close();
                    return;
                }

                // 説明書を開いている間は、何もしない (モードを戻すと、説明書がすぐ閉じてしまう: 録音 20261117_174214)
                // 説明書を閉じると、モードが戻るので、次のフレームから、また3D編集のモードにする
                if (dataGetter.CurrentEditMode.Value == EditMode.Explanation)
                {
                    if (IsDragging) { CancelDrag(); }
                    SetCursor(EditMode.None);
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
            ShowStatus("ロックした節は、動かせません (外すには、右クリックのメニューの「ロックを外す」)");
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
                    ConnectViewPanel();
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
            layerWhileOpen = dataGetter.EditNoteType.Value;
            ForgetReopen();
            isOpen = true;
            waitForMouseRelease = true;
            CancelDrag();

            EditorMenuBar.IsBlockedByOverlay = true;
            dataSetter.SetEditMode(EditMode.SpaceHoldView);

            RefreshChain();
            depthOrigin = sections.Count > 0 ? sections[0].Depth : 0f;
            nextContextRefreshTime = 0f;
            tunnelKey.Clear();

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
            // (押しただけの右クリック・空いた所のクリックは、やめるだけ。終えると、メニューを出したり、選択を外したりしてしまう)
            if (dragKind == DragKind.RightPress || dragKind == DragKind.EmptyPress) { CancelDrag(); }
            else { FinishDrag(); }
            // ◆ の位置の窓で打っている途中の値も、反映してから閉じる
            CloseControlPanel();
            EditorContextMenuView.Hide();
            SetCursor(EditMode.None);
            if (overlay != null) { overlay.SetVisible(false); }
            extraSelected.Clear();
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
            chainSet.Clear();
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
                case EditMode.VertexPlace:
                case EditMode.VerticesRotate:
                case EditMode.VerticesScale:
                    return dataGetter.EditNoteType.Value == EditNoteType.Vertices ? modeBeforeOpen : EditMode.None;
                default:
                    return dataGetter.EditNoteType.Value == EditNoteType.Vertices ? EditMode.VertexEdit : EditMode.None;
            }
        }

        /// <summary>
        /// 全画面プレビューへ行くので閉じるとき: 開いていた SpaceHold (選んでいた節と、つながりの節すべて) を覚えておく
        /// </summary>
        private void RememberForPreview()
        {
            ForgetReopen();
            if (dataGetter.EditNoteType.Value != EditNoteType.Preview || dataGetter.IsLoadingChart.Value) { return; }

            reopenAfterPreview = selectedNote ?? anchor;
            if (reopenAfterPreview == null) { return; }
            reopenChain.AddRange(chain);
            reopenChart = openedChart;
            reopenLayer = layerWhileOpen;
        }

        /// <summary>
        /// 全画面プレビューから、3D編集を開いていたときのレイヤーへ戻ったら、また開く
        /// 選んでいた節が無くなっていたら (プレビューで元に戻したなど)、つながりの節のうち、残っている最初の節で開く
        /// (プレビューで別のレイヤーのノーツを選んで戻ったとき、譜面を読み込んだとき、その SpaceHold が無くなったときは開かない)
        /// </summary>
        private void TryReopenAfterPreview()
        {
            if (reopenAfterPreview == null) { return; }
            // 別の譜面になったら、やめる (前の譜面のノーツを持ち続けないように、すぐ捨てる)
            if (!ReferenceEquals(dataGetter.ChartData.Value, reopenChart)) { ForgetReopen(); return; }
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return; }

            bool canReopen = dataGetter.EditNoteType.Value == reopenLayer && !dataGetter.IsLoadingChart.Value;
            NoteData_SpaceHold note = canReopen ? FindReopenNote() : null;
            ForgetReopen();
            if (note != null) { Open(note); }
        }

        /// <summary>
        /// また開くときのノーツ: 選んでいた節。無くなっていたら、つながりの節のうち、残っている最初の節 (どれも無ければ null)
        /// </summary>
        private NoteData_SpaceHold FindReopenNote()
        {
            if (notesGetter.GetNoteObject(reopenAfterPreview) != null) { return reopenAfterPreview; }

            foreach (var note in reopenChain)
            {
                if (note != null && notesGetter.GetNoteObject(note) != null) { return note; }
            }
            return null;
        }

        /// <summary>
        /// プレビューのあとに開き直すために覚えていたものを捨てる
        /// </summary>
        private void ForgetReopen()
        {
            reopenAfterPreview = null;
            reopenChain.Clear();
            reopenChart = null;
        }

        /// <summary>
        /// 別の譜面になったら、覚えている制御点を捨てる (前の譜面のノーツを持ち続けないように)
        /// </summary>
        private void ForgetControlsOfOtherChart()
        {
            if (controlsChart == null || ReferenceEquals(controlsChart, dataGetter.ChartData.Value)) { return; }

            controls.Clear();
            controlsChart = null;
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
                chainSet.Clear();
                sections.Clear();
                spans.Clear();
                selectedIndex = -1;
                return;
            }

            // (譜面から消されたノーツは、つながりの一覧からも外れている。NoteObjectsBinder)
            SpaceHoldSections.GetChain(notesGetter, anchor, chain);
            chainSet.Clear();
            foreach (var note in chain) { chainSet.Add(note); }
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
            if (editing is NoteData_SpaceHold spaceHold && chainSet.Contains(spaceHold))
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
        /// 節をクリックしたとき (Ctrl なし): 選択モードと Shift に合わせて選ぶ (録音 20261117_190401)
        /// ・Shift: 今の節 (いちばん手前に選んでいる節) から、クリックした節までを、まとめて選ぶ
        /// ・通常: その節だけ / 全体: 節を全部 (クリックした節が、いちばん手前) / Nつおき: その節と、そこから N つおきの節 (前後どちらへも)
        /// </summary>
        private void ClickSelectSection(int index)
        {
            if (index < 0 || index >= sections.Count) { return; }

            if (ModifierKey.Shift && selectedIndex >= 0)
            {
                // (SelectMany の中で、今の節が替わるので、先に覚えておく)
                int anchorIndex = selectedIndex;
                SelectMany(index, i => (i - anchorIndex) * (i - index) <= 0);
                return;
            }

            switch (selectMode)
            {
                case SectionSelectMode.All:
                    SelectMany(index, _ => true);
                    break;
                case SectionSelectMode.Alternate:
                {
                    // (N つおき = N+1 個ごと。1つおきなら 2 個ごと)
                    int step = AlternateInterval + 1;
                    SelectMany(index, i => (i - index) % step == 0);
                    break;
                }
                default:
                    Select(index);
                    break;
            }
        }

        /// <summary>
        /// 節を、いちばん手前に選んでいる節にする (選んでいるほかの節は、そのまま)。選んでいなければ、クリックしたときと同じに選ぶ
        /// </summary>
        private void MakePrimary(int index)
        {
            if (index < 0 || index >= sections.Count || index == selectedIndex) { return; }
            if (!IsSectionSelected(sections[index].Note))
            {
                ClickSelectSection(index);
                return;
            }

            var others = new List<NoteData_SpaceHold>(extraSelected);
            if (selectedNote != null) { others.Add(selectedNote); }
            Select(index);
            foreach (var note in others)
            {
                if (!ReferenceEquals(note, selectedNote)) { extraSelected.Add(note); }
            }
        }

        /// <summary>
        /// 条件に合う節をまとめて選ぶ (primary が、いちばん手前に選んでいる節になる)
        /// </summary>
        private void SelectMany(int primary, Func<int, bool> isIncluded)
        {
            Select(primary);
            for (int i = 0; i < sections.Count; i++)
            {
                if (i != primary && isIncluded(i)) { extraSelected.Add(sections[i].Note); }
            }
        }

        /// <summary>
        /// 選択モードのボタン (と Ctrl+A)
        /// ・全体: 押したらすぐ、全部を選ぶ。もう全部を選んでいるときに押すと、選択を外して「通常」に戻す (録音 20261117_202034)
        /// ・通常・Nつおき: 次にクリックした節から。「全体」から切り替えたときは、全部の選択を外す (録音 20261117_202034)
        /// </summary>
        private void ChooseSelectMode(SectionSelectMode mode)
        {
            if (mode == SectionSelectMode.All)
            {
                if (AreAllSectionsSelected())
                {
                    selectMode = SectionSelectMode.Normal;
                    Select(-1);
                    ShowStatus("節の選択を外しました");
                }
                else { SelectAllSections(); }
                return;
            }

            if (selectMode == SectionSelectMode.All) { Select(-1); }
            selectMode = mode;
        }

        /// <summary>
        /// 選択モード「全体」: SpaceHold の節を全部選ぶ (いちばん手前は、今の節。無ければ最初の節)
        /// </summary>
        private void SelectAllSections()
        {
            if (sections.Count == 0) { return; }

            selectMode = SectionSelectMode.All;
            SelectMany(selectedIndex >= 0 ? selectedIndex : 0, _ => true);
            ShowStatus($"節を全部選びました ({sections.Count} 個)");
        }

        /// <summary>
        /// SpaceHold の節を、全部選んでいるか (節が無ければ false)
        /// </summary>
        private bool AreAllSectionsSelected()
        {
            if (sections.Count == 0) { return false; }
            foreach (var section in sections)
            {
                if (!IsSectionSelected(section.Note)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// その節を選んでいるか (Ctrl+クリックで加えた節も含む)
        /// </summary>
        private bool IsSectionSelected(NoteData_SpaceHold note)
        {
            return note != null && (ReferenceEquals(note, selectedNote) || extraSelected.Contains(note));
        }

        /// <summary>
        /// 節を、1つだけ選んでいるか (「頂点レイヤーで編集」のボタンは、このときだけ出す)
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
        /// 無くなった節を、選択から外す
        /// (道具の列は、いつも出ている: 節を選んでいなくても、道具は選んだままにする。録音 20261117_201649)
        /// </summary>
        private void PruneSelection()
        {
            if (extraSelected.Count > 0) { extraSelected.RemoveWhere(isStaleSelection); }
        }

        #endregion

        #region キー操作

        private void HandleKeys()
        {
            if (InputFieldFocus.IsTyping || EditorMenuBar.IsMenuOpenOrJustClosed) { return; }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // ◆ の位置の窓を出しているときは、まず窓を閉じる。道具 (回転・拡大縮小) を選んでいるときは、まず「編集」に戻る
                if (IsControlPanelShown) { CloseControlPanel(); }
                else if (vertexTool != PreviewVertexTool.Move) { vertexTool = PreviewVertexTool.Move; }
                else { Close(); }
                return;
            }

            // Delete: 選んでいる節を消す (3D編集では、頂点を選ばない)
            if (Input.GetKeyDown(KeyCode.Delete) && !IsDragging && !EditorContextMenuView.IsOpen)
            {
                DeleteSections(GetSelectedSectionIndices());
            }

            // Ctrl+A: 選択モード「全体」のボタンと同じ (全部を選ぶ。全部を選んでいるときは、選択を外す。録音 20261117_190401 / 202034)
            if (ModifierKey.Ctrl && Input.GetKeyDown(KeyCode.A) && !IsDragging) { ChooseSelectMode(SectionSelectMode.All); }

            // Alt+キー: 道具「回転」「拡大縮小」を選ぶ / やめる (録音 20261117_180313)
            if (ModifierKey.Alt && !IsDragging)
            {
                if (rotateKey != KeyCode.None && Input.GetKeyDown(rotateKey)) { ToggleTool(PreviewVertexTool.Rotate); }
                else if (scaleKey != KeyCode.None && Input.GetKeyDown(scaleKey)) { ToggleTool(PreviewVertexTool.Scale); }
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

        /// <summary>
        /// 道具を選ぶ。選んでいる道具をもう一度選ぶと、やめる (全画面プレビューの列と同じ)
        /// (「編集」(Move) を選ぶと、いつも道具をやめた状態になる: 録音 20261117_204809)
        /// </summary>
        private void ToggleTool(PreviewVertexTool tool)
        {
            vertexTool = vertexTool == tool ? PreviewVertexTool.Move : tool;
        }

        /// <summary>
        /// 回転・拡大縮小で動かす節と、その効き方 (1 = 全部、0 と 1 の間 = その割合だけ)。選んでいる節だけを動かす。ロックした節は動かさない
        /// ・通常: 選んでいる節を、全部同じだけ
        /// ・なめらか: 掴んだ節 (grabbed) は全部、選んでいるほかの節は、選んでいる範囲のすぐ外の節に近いほど少し (録音 20261117_190722)
        ///   (180度回すと、間の節は 170度、160度 … とねじれていく。範囲が SpaceHold の端まであるときは、端の節が 0)
        /// </summary>
        private List<(int Index, float Weight)> GetTransformTargets(int grabbed)
        {
            var selected = GetSelectedSectionIndices();
            if (!selected.Contains(grabbed)) { selected.Add(grabbed); }
            selected.RemoveAll(index => index != grabbed && IsLockedSection(index));

            var targets = new List<(int Index, float Weight)>();
            if (transformRange != SectionTransformRange.Smooth)
            {
                foreach (int index in selected) { targets.Add((index, 1f)); }
                return targets;
            }

            int first = int.MaxValue;
            int last = int.MinValue;
            foreach (int index in selected)
            {
                first = Mathf.Min(first, index);
                last = Mathf.Max(last, index);
            }
            // 動かさない端: 選んでいる範囲のすぐ外の節 (範囲が SpaceHold の端まであるときは、端の節)
            int before = first > 0 ? first - 1 : first;
            int after = last < sections.Count - 1 ? last + 1 : last;

            foreach (int index in selected)
            {
                float weight = 1f;
                if (index < grabbed && before < grabbed)
                {
                    weight = SpaceHoldSections.GetFraction(sections[before], sections[grabbed], sections[index].Time, sections[index].Depth);
                }
                else if (index > grabbed && after > grabbed)
                {
                    weight = 1f - SpaceHoldSections.GetFraction(sections[grabbed], sections[after], sections[index].Time, sections[index].Depth);
                }
                targets.Add((index, weight));
            }
            return targets;
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

        #region 画面 (視点のウィンドウ・操作説明・シーンの画面部品)

        /// <summary>
        /// 右上の視点のウィンドウ (シーンに置いてある: SpaceHold3DViewUi) のボタンと向きの印に、処理をつなぐ (最初に開いたときに1回だけ)
        /// ・向きの印 (軸の先を押すと、その向きから見る。まんなかで透視と平行の切り替え)
        /// ・透視 / 平行、全体を映す (F)、奥行きの縮尺 [-] [+]
        /// 節の形の道具 (回転、拡大縮小、頂点スライド、反転)、動かし方と選択モード、右下のボタン (頂点レイヤーで編集、編集画面へ戻る) は、
        /// シーンに置いてある別の画面部品 (SpaceHold3DOverlayView) を使う (ConnectOverlay)
        /// 操作の説明は、画面の右の「操作説明」のウィンドウに出す (3D編集の行も、シーンに置いてある。エディットモード SpaceHoldView の行)
        /// </summary>
        private void ConnectViewPanel()
        {
            var ui = view.Ui;
            if (ui == null) { return; }

            gizmo = ui.Gizmo;
            if (gizmo != null)
            {
                gizmo.OnAxisClicked = SetViewFromAxis;
                gizmo.OnCenterClicked = ToggleProjection;
            }

            projectionButton = ui.ProjectionButton;
            projectionButton?.SetOnClick(ToggleProjection);
            ui.FrameButton?.SetOnClick(FrameSelectionOrAll);
            ui.DepthDownButton?.SetOnClick(() => ChangeDepthScale(-1));
            ui.DepthUpButton?.SetOnClick(() => ChangeDepthScale(1));
            depthScaleLabel = ui.DepthScaleLabel;
        }

        #region シーンの画面部品 (道具の列、右下のボタン、◆ の位置の窓)

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

            overlay.OnSlideClicked += delta => { if (isOpen) { SlideSelection(delta); } };
            overlay.OnMirrorClicked += isLeftRight => { if (isOpen) { MirrorSelection(isLeftRight); } };
            overlay.OnToolClicked += tool =>
            {
                if (!isOpen) { return; }
                // 選んでいる道具をもう一度押すと、道具をやめる (全画面プレビューの列と同じ)。「編集」を押すと、道具をやめる
                ToggleTool(tool);
            };
            overlay.OnRangeClicked += range => { if (isOpen) { transformRange = range; } };
            overlay.OnSelectModeClicked += mode => { if (isOpen) { ChooseSelectMode(mode); } };

            // 道具のキー: 頂点レイヤーの「回転」「拡大縮小」と同じキー (ボタンの右下に出す)
            var inputHandler = UnityEngine.Object.FindObjectOfType<InputHandler>(true);
            if (inputHandler != null)
            {
                inputHandler.TryGetEditModeShortcutKey(EditMode.VerticesRotate, EditNoteType.Vertices, out rotateKey);
                inputHandler.TryGetEditModeShortcutKey(EditMode.VerticesScale, EditNoteType.Vertices, out scaleKey);
            }
            overlay.SetToolShortcut(PreviewVertexTool.Rotate, ShortcutBadge.FormatAltKey(rotateKey));
            overlay.SetToolShortcut(PreviewVertexTool.Scale, ShortcutBadge.FormatAltKey(scaleKey));
            overlay.OnVertexLayerClicked += () => { if (isOpen) { OpenSelectedInVerticesLayer(); } };
            // 節の選択を外す (道具は、選んだままにする: 録音 20261117_201649)
            overlay.OnEndVertexEditClicked += () => { if (isOpen) { Select(-1); } };
            overlay.OnCloseClicked += () => { if (isOpen) { Close(); } };
            // レイヤーのボタン: 3D編集を閉じて、スペースのレイヤーに戻る (録音 20261117_195031)
            overlay.OnLayerClicked += () =>
            {
                if (!isOpen) { return; }
                Close();
                if (dataGetter.EditNoteType.Value != EditNoteType.Space) { dataSetter.SetEditNoteType(EditNoteType.Space); }
            };
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
        /// 左上の道具の列と右下のボタンを、今の道具・選択に合わせる
        /// </summary>
        private void RefreshOverlayTools()
        {
            if (overlay == null) { return; }

            overlay.SetListMode(selectedNote != null);
            overlay.SetTool(vertexTool);
            overlay.SetRange(transformRange);
            overlay.SetSelectMode(selectMode);
            // 選択モードのボタンの「Nつおき」の文字 (設定メニューで変えたら、すぐ合わせる)
            overlay.SetAlternateInterval(AlternateInterval);
            // 頂点レイヤーで編集: 節を1つだけ選んでいる間だけ出す (録音 20261117_024055)
            overlay.SetVertexLayerButtonVisible(IsSingleSectionSelected && !IsLockedSection(selectedIndex));
        }

        /// <summary>
        /// つまみを右クリックしたとき: 節の中心 = 節のメニュー。◆ = 位置を数値で決める窓
        /// </summary>
        private void ShowHandleMenu(Handle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.Control:
                    // ◆: メニューを挟まず、位置を数値で決める窓を出す (まっすぐに戻すのは、窓のボタンで: 録音 20261117_005036)
                    if (handle.Span >= 0 && handle.Span < spans.Count) { OpenControlPanel(handle.Span); }
                    break;
                case HandleKind.Center:
                    ShowSectionMenu(handle.Section);
                    break;
            }
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
                // (最後の節のときは、前の節との間に足す: InsertSectionAfter)
                string insertLabel = index == sections.Count - 1 ? "この節の前に節を足す (補間)" : "この節の後ろに節を足す (補間)";
                items.Add(new EditorContextMenuView.Item(insertLabel, () => { if (IsLiveSection(note)) { InsertSectionAfter(chain.IndexOf(note)); } }));
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
                items.Add(new EditorContextMenuView.Item("この節を消す", () =>
                {
                    int current = chain.IndexOf(note);
                    if (current >= 0) { DeleteSections(new List<int> { current }); }
                }, shortcut: "Delete"));
            }

            // 節の中心の位置の窓も、メニューと一緒に出して、メニューと一緒に閉じる
            // (メニューの外を1回クリックすれば、両方消える。録音 20261118_025337)
            bool isShown = EditorContextMenuView.Show(Input.mousePosition, items, () =>
            {
                if (centerPanelNote == note) { CloseControlPanel(); }
            });
            if (isShown) { OpenCenterPanel(note); }
        }

        /// <summary>
        /// 節の中心の位置の窓を、右クリックのメニューの上に出す (数値で、中心の位置を細かく決められる。録音 20261118_020916)
        /// (窓は、メニューより手前のキャンバスにあるので、メニューを出したまま打てる)
        /// </summary>
        private void OpenCenterPanel(NoteData_SpaceHold note)
        {
            if (overlay == null || note == null) { return; }

            controlPanelStart = null;
            controlPanelEnd = null;
            centerPanelNote = note;
            overlay.SetControlPanelMode(true);

            // メニューのすぐ上 (上に入らないときは、すぐ下)。窓の左の端の、高さのまんなかを合わせる
            // (メニューは、画面の下の端の近くでは上へずれるので、カーソルではなく、出たメニューの四角から決める)
            float scale = view.Canvas != null && view.Canvas.scaleFactor > 0f ? view.Canvas.scaleFactor : 1f;
            Vector2 position = (Vector2)Input.mousePosition / scale + new Vector2(6f, CENTER_PANEL_RAISE);
            if (EditorContextMenuView.TryGetScreenRect(out Rect menu))
            {
                float halfHeight = SpaceHold3DOverlayView.SECTION_CENTER_PANEL_HEIGHT * 0.5f;
                float screenTop = Screen.height / scale;
                float above = menu.yMax / scale + CENTER_PANEL_GAP + halfHeight;
                float below = menu.yMin / scale - CENTER_PANEL_GAP - halfHeight;
                bool fitsAbove = above + halfHeight <= screenTop - CENTER_PANEL_TOP_MARGIN;
                position = new Vector2(menu.xMin / scale, fitsAbove ? above : below);
            }
            overlay.ShowControlPanel(position);
            RefreshControlPanel();
        }

        // 節の中心の位置の窓と、メニューの間
        const float CENTER_PANEL_GAP = 6f;
        // 窓を置けない、画面の上の端の幅 (メニューバーと、少しの間。窓を動かすときの寄せ方と同じ)
        const float CENTER_PANEL_TOP_MARGIN = 42f;

        // 節の中心の位置の窓を、カーソルからどれだけ上に出すか (窓の高さの半分と、少しの間)
        const float CENTER_PANEL_RAISE = SpaceHold3DOverlayView.SECTION_CENTER_PANEL_HEIGHT * 0.5f + 12f;

        /// <summary>
        /// 節の中心の位置の窓に、数値が入力されたとき: 節を、その位置へ動かす (選んでいるほかの節も、ドラッグと同じように動く。元に戻せる)
        /// </summary>
        private void OnCenterValueEntered(int axis, float value)
        {
            if (!isOpen || IsDragging || axis > 1) { return; }
            int index = centerPanelNote != null ? chain.IndexOf(centerPanelNote) : -1;
            if (index < 0 || index >= sections.Count || !IsLiveSection(centerPanelNote)) { return; }
            if (IsLockedSection(index))
            {
                ShowLockedStatus();
                return;
            }

            Vector2 center = sections[index].Center;
            Vector2 delta = axis == 0 ? new Vector2(value - center.x, 0f) : new Vector2(0f, value - center.y);
            if (delta.sqrMagnitude < 1e-12f) { return; }

            var previous = new List<VertexDataToPos>();
            var current = new List<VertexDataToPos>();
            foreach (var target in GetTransformTargets(index))
            {
                if (target.Weight <= 0f) { continue; }
                foreach (var vertex in sections[target.Index].Note.SpaceVertices.Vertices)
                {
                    Vector2 before = vertex.Position.Value;
                    vertex.SetPosition(before + delta * target.Weight);
                    if (vertex.Position.Value == before) { continue; }
                    previous.Add(new VertexDataToPos(vertex, before));
                    current.Add(new VertexDataToPos(vertex, vertex.Position.Value));
                }
            }
            if (current.Count == 0) { return; }

            UndoRedo.Vertices.VerticesMoveRecord.RecordVertcesMoving(previous, current);
            ShowStatus("節の中心の位置を、数値で決めました");
        }

        /// <summary>
        /// その節が、まだ開いている SpaceHold の中にあるか (メニューや窓が、消えた節に働かないように)
        /// </summary>
        private bool IsLiveSection(NoteData_SpaceHold note)
        {
            return note != null && chainSet.Contains(note) && notesGetter.GetNoteObject(note) != null;
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
        /// その節の形を、ほかの節すべてに反映する (それぞれの節の位置 (節の中心 = 形の重心) は、今のまま。ロックした節は変えない。元に戻すのは1回ぶん)
        /// (録音 20261117_023744: ハートの形の節から反映すると、ホールド全体がハートの形のトンネルになる)
        /// </summary>
        private void ApplyShapeToAll(NoteData_SpaceHold source)
        {
            Vector2[] sourcePoints = SpaceHoldSections.GetVertices(source.SpaceVertices);
            if (sourcePoints.Length < PreviewVertexTools.MIN_VERTEX_COUNT) { return; }

            Vector2 sourceCenter = SpaceHoldSections.GetCenter(sourcePoints);
            var shape = new List<Vector2>();
            foreach (var point in sourcePoints) { shape.Add(point - sourceCenter); }

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
                Vector2 center = SpaceHoldSections.GetCenter(SpaceHoldSections.GetVertices(vertices));
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
        /// 2つの節の間を、まっすぐに戻す (元に戻すのは1回ぶん)
        /// 間のメッシュ中継点は残して、それぞれの形のまま、中心が2つの節の中心を結ぶ直線の上に来るように動かす
        /// (回した・大きさを変えた形も、そのまま残す: 録音 20261117_194644。ノーツの面からはみ出す中継点は、形ごと縮めて収める)
        /// </summary>
        private void StraightenSpan(NoteData_SpaceHold start, NoteData_SpaceHold end)
        {
            for (int i = 0; i < spans.Count; i++)
            {
                if (!ReferenceEquals(sections[spans[i].StartIndex].Note, start) || !ReferenceEquals(sections[spans[i].EndIndex].Note, end)) { continue; }

                Span span = spans[i];
                SpaceHoldSection a = sections[span.StartIndex];
                SpaceHoldSection b = sections[span.EndIndex];
                Vector2 centerA = a.Center;
                Vector2 centerB = b.Center;

                var moved = new List<VertexData>();
                var before = new List<Vector2>();
                var after = new List<Vector2>();
                for (int r = span.StartIndex + 1; r < span.EndIndex; r++)
                {
                    SpaceHoldSection relay = sections[r];
                    var vertices = relay.Note.SpaceVertices.Vertices;
                    Vector2[] points = SpaceHoldSections.GetVertices(relay.Note.SpaceVertices);
                    Vector2 center = SpaceHoldSections.GetCenter(points);
                    Vector2 target = Vector2.Lerp(centerA, centerB, SpaceHoldSections.GetFraction(a, b, relay.Time, relay.Depth));
                    if ((target - center).sqrMagnitude <= 1e-12f) { continue; }

                    var shape = new List<Vector2>(points.Length);
                    foreach (var point in points) { shape.Add(point - center); }
                    float scale = GetFitScale(target, shape);
                    // (中心がノーツの面の端にあって収まらないときは、縮めずに置く。はみ出した頂点は、面の端に寄る)
                    if (scale <= 0f) { scale = 1f; }

                    for (int v = 0; v < vertices.Count; v++)
                    {
                        moved.Add(vertices[v]);
                        before.Add(points[v]);
                        after.Add(target + shape[v] * scale);
                    }
                }

                NoteData_SpaceHold startNote = a.Note;
                NoteData_SpaceHold endNote = b.Note;
                Vector3 straight = SpaceHoldCurveMath.GetStraightControl(a, b);
                if (moved.Count == 0)
                {
                    StoreControl(startNote, endNote, straight);
                }
                else
                {
                    Vector3? storedControl = GetStoredControl(startNote, endNote);
                    Record(() =>
                    {
                        for (int k = 0; k < moved.Count; k++) { moved[k].SetPosition(after[k]); }
                        StoreControl(startNote, endNote, straight);
                    },
                    () =>
                    {
                        for (int k = 0; k < moved.Count; k++) { moved[k].SetPosition(before[k]); }
                        RestoreControl(startNote, endNote, storedControl);
                    });
                }

                ShowStatus("2つの節の間を、まっすぐに戻しました");
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

        /// <summary>
        /// スペースのレイヤーで、頂点を持つノーツ (SpaceHold・SpaceBreak) を選んでいれば、頂点レイヤーで開ける
        /// (ミニプレビューの上の段の左の「頂点レイヤーで編集」。録音 20261117_195031)
        /// </summary>
        public bool CanOpenSpaceSelectionInVerticesLayer()
        {
            if (isOpen) { return false; }
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Space) { return false; }

            // (毎フレーム呼ばれるので、ノーツのオブジェクトは探さない。1つ見つかれば十分)
            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (selecting is IVerticesControlableNoteData && !NoteLock.IsLocked(selecting)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 選んでいる、頂点を持つノーツのうち、いちばん前のもの (つながった SpaceHold を全体で選んだときは、最初の部分) を、頂点レイヤーで開く
        /// </summary>
        public void OpenSpaceSelectionInVerticesLayer()
        {
            if (!CanOpenSpaceSelectionInVerticesLayer()) { return; }

            var target = FindFirstSelectedShape();
            if (target == null) { return; }

            dataSetter.SetEditNoteType(EditNoteType.Vertices);
            notesSetter.SetEditingVertices(target);
        }

        /// <summary>
        /// 選んでいる、頂点を持つノーツ (ロックしていないもの) のうち、いちばん前のもの。無ければ null
        /// </summary>
        private IVerticesControlableNoteData FindFirstSelectedShape()
        {
            IVerticesControlableNoteData first = null;
            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (selecting is not IVerticesControlableNoteData shape || NoteLock.IsLocked(selecting)) { continue; }
                if (notesGetter.GetNoteObject(selecting) == null) { continue; }

                if (first == null || IsEarlier(selecting, (IDeployableNoteData)first)) { first = shape; }
            }
            return first;
        }

        private static bool IsEarlier(IDeployableNoteData a, IDeployableNoteData b)
        {
            if (a.Address.BarIndex != b.Address.BarIndex) { return a.Address.BarIndex < b.Address.BarIndex; }
            return a.Address.SubDivisionIndex < b.Address.SubDivisionIndex;
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
            centerPanelNote = null;
            overlay.SetControlPanelMode(false);

            overlay.ShowControlPanel(GetControlPanelPosition(span));
            RefreshControlPanel();
            ShowStatus("◆ の位置を、数値で決められます (Z: 0 = 前の節、1 = 次の節)");
        }

        private void CloseControlPanel()
        {
            if (overlay != null) { overlay.HideControlPanel(); }
            controlPanelStart = null;
            controlPanelEnd = null;
            centerPanelNote = null;
        }

        /// <summary>
        /// ◆ の位置の窓の、表示と場所を、今の ◆ に合わせる (区間が無くなったら、窓を閉じる)
        /// </summary>
        private void RefreshControlPanel()
        {
            if (!IsControlPanelShown) { return; }

            // 節の中心の位置: 今の中心を出す (節が無くなったら、窓を閉じる)
            if (centerPanelNote != null)
            {
                int centerIndex = chain.IndexOf(centerPanelNote);
                if (centerIndex < 0 || centerIndex >= sections.Count || !IsLiveSection(centerPanelNote))
                {
                    CloseControlPanel();
                    return;
                }
                Vector2 center = sections[centerIndex].Center;
                overlay.SetControlValues(center.x, center.y, 0f);
                return;
            }

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
            if (centerPanelNote != null)
            {
                OnCenterValueEntered(axis, value);
                return;
            }
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

            int count = CommitCurve(span.StartIndex, span.EndIndex, control, span.Control);
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

        /// <summary>
        /// 閉じて、選んでいる節を頂点レイヤーで編集する (ロックした節は開かない)
        /// </summary>
        private void OpenSelectedInVerticesLayer()
        {
            OpenSectionInVerticesLayer(selectedNote ?? anchor);
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
        /// 今の帯の形 (回した・大きさを変えた中継点) は残して、曲線が動いた分だけ動かす (録音 20261117_194644)
        /// 制御点が動いていないときは、制御点を覚えるだけ (元に戻す操作も、中継点の置き直しもしない)
        /// </summary>
        /// <param name="previousControl">区間の今の制御点 (動かす前)</param>
        /// <returns>置いた中継点の数 (置き直さなかったときは -1)</returns>
        private int CommitCurve(int startIndex, int endIndex, Vector3 control, Vector3 previousControl)
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || timeline == null) { return -1; }
            if (startIndex < 0 || endIndex >= sections.Count || startIndex >= endIndex) { return -1; }

            SpaceHoldSection a = sections[startIndex];
            SpaceHoldSection b = sections[endIndex];
            int chainIndex = a.Note.ChainIndex.Value;
            if (chainIndex < 0) { return -1; }

            control = SpaceHoldCurveMath.ClampControl(a, b, control);
            if ((control - SpaceHoldCurveMath.ClampControl(a, b, previousControl)).sqrMagnitude <= 1e-10f)
            {
                StoreControl(a.Note, b.Note, control);
                return 0;
            }

            var baked = new List<SpaceHoldCurveMath.BakedSection>();
            SpaceHoldCurveMath.BakeKeepingShapes(timeline, sections.GetRange(startIndex, endIndex - startIndex + 1), previousControl, control,
                                                 SpaceHoldSections.GetNormalizedVertexCount(sections), baked);

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
            Vector3? storedControl = GetStoredControl(startNote, endNote);

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

                RestoreControl(startNote, endNote, storedControl);
            });

            // 選んでいた節が外れたら、区間の始まりの節を選ぶ
            // (ロックした節は、選んでも頂点レイヤーで編集する節にはしない: Select と同じ)
            if (selectedNote != null && removed.Contains(selectedNote))
            {
                selectedNote = startNote;
                if (!NoteLock.IsLocked(startNote))
                {
                    notesSetter.SetEditingVertices(startNote);
                    lastEditing = startNote;
                }
            }

            return added.Count;
        }

        private void StoreControl(NoteData_SpaceHold start, NoteData_SpaceHold end, Vector3 control)
        {
            controls[start] = new StoredControl { End = end, Control = control };
        }

        /// <summary>
        /// その区間について覚えている制御点 (覚えていなければ null)
        /// </summary>
        private Vector3? GetStoredControl(NoteData_SpaceHold start, NoteData_SpaceHold end)
        {
            return controls.TryGetValue(start, out StoredControl stored) && ReferenceEquals(stored.End, end) ? stored.Control : (Vector3?)null;
        }

        /// <summary>
        /// 元に戻すとき: 覚えていた制御点に戻す (覚えていなかったときは、忘れる)
        /// </summary>
        private void RestoreControl(NoteData_SpaceHold start, NoteData_SpaceHold end, Vector3? control)
        {
            if (control.HasValue) { StoreControl(start, end, control.Value); }
            else { controls.Remove(start); }
        }

        #endregion
    }
}
