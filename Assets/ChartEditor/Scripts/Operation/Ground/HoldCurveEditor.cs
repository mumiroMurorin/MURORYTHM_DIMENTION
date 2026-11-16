using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using VContainer.Unity;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// ホールドを、制御点で曲げる
    ///
    /// グラウンドのレイヤーで、ホールドの節 (始点・中継点・終点) を1つだけ選ぶと、
    /// その節と隣の節との間に、制御点の印 (ひし形) が出る。次の節との間のものと、前の節との間のものの、最大2つ。
    /// 節の間にあるメッシュ中継点を選んだときは、その中継点がある区間 (前の節と次の節の間) の制御点が1つ出る。
    /// 印を掴んで動かすと、2つの節の間が、制御点の方へ引っ張られた曲線 (ベジェ曲線) になるように、
    /// 間にメッシュ中継点を置き直す。
    ///
    /// 決まりごと:
    /// ・制御点は、隣り合った2つの節の間に1つ (2つの節が共有する)。
    ///   「隣の節」は、メッシュ中継点を飛ばした、次の節 (始点・中継点・終点)
    /// ・メッシュ中継点は、すべての分線に置くのではなく、曲線に近い折れ線を作るのに必要な分線にだけ置く (HoldCurveMath.SelectRelays)
    /// ・制御点を動かすと、その2つの節の間にあるメッシュ中継点は、手で置いたものも含めて、すべて置き直される
    /// ・制御点の位置は、2つの節のうち時間が早い方の節が、編集している間だけ覚えている (譜面ファイルには保存しない)。
    ///   譜面を開き直すと、制御点は動かしていない位置 (2つの節の真ん中) に戻る
    ///   (置いたメッシュ中継点は、ふつうのノーツとして残っている)
    /// ・制御点は、レーンの外へも動かせる (曲線は、制御点までの半分ほどしか寄らないので、大きく曲げるには、外へ出す必要がある)。
    ///   時間方向は、2つの節の間から出せない (出すと、曲線が時間をさかのぼる)
    /// ・印を離したときに、動かす前からの変更を、1回ぶんとして履歴に記録する
    /// ・制御点で曲げた区間のメッシュ中継点は、両端の節を動かしたり、幅を変えたりすると、曲線に合わせて置き直される
    ///   (元に戻す・やり直すで節が動いたときも同じ)。
    ///   手で動かした中継点がある区間は、置き直さない (手で直した形を、勝手に上書きしないため)
    ///
    /// ・印を右クリック・ダブルクリック: 位置を数値で決める窓 (HoldCurveControlPanelView。横 = まっすぐな線からのずれ (レーンの数)、
    ///   時間 = 0 が前の節、1 が次の節。窓の「まっすぐに戻す」で、まっすぐに戻す。録音 20261117_005036: メニューは挟まない)
    /// ・ロックしたメッシュ中継点は、節として扱う (その中継点と隣の節の間を、曲げられる。置き直しで消されることもない)
    ///
    /// 印の見た目は、シーンに置いてある HoldCurveHandlesView。曲線の計算は HoldCurveMath。
    /// </summary>
    public class HoldCurveEditor : ITickable, IPostLateTickable, IDisposable
    {
        // 制御点を掴めないモード (ほかの操作の途中や、クリックに別の意味があるツールを使っている間)
        static readonly EditMode[] ignoreEditModes =
        {
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.Connect,
            EditMode.Connecting,
            EditMode.DisConnect,
            EditMode.ChangeType,
            EditMode.SpaceMoving,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
            EditMode.Preview,
        };

        // 制御点を、2つの節をまっすぐ結んだ線から、横へ離せる限度 (レーンの数)
        const float MAX_SIDE_OFFSET = HoldCurveMath.LANE_COUNT * 2f;
        // 印を、画面の左右の端から、これより内側に収める (ピクセル数。画面の外へ出ると、掴めなくなる)
        const float SCREEN_MARGIN = 24f;
        // 2つの節をまっすぐ結んだ線に、これより近いとき (レーンの数) は、まっすぐにする (まっすぐへ戻しやすいように)
        const float STRAIGHT_SNAP = 0.2f;

        static HoldCurveEditor instance;

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;

        HoldCurveHandlesView view;
        bool hasSearchedScene;

        // 位置を数値で決める窓 (シーンに置いてある) と、窓で見せている区間
        HoldCurveControlPanelView controlPanel;
        NoteData_Hold panelStart;
        NoteData_Hold panelEnd;
        // 窓を閉じるのに Esc を使ったフレーム (同じ Esc で、ホールドの単独編集まで終わらないように)
        int escapeUsedFrame = -1;
        // 窓の場所 (印が描けないときは、ここに置いたままにする)
        Vector2 lastPanelPosition;
        // 窓の数値を反映している最中か (そのときは、選択を変えない)
        bool isApplyingFromPanel;
        // 印のダブルクリック
        const float DOUBLE_CLICK_SECONDS = 0.35f;
        const float DOUBLE_CLICK_PIXELS = 12f;
        float lastMarkerClickTime = -10f;
        Vector2 lastMarkerClickPosition;
        NoteData_Hold lastMarkerClickStart;
        NoteData_Hold lastMarkerClickEnd;

        /// <summary>
        /// 表示している制御点1つぶん
        /// </summary>
        private struct Marker
        {
            public bool IsShown;
            // 制御点をはさむ2つの節 (Start が、時間が早い方)
            public NoteData_Hold Start;
            public NoteData_Hold End;
            // 画面上の位置
            public Vector2 StartScreen;
            public Vector2 EndScreen;
            public Vector2 MarkerScreen;
        }

        // 選んでいる節と、その前後の制御点 (次の節との間のもの、前の節との間のもの)
        NoteData_Hold target;
        Marker markerToNext;
        Marker markerToBack;

        // カーソルが印の上にあるか (フレームごとに1回だけ調べる)
        int hoverCheckedFrame = -1;
        bool isPointerOnMarker;
        bool isHoveredToNext;

        // 掴んでいる間の状態
        bool isDragging;
        bool isDraggingToNext;
        // 掴んでいる制御点をはさむ2つの節 (dragStart が、時間が早い方。制御点の位置は dragStart が覚えている)
        NoteData_Hold dragStart;
        NoteData_Hold dragEnd;
        CurvedSpan dragSpan;
        EditMode modeBeforeDrag;
        Vector2? controlBeforeDrag;
        Vector2 pressScreenPosition;
        // 掴んだときの、カーソルから印の中心までのずれ (掴んだ瞬間に、印がカーソルの位置へ飛ばないように)
        Vector2 grabOffset;
        // 掴んでから、実際に置き直したか (動かさずに離したときは、何も変えない)
        bool hasApplied;
        // 掴む前に、2つの節の間にあったメッシュ中継点 (元に戻すときに、そのままの場所へ戻す)
        readonly List<OriginalRelay> originalRelays = new List<OriginalRelay>();

        /// <summary>
        /// 制御点を動かす前にあったメッシュ中継点と、そのときの場所
        /// </summary>
        private struct OriginalRelay
        {
            public NoteData_Hold Note;
            public AddressWithinRange Address;
        }

        /// <summary>
        /// カーソルが制御点の印の上にある、または制御点を掴んでいる
        /// (この間は、譜面のほかの操作 (選択、範囲選択、配置、移動など) にクリックを渡さない)
        /// </summary>
        /// <summary>
        /// 位置を数値で決める窓を出しているか、このフレームに Esc で閉じたか (その Esc を、ほかの処理で使わないように)
        /// </summary>
        public static bool IsUsingEscape => instance != null &&
            ((instance.controlPanel != null && instance.controlPanel.IsShown) || instance.escapeUsedFrame == Time.frameCount);

        public static bool IsCapturingPointer => instance != null && (instance.isDragging || (instance.IsPointerOnMarker() && instance.CanStartDrag()));

        /// <summary>
        /// 制御点を掴んで動かしている最中か
        /// </summary>
        public static bool IsDragging => instance != null && instance.isDragging;

        public HoldCurveEditor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;

            instance = this;

            // 節が選ばれたら、その節の曲げた区間を覚える (選んで、そのまま動かしたときにも、中継点が置き直されるように)
            selectionSubscription = notesGetter.SelectingNotes.ObserveAdd().Subscribe(added =>
            {
                if (added.Value is NoteData_Hold hold) { OnHoldSelected(hold); }
            });
        }

        void IDisposable.Dispose()
        {
            selectionSubscription?.Dispose();
            selectionSubscription = null;

            if (instance == this) { instance = null; }
        }

        void ITickable.Tick()
        {
            SearchSceneOnce();
            if (view == null) { return; }

            if (isDragging)
            {
                try
                {
                    // ボタンを離した、ほかの処理にモードを切り替えられた (譜面の読み込みが始まったときなど)、節が無くなった
                    if (!Input.GetMouseButton(0) || dataGetter.CurrentEditMode.Value != EditMode.CurveHandling || !IsDragSpanInChart())
                    {
                        EndDrag();
                    }
                    else
                    {
                        UpdateDrag();
                    }
                }
                catch (Exception exception)
                {
                    // 途中で失敗しても、掴んだままの状態で止まらないようにする
                    // (止まると、選択も、削除も、元に戻す操作も、受け付けなくなる)
                    Debug.LogException(exception);
                    AbortDrag();
                }
                return;
            }

            // 曲げた区間の節が動いていたら、間のメッシュ中継点を曲線に合わせて置き直す
            try
            {
                FollowCurvedSpans();
            }
            catch (Exception exception)
            {
                // 置き直しに失敗しても、ほかの操作は続けられるようにする (覚えていた区間は捨てる)
                Debug.LogException(exception);
                curvedSpans.Clear();
                newlySelectedNodes.Clear();
            }

            HandleControlPanelInput();

            // 印の右クリック: ダブルクリックと同じ、位置を数値で決める窓
            if (Input.GetMouseButtonDown(1) && IsPointerOnMarker() && CanStartDrag())
            {
                Marker pressed = isHoveredToNext ? markerToNext : markerToBack;
                if (pressed.IsShown) { OpenControlPanel(pressed.Start, pressed.End); }
                return;
            }

            if (Input.GetMouseButtonDown(0) && IsPointerOnMarker() && CanStartDrag())
            {
                // 印のダブルクリック: 位置を数値で決める窓を出す
                Marker clicked = isHoveredToNext ? markerToNext : markerToBack;
                bool isDoubleClick = clicked.Start != null &&
                                     ReferenceEquals(clicked.Start, lastMarkerClickStart) && ReferenceEquals(clicked.End, lastMarkerClickEnd) &&
                                     Time.unscaledTime - lastMarkerClickTime <= DOUBLE_CLICK_SECONDS &&
                                     ((Vector2)Input.mousePosition - lastMarkerClickPosition).magnitude <= DOUBLE_CLICK_PIXELS;
                if (isDoubleClick)
                {
                    lastMarkerClickStart = null;
                    OpenControlPanel(clicked.Start, clicked.End);
                    return;
                }

                lastMarkerClickStart = clicked.Start;
                lastMarkerClickEnd = clicked.End;
                lastMarkerClickTime = Time.unscaledTime;
                lastMarkerClickPosition = Input.mousePosition;
                BeginDrag(isHoveredToNext);
            }
        }

        #region 位置を数値で決める窓

        /// <summary>
        /// 窓を出している間: 窓の外が押されたら閉じる (打っている途中の値は、反映してから)。Esc でも閉じる
        /// </summary>
        private void HandleControlPanelInput()
        {
            if (controlPanel == null || !controlPanel.IsShown) { return; }

            bool isPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            if (isPressed && !controlPanel.IsPointerOver(Input.mousePosition))
            {
                controlPanel.CommitTyping();
                CloseControlPanel();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && !InputFieldFocus.IsTyping)
            {
                CloseControlPanel();
                escapeUsedFrame = Time.frameCount;
            }
        }

        private void OpenControlPanel(NoteData_Hold start, NoteData_Hold end)
        {
            if (start == null || end == null) { return; }

            if (controlPanel == null)
            {
                controlPanel = UnityEngine.Object.FindObjectOfType<HoldCurveControlPanelView>(true);
                if (controlPanel == null)
                {
                    Debug.LogWarning("【ホールドの曲線】位置を数値で決める窓 (HoldCurveControlPanelView) がシーンにありません");
                    return;
                }
                controlPanel.OnValueEntered += OnControlValueEntered;
                controlPanel.OnCloseClicked += CloseControlPanel;
                controlPanel.OnStraightClicked += () =>
                {
                    if (panelStart == null || panelEnd == null) { return; }
                    ApplyControl(panelStart, panelEnd, new Vector2(0f, 0.5f));
                    RefreshControlPanel();
                };
            }

            panelStart = start;
            panelEnd = end;
            lastPanelPosition = (Vector2)Input.mousePosition + new Vector2(24f, 0f);
            controlPanel.Show(GetPanelPosition());
            RefreshControlPanel();
        }

        private void CloseControlPanel()
        {
            if (controlPanel != null) { controlPanel.Hide(); }
            panelStart = null;
            panelEnd = null;
        }

        /// <summary>
        /// 窓の表示と場所を、今の制御点に合わせる (区間が無くなったら閉じる)
        /// </summary>
        private void RefreshControlPanel()
        {
            if (controlPanel == null || !controlPanel.IsShown) { return; }

            if (panelStart == null || panelEnd == null || !IsInChain(panelStart) || !ReferenceEquals(FindNeighbor(panelStart, true), panelEnd) ||
                dataGetter.EditNoteType.Value != EditNoteType.Ground)
            {
                CloseControlPanel();
                return;
            }

            Vector2 control = GetSpanControl(panelStart, panelEnd) ?? new Vector2(0f, 0.5f);
            controlPanel.SetValues(control.x, control.y);
            controlPanel.Move(GetPanelPosition());
        }

        /// <summary>
        /// 窓を出す場所 (印の右。画面のピクセル)
        /// </summary>
        private Vector2 GetPanelPosition()
        {
            Camera camera = EditorViewCameras.GroundView;
            if (camera == null) { return lastPanelPosition; }

            // (印が描けないとき (画面の外など) は、前の場所のまま。カーソルの位置にすると、窓がカーソルを追いかけてしまう)
            Marker marker = CreateMarker(panelStart, panelEnd, camera);
            if (marker.IsShown) { lastPanelPosition = marker.MarkerScreen + new Vector2(24f, 0f); }
            return lastPanelPosition;
        }

        /// <summary>
        /// 窓に数値が入力されたとき: その位置の曲線になるように、間のメッシュ中継点を置き直す (元に戻せる)
        /// </summary>
        private void OnControlValueEntered(int axis, float value)
        {
            if (panelStart == null || panelEnd == null) { return; }

            Vector2 control = GetSpanControl(panelStart, panelEnd) ?? new Vector2(0f, 0.5f);
            if (axis == 0) { control.x = Mathf.Clamp(value, -MAX_SIDE_OFFSET, MAX_SIDE_OFFSET); }
            else { control.y = Mathf.Clamp01(value); }

            ApplyControl(panelStart, panelEnd, control);
        }

        /// <summary>
        /// 2つの節の間の制御点を、その位置にする (印を掴んで、そこまで動かして離したのと同じ。元に戻すのは1回ぶん)
        /// </summary>
        private void ApplyControl(NoteData_Hold start, NoteData_Hold end, Vector2 control)
        {
            if (isDragging || start == null || end == null) { return; }
            if (!ReferenceEquals(FindNeighbor(start, true), end)) { return; }
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return; }
            // (ほかのレイヤーへ切り替えたときに、打っていた値が反映されないように)
            if (dataGetter.EditNoteType.Value != EditNoteType.Ground) { return; }

            isDragging = true;
            isDraggingToNext = true;
            dragStart = start;
            dragEnd = end;
            dragSpan = null;
            modeBeforeDrag = dataGetter.CurrentEditMode.Value;
            // 覚えていない区間に残っている制御点の位置は、前の区間のためのもの: まっすぐから始める
            // (元に戻すときのために、消す前の値を覚えておく)
            controlBeforeDrag = dragStart.CurveControl;
            if (dragStart.CurveControl.HasValue && FindSpan(dragStart, dragEnd) == null) { dragStart.SetCurveControl(null); }
            hasApplied = false;
            originalRelays.Clear();

            try
            {
                dragStart.SetCurveControl(control);
                ApplyCurve();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                AbortDrag();
                return;
            }

            isApplyingFromPanel = true;
            try { EndDrag(); }
            finally { isApplyingFromPanel = false; }
        }

        #endregion

        void IPostLateTickable.PostLateTick()
        {
            if (view == null) { return; }

            // 印の位置を、そのフレームの動き (スクロール、拡大、ノーツの移動) がすべて終わった後の位置に合わせる
            RefreshMarkers();
            RefreshControlPanel();

            bool isOnMarker = isDragging || IsPointerOnMarker();
            ShowMarker(true, markerToNext, isOnMarker && (isDragging ? isDraggingToNext : isHoveredToNext));
            ShowMarker(false, markerToBack, isOnMarker && !(isDragging ? isDraggingToNext : isHoveredToNext));
        }

        private void ShowMarker(bool isToNext, Marker marker, bool isHighlighted)
        {
            if (marker.IsShown) { view.Show(isToNext, marker.StartScreen, marker.EndScreen, marker.MarkerScreen, isHighlighted); }
            else { view.Hide(isToNext); }
        }

        private void SearchSceneOnce()
        {
            if (hasSearchedScene) { return; }
            hasSearchedScene = true;

            view = UnityEngine.Object.FindObjectOfType<HoldCurveHandlesView>(true);
            if (view == null) { Debug.LogWarning("【HoldCurve】制御点の表示(HoldCurveHandlesView)がシーンに無いため、ホールドを曲げる操作は使えません"); }
        }

        #region 制御点を出す節と、印の位置

        /// <summary>
        /// 制御点を出す節を決めて、印の位置を求める
        /// </summary>
        private void RefreshMarkers()
        {
            markerToNext.IsShown = false;
            markerToBack.IsShown = false;

            if (!isDragging) { target = FindTarget(); }

            Camera camera = EditorViewCameras.GroundView;
            if (camera == null) { return; }

            if (isDragging)
            {
                // 掴んでいる間は、掴んだ制御点だけを出す
                Marker dragged = CreateMarker(dragStart, dragEnd, camera);
                if (isDraggingToNext) { markerToNext = dragged; }
                else { markerToBack = dragged; }
                return;
            }

            if (target == null) { return; }

            // メッシュ中継点を選んでいるとき: その中継点がある区間 (前の節と次の節の間) の制御点を出す
            if (target.NoteType == DeploymentNoteType.HoldMeshRelay && !NoteLock.IsLocked(target))
            {
                markerToNext = CreateMarker(FindNeighbor(target, false), FindNeighbor(target, true), camera);
                return;
            }

            markerToNext = CreateMarker(target, FindNeighbor(target, true), camera);
            markerToBack = CreateMarker(FindNeighbor(target, false), target, camera);
        }

        /// <summary>
        /// 制御点を出すノーツ: グラウンドのレイヤーで、1つだけ選ばれている、つながったホールドのノーツ (メッシュ中継点も含む)
        /// </summary>
        private NoteData_Hold FindTarget()
        {
            if (dataGetter.IsLoadingChart.Value) { return null; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Ground) { return null; }
            if (dataGetter.ChartData.Value == null) { return null; }
            // 制御点を掴めないモード (接続・種類変更のツール、ノーツを動かしている最中など) の間は、印を出さない
            // (出したままにすると、印の下にあるノーツを、そのツールでクリックできなくなる)
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return null; }
            // 画面上のメニューを開いている間は、印を出さない (印は、メニューよりも手前に描かれる)
            if (EditorMenuBar.IsBlockingChartInput) { return null; }

            var selecting = notesGetter.SelectingNotes;
            if (selecting == null || selecting.Count != 1) { return null; }

            var hold = selecting[0] as NoteData_Hold;
            if (hold == null || hold.ChainIndex.Value < 0 || hold.Address == null) { return null; }

            return hold;
        }

        /// <summary>
        /// つながりの中で、隣の節を探す (メッシュ中継点は飛ばす)
        /// </summary>
        /// <param name="isToNext">次の方へ探すか (false のときは、前の方へ探す)</param>
        private NoteData_Hold FindNeighbor(NoteData_Hold note, bool isToNext)
        {
            var list = GetChain(note);
            if (list == null) { return null; }

            int index = IndexOf(list, note);
            if (index < 0) { return null; }

            int step = isToNext ? 1 : -1;
            for (int i = index + step; i >= 0 && i < list.Count; i += step)
            {
                var neighbor = list[i] as NoteData_Hold;
                if (neighbor == null) { return null; }
                // ロックしたメッシュ中継点も、節として扱う (その中継点と隣の節の間を、曲げられるように。録音 20261116_233430)
                if (neighbor.NoteType != DeploymentNoteType.HoldMeshRelay || NoteLock.IsLocked(neighbor)) { return neighbor; }
            }

            return null;
        }

        /// <summary>
        /// ノーツが属している、ホールドのつながり (時間の順に並んでいる)
        /// </summary>
        private IReadOnlyReactiveCollection<IChainNoteData> GetChain(NoteData_Hold note)
        {
            return notesGetter.GetChainNoteList(note.ChainIndex.Value)?.ChainNoteList;
        }

        private static int IndexOf(IReadOnlyReactiveCollection<IChainNoteData> list, IChainNoteData note)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], note)) { return i; }
            }
            return -1;
        }

        /// <summary>
        /// 2つの節の間の制御点1つぶんの、印の位置を求める
        /// </summary>
        /// <param name="start">時間が早い方の節 (制御点の位置を覚えている節)</param>
        /// <param name="end">時間が遅い方の節</param>
        private Marker CreateMarker(NoteData_Hold start, NoteData_Hold end, Camera camera)
        {
            var marker = new Marker();
            if (start == null || end == null) { return marker; }
            // 間に分線が1本も無い節どうし (同じ分線、隣り合った分線) には、中継点を置く場所が無い
            if (!HasLineBetween(start, end)) { return marker; }

            if (!TryGetNodeWorld(start, out Vector3 startWorld, out float laneWidth)) { return marker; }
            if (!TryGetNodeWorld(end, out Vector3 endWorld, out _)) { return marker; }

            // 制御点 = 2つの節の中心をまっすぐ結んだ線の上の点から、横へずらした点
            Vector2? control = GetSpanControl(start, end);
            float time = HoldCurveMath.GetControlTime(control);
            float side = control.HasValue ? control.Value.x : 0f;
            Vector3 markerWorld = Vector3.Lerp(startWorld, endWorld, time) + new Vector3(side * laneWidth, 0f, 0f);

            Vector3 markerScreen = camera.WorldToScreenPoint(markerWorld);
            // カメラの後ろにあるものは出さない
            if (markerScreen.z < 0f) { return marker; }

            // 節を動かした後などに、印が画面の左右の外へ出てしまっても、掴み直せるように、画面の端で止めて出す
            markerScreen.x = ClampToScreenWidth(markerScreen.x);

            Vector3 startScreen = camera.WorldToScreenPoint(startWorld);
            Vector3 endScreen = camera.WorldToScreenPoint(endWorld);

            marker.IsShown = true;
            marker.Start = start;
            marker.End = end;
            marker.MarkerScreen = markerScreen;
            // カメラの後ろにある節へは、線を引かない (長さ 0 の線になる)
            marker.StartScreen = startScreen.z < 0f ? markerScreen : startScreen;
            marker.EndScreen = endScreen.z < 0f ? markerScreen : endScreen;
            return marker;
        }

        private static float ClampToScreenWidth(float x)
        {
            if (Screen.width <= SCREEN_MARGIN * 2f) { return x; }
            return Mathf.Clamp(x, SCREEN_MARGIN, Screen.width - SCREEN_MARGIN);
        }

        /// <summary>
        /// 2つの節の間に、分線が1本以上あるか (両端の節がある分線は数えない)
        /// </summary>
        private bool HasLineBetween(NoteData_Hold a, NoteData_Hold b)
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null) { return false; }

            // 時間が早い方を first にする
            var first = a.Address;
            var last = b.Address;
            if (last.BarIndex < first.BarIndex || (last.BarIndex == first.BarIndex && last.SubDivisionIndex < first.SubDivisionIndex))
            {
                first = b.Address;
                last = a.Address;
            }

            if (first.BarIndex == last.BarIndex) { return last.SubDivisionIndex - first.SubDivisionIndex > 1; }
            if (first.BarIndex < 0 || first.BarIndex >= chartData.BarDatas.Count) { return false; }

            // 小節をまたぐとき: 早い方の節の後ろか、遅い方の節の手前か、間の小節に、分線がある
            return first.SubDivisionIndex < chartData.BarDatas[first.BarIndex].SubDivisionDatas.Count - 1 ||
                   last.SubDivisionIndex > 0 ||
                   last.BarIndex - first.BarIndex > 1;
        }

        /// <summary>
        /// 節の中心の、譜面の画面(3D)の中の位置と、レーン1本ぶんの横幅を求める
        /// </summary>
        private bool TryGetNodeWorld(NoteData_Hold note, out Vector3 center, out float laneWidth)
        {
            center = Vector3.zero;
            laneWidth = 0f;

            var chartData = dataGetter.ChartData.Value;
            var lanes = note.Address.RangeRP;
            if (chartData == null || lanes.Count == 0) { return false; }

            int bar = note.Address.BarIndex;
            int sub = note.Address.SubDivisionIndex;
            Transform first = GetLocation(chartData, bar, sub, lanes[0]);
            Transform last = GetLocation(chartData, bar, sub, lanes[lanes.Count - 1]);
            Transform lane0 = GetLocation(chartData, bar, sub, 0f);
            Transform lane1 = GetLocation(chartData, bar, sub, 1f);
            if (first == null || last == null || lane0 == null || lane1 == null) { return false; }

            center = (first.position + last.position) * 0.5f;
            laneWidth = lane1.position.x - lane0.position.x;
            return !Mathf.Approximately(laneWidth, 0f);
        }

        /// <summary>
        /// 譜面の画面(3D)の中で、分線とレーンが交わる場所 (ノーツの置き場所)
        /// </summary>
        private Transform GetLocation(ChartData chartData, int bar, int sub, float lane)
        {
            // 調べるたびに確保しないよう、アドレスは使い回す
            locationAddress.SetBarIndex(bar);
            locationAddress.SetSubDivisionIndex(sub);
            locationAddress.SetSliderIndex(lane);
            return chartData.GetPlacementLocation(locationAddress);
        }

        /// <summary>
        /// 節の中心 (レーン番号。左端のレーンの左の縁が 0)
        /// </summary>
        private static float GetCenter(NoteData_Hold note)
        {
            var lanes = note.Address.RangeRP;
            return lanes[0] + lanes.Count * 0.5f;
        }

        #endregion

        #region 印を掴む・動かす・離す

        /// <summary>
        /// カーソルが、表示中の印の上にあるか (フレームごとに1回だけ調べて、結果を使い回す)
        /// </summary>
        private bool IsPointerOnMarker()
        {
            if (hoverCheckedFrame == Time.frameCount) { return isPointerOnMarker; }
            hoverCheckedFrame = Time.frameCount;
            isPointerOnMarker = false;

            if (view == null || isDragging) { return false; }
            if (!markerToNext.IsShown && !markerToBack.IsShown) { return false; }

            float radius = view.MarkerRadius;
            Vector2 pointer = Input.mousePosition;
            float distanceToNext = markerToNext.IsShown ? (pointer - markerToNext.MarkerScreen).magnitude : float.MaxValue;
            float distanceToBack = markerToBack.IsShown ? (pointer - markerToBack.MarkerScreen).magnitude : float.MaxValue;

            if (Mathf.Min(distanceToNext, distanceToBack) > radius) { return false; }
            // 印よりも手前に、ほかのボタンやパネルがあるときは、そちらへの操作
            if (!view.IsMarkerInFront(pointer)) { return false; }

            isPointerOnMarker = true;
            isHoveredToNext = distanceToNext <= distanceToBack;
            return true;
        }

        private bool CanStartDrag()
        {
            if (target == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (InputFieldFocus.IsTyping) { return false; }
            return !dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes);
        }

        private void BeginDrag(bool isToNext)
        {
            Marker marker = isToNext ? markerToNext : markerToBack;
            if (!marker.IsShown || marker.Start == null || marker.End == null) { return; }

            isDragging = true;
            isDraggingToNext = isToNext;
            dragStart = marker.Start;
            dragEnd = marker.End;
            dragSpan = null;
            modeBeforeDrag = dataGetter.CurrentEditMode.Value;
            // 覚えていない区間に残っている制御点の位置は、前の区間のためのもの (次の節が入れ替わった) なので、まっすぐから始める
            // (元に戻すときのために、消す前の値を覚えておく)
            controlBeforeDrag = dragStart.CurveControl;
            if (dragStart.CurveControl.HasValue && FindSpan(dragStart, dragEnd) == null) { dragStart.SetCurveControl(null); }
            pressScreenPosition = Input.mousePosition;
            grabOffset = marker.MarkerScreen - pressScreenPosition;
            hasApplied = false;
            originalRelays.Clear();

            dataSetter.SetEditMode(EditMode.CurveHandling);
        }

        private void UpdateDrag()
        {
            Camera camera = EditorViewCameras.GroundView;
            if (camera == null) { return; }

            // 掴んだだけで動かしていない間は、何も変えない
            if (!hasApplied && ((Vector2)Input.mousePosition - pressScreenPosition).sqrMagnitude < 4f) { return; }

            if (!TryGetNodeWorld(dragStart, out Vector3 startWorld, out float laneWidth)) { return; }
            if (!TryGetNodeWorld(dragEnd, out Vector3 endWorld, out _)) { return; }

            // 印の中心の位置 (カーソルの位置に、掴んだときのずれを足したもの)。画面の左右の外へは出さない
            Vector2 pointer = (Vector2)Input.mousePosition + grabOffset;
            pointer.x = ClampToScreenWidth(pointer.x);

            // 譜面の面の上の位置に直す
            var plane = new Plane(Vector3.up, startWorld);
            Ray ray = camera.ScreenPointToRay(pointer);
            if (!plane.Raycast(ray, out float distance)) { return; }
            Vector3 point = ray.GetPoint(distance);

            // 時間方向: 始まりの節から終わりの節までの、何割のところか
            // (2つの節の間からは出さない。出すと、曲線が節の手前や先へはみ出して戻ってくる形になり、ホールドでは表せない)
            float depth = endWorld.z - startWorld.z;
            if (Mathf.Approximately(depth, 0f)) { return; }
            float time = Mathf.Clamp01((point.z - startWorld.z) / depth);

            // レーンの方向: 2つの節の中心をまっすぐ結んだ線から、横へどれだけ離れているか (レーンの外へも出せる)
            float startCenter = GetCenter(dragStart);
            float endCenter = GetCenter(dragEnd);
            float lane = startCenter + (point.x - startWorld.x) / laneWidth;
            float side = Mathf.Clamp(lane - Mathf.Lerp(startCenter, endCenter, time), -MAX_SIDE_OFFSET, MAX_SIDE_OFFSET);
            if (Mathf.Abs(side) < STRAIGHT_SNAP) { side = 0f; }

            // 制御点の位置が変わっていなければ、何もしない (掴んだまま止めている間)
            var control = new Vector2(side, time);
            Vector2? current = dragStart.CurveControl;
            if (hasApplied && current.HasValue && current.Value == control) { return; }

            dragStart.SetCurveControl(control);
            ApplyCurve();
        }

        /// <summary>
        /// 掴んでいる制御点をはさむ2つの節の間を、今の制御点の位置の曲線でつなぐように、メッシュ中継点を置き直す
        /// </summary>
        private void ApplyCurve()
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null) { return; }

            SpanState state = GetSpanState(dragStart, dragEnd);
            if (!TryDerive(chartData, state)) { return; }

            if (!hasApplied)
            {
                // 最初の1回: 間にあったメッシュ中継点を覚えて、譜面から外す
                hasApplied = true;
                hasEverCurved = true;
                dragSpan = GetOrAddSpan(dragStart, dragEnd);

                CollectRelaysToClear(dragSpan);
                foreach (var relay in relayBuffer)
                {
                    originalRelays.Add(new OriginalRelay { Note = relay, Address = new AddressWithinRange(relay.Address) });
                    chartData.RemoveNote(relay, false);
                }
            }

            FillChainSet(GetChain(dragStart));
            ApplyDerived(chartData, dragSpan);
            dragSpan.Applied = state;
            dragSpan.Checked = state;
        }

        private void EndDrag()
        {
            isDragging = false;

            // モードを戻す (読み込みなどで、ほかの処理がモードを切り替えていたときは、そのままにする)
            if (dataGetter.CurrentEditMode.Value == EditMode.CurveHandling)
            {
                dataSetter.SetEditMode(modeBeforeDrag == EditMode.CurveHandling ? EditMode.None : modeBeforeDrag);
            }

            NoteData_Hold start = dragStart;
            CurvedSpan span = dragSpan;
            dragStart = null;
            dragEnd = null;
            dragSpan = null;

            // メッシュ中継点を選んで制御点を動かすと、その中継点は、置き直しでいったん譜面から外れて、選択も外れる
            // (同じ分線にまた置かれて、譜面へ戻っていることもある)。選んでいるものが無くなると、制御点の印も消えてしまうので、
            // 戻っていればその中継点を、無くなっていれば区間の始まりの節を、選び直す
            if (hasApplied && !isApplyingFromPanel && start != null && target != null && target.NoteType == DeploymentNoteType.HoldMeshRelay && !IsSelecting(target))
            {
                notesSetter.ClearSelectingNotes();
                notesSetter.TryAddSelectingNotes(IsInChain(target) ? target : start);
            }

            if (!hasApplied || span == null)
            {
                // 掴んだだけ、または置く場所が無かった: 制御点も動かす前の位置に戻す
                if (start != null) { start.SetCurveControl(controlBeforeDrag); }
                originalRelays.Clear();
                return;
            }

            // 動かす前からの変更を、1回ぶんとして履歴に記録する (変更そのものは、もう譜面に入っている)
            Vector2? before = controlBeforeDrag;
            Vector2? after = start.CurveControl;
            var originals = new List<OriginalRelay>(originalRelays);
            originalRelays.Clear();

            Record(() =>
            {
                // やり直す: 元からあった中継点を外して、制御点の位置の曲線のとおりに置き直す
                var chartData = dataGetter.ChartData.Value;
                if (chartData == null) { return; }

                ClearRelays(chartData, span);
                span.Start.SetCurveControl(after);

                SpanState state = GetSpanState(span.Start, span.End);
                if (TryDerive(chartData, state))
                {
                    FillChainSet(GetChain(span.Start));
                    ApplyDerived(chartData, span);
                }
                span.Applied = state;
                span.Checked = state;
            },
            () =>
            {
                // 元に戻す: 置いた中継点を外して、元からあった中継点を、元の場所へ戻す
                var chartData = dataGetter.ChartData.Value;
                if (chartData == null) { return; }

                ClearRelays(chartData, span);
                foreach (var original in originals)
                {
                    original.Note.SetAddress(original.Address);
                    original.Note.SetNoteType(DeploymentNoteType.HoldMeshRelay);
                    chartData.AddNote(original.Note);
                }
                span.Start.SetCurveControl(before);

                SpanState state = GetSpanState(span.Start, span.End);
                span.Applied = state;
                span.Checked = state;
            },
            false);

            int count = 0;
            foreach (var pair in span.Relays)
            {
                if (IsInChain(pair.Value)) { count++; }
            }
            Debug.Log($"【ホールドの曲線】メッシュ中継点を {count} 個置きました");
        }

        /// <summary>
        /// 掴んでいる状態を、何も記録せずにやめる (途中で失敗したとき用)
        /// </summary>
        private void AbortDrag()
        {
            // 途中まで置き直していたら、動かす前の状態へ戻す (元からあった中継点を、無くしたままにしない)
            // 区間の節が、まだ同じつながりにあるときだけ (譜面が入れ替わっていたら、触らない)
            try
            {
                var chartData = dataGetter.ChartData.Value;
                if (hasApplied && dragSpan != null && chartData != null && ReferenceEquals(chartData, curvedSpansChart) && IsDragSpanInChart())
                {
                    ClearRelays(chartData, dragSpan);
                    foreach (var original in originalRelays)
                    {
                        original.Note.SetAddress(original.Address);
                        original.Note.SetNoteType(DeploymentNoteType.HoldMeshRelay);
                        chartData.AddNote(original.Note);
                    }
                    dragStart.SetCurveControl(controlBeforeDrag);

                    SpanState state = GetSpanState(dragSpan.Start, dragSpan.End);
                    dragSpan.Applied = state;
                    dragSpan.Checked = state;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            isDragging = false;
            hasApplied = false;
            dragStart = null;
            dragEnd = null;
            dragSpan = null;
            originalRelays.Clear();

            if (dataGetter.CurrentEditMode.Value == EditMode.CurveHandling)
            {
                dataSetter.SetEditMode(modeBeforeDrag == EditMode.CurveHandling ? EditMode.None : modeBeforeDrag);
            }
        }

        /// <summary>
        /// 掴んでいる制御点をはさむ2つの節が、まだ同じつながりの中にあるか
        /// </summary>
        private bool IsDragSpanInChart()
        {
            if (dragStart == null || dragEnd == null) { return false; }

            var list = GetChain(dragStart);
            return list != null && IndexOf(list, dragStart) >= 0 && IndexOf(list, dragEnd) >= 0;
        }

        private bool IsSelecting(NoteData_Hold note)
        {
            foreach (var selecting in notesGetter.SelectingNotes)
            {
                if (ReferenceEquals(selecting, note)) { return true; }
            }
            return false;
        }

        private bool IsInChain(NoteData_Hold note)
        {
            if (note == null || note.ChainIndex.Value < 0) { return false; }

            var list = GetChain(note);
            return list != null && IndexOf(list, note) >= 0;
        }

        #endregion

        #region 曲げた区間と、その区間に置いた中継点

        /// <summary>
        /// 2つの節の今の状態 (どの分線の、どのレーンにあるか) と、制御点の位置
        /// </summary>
        private struct SpanState
        {
            public int StartBar, StartSub, StartLeft, StartWidth;
            public int EndBar, EndSub, EndLeft, EndWidth;
            public bool HasControl;
            public Vector2 Control;

            public bool IsSame(SpanState other)
            {
                return StartBar == other.StartBar && StartSub == other.StartSub && StartLeft == other.StartLeft && StartWidth == other.StartWidth &&
                       EndBar == other.EndBar && EndSub == other.EndSub && EndLeft == other.EndLeft && EndWidth == other.EndWidth &&
                       HasControl == other.HasControl && Control == other.Control;
            }
        }

        /// <summary>
        /// 制御点で曲げた区間 (隣り合った2つの節)
        /// </summary>
        private class CurvedSpan
        {
            public NoteData_Hold Start;
            public NoteData_Hold End;
            // 間の中継点が、曲線のとおりに置いてあると確かめた、最後の状態
            public SpanState Applied;
            // 最後に調べたときの状態 (ここから変わっていなければ、何も調べない)
            public SpanState Checked;
            // この区間に置いた中継点 (分線ごとに1つ)。譜面から外している間も、覚えておく。
            // 節を動かして、同じ分線にまた中継点が要るようになったときに、同じものを譜面へ戻すため
            // (別のものに作り直すと、それまでの操作の履歴が、譜面に無い中継点を指したままになる)
            public readonly Dictionary<long, NoteData_Hold> Relays = new Dictionary<long, NoteData_Hold>();
        }

        // 制御点で曲げた区間 (編集している間だけ覚えている)
        readonly List<CurvedSpan> curvedSpans = new List<CurvedSpan>();
        // 覚えている区間が属する譜面 (譜面が入れ替わったら、覚えていた区間は捨てる)
        ChartData curvedSpansChart;

        // 選ばれたばかりの節 (次のフレームで、その節の曲げた区間を覚える)
        readonly List<NoteData_Hold> newlySelectedNodes = new List<NoteData_Hold>();
        IDisposable selectionSubscription;
        // 制御点を1回でも動かしたか (動かすまでは、曲げた区間はどこにも無いので、選択のたびに調べない)
        bool hasEverCurved;

        // 作業用 (置き直すたびに確保しないよう使い回す)
        readonly AddressInChart locationAddress = new AddressInChart();
        // 2つの節の間にある、すべての分線と、その時間方向の位置
        readonly List<AddressInChart> lineAddresses = new List<AddressInChart>();
        readonly List<float> linePositions = new List<float>();
        float spanStartPosition;
        float spanEndPosition;
        // lineAddresses を調べたときの、2つの節の場所 (同じ場所なら、調べ直さない)
        ChartData preparedChart;
        int preparedStartBar = -1, preparedStartSub, preparedEndBar, preparedEndSub;
        int preparedFrame = -1;
        // 曲線から求めた、中継点を置く分線と、占めるレーン
        readonly List<int> selectedIndices = new List<int>();
        readonly List<HoldCurveMath.Lanes> selectedLanes = new List<HoldCurveMath.Lanes>();
        readonly List<AddressInChart> neededLines = new List<AddressInChart>();
        readonly List<HoldCurveMath.Lanes> neededLanes = new List<HoldCurveMath.Lanes>();
        readonly HashSet<long> neededKeys = new HashSet<long>();
        // つながりに入っているノーツ
        readonly HashSet<IChainNoteData> chainSet = new HashSet<IChainNoteData>();
        readonly List<NoteData_Hold> relayBuffer = new List<NoteData_Hold>();
        readonly List<long> keyBuffer = new List<long>();

        private static SpanState GetSpanState(NoteData_Hold start, NoteData_Hold end)
        {
            Vector2? control = start.CurveControl;
            var startLanes = start.Address.RangeRP;
            var endLanes = end.Address.RangeRP;

            return new SpanState
            {
                StartBar = start.Address.BarIndex,
                StartSub = start.Address.SubDivisionIndex,
                StartLeft = startLanes.Count > 0 ? (int)startLanes[0] : 0,
                StartWidth = startLanes.Count,
                EndBar = end.Address.BarIndex,
                EndSub = end.Address.SubDivisionIndex,
                EndLeft = endLanes.Count > 0 ? (int)endLanes[0] : 0,
                EndWidth = endLanes.Count,
                HasControl = control.HasValue,
                Control = control ?? Vector2.zero,
            };
        }

        /// <summary>
        /// 分線を表す番号 (小節と、その中の分線の番号を、1つの数にまとめたもの)
        /// </summary>
        private static long LineKey(int bar, int sub)
        {
            return ((long)bar << 24) | (uint)sub;
        }

        private static long LineKey(NoteData_Hold note)
        {
            return LineKey(note.Address.BarIndex, note.Address.SubDivisionIndex);
        }

        /// <summary>
        /// 中継点が、その分線の、そのレーンにあるか
        /// </summary>
        private static bool IsAt(NoteData_Hold relay, AddressInChart line, HoldCurveMath.Lanes lanes)
        {
            var address = relay.Address;
            if (address.BarIndex != line.BarIndex || address.SubDivisionIndex != line.SubDivisionIndex) { return false; }

            var range = address.RangeRP;
            return range.Count == lanes.Width && range.Count > 0 && (int)range[0] == lanes.Left;
        }

        private void FillChainSet(IReadOnlyReactiveCollection<IChainNoteData> list)
        {
            chainSet.Clear();
            if (list == null) { return; }

            for (int i = 0; i < list.Count; i++) { chainSet.Add(list[i]); }
        }

        /// <summary>
        /// その区間の制御点の位置 (制御点が無い = まっすぐ、のときは null)
        ///
        /// 制御点の位置は、区間の早い方の節が持っている。その節の次の節が入れ替わったとき (消した、つなぎ直した、複製した) は、
        /// 前の区間のための位置が節に残ったままになる。曲げた区間として覚えている区間のときだけ、その位置を使う
        /// </summary>
        private Vector2? GetSpanControl(NoteData_Hold start, NoteData_Hold end)
        {
            if (start == null) { return null; }
            // 掴んでいる最中の区間は、今動かしている位置そのもの
            if (isDragging && ReferenceEquals(start, dragStart) && ReferenceEquals(end, dragEnd)) { return start.CurveControl; }

            return FindSpan(start, end) != null ? start.CurveControl : null;
        }

        private CurvedSpan FindSpan(NoteData_Hold start, NoteData_Hold end)
        {
            foreach (var span in curvedSpans)
            {
                if (ReferenceEquals(span.Start, start) && ReferenceEquals(span.End, end)) { return span; }
            }
            return null;
        }

        /// <summary>
        /// 区間を、曲げた区間として覚える (もう覚えていれば、それを返す)
        /// </summary>
        private CurvedSpan GetOrAddSpan(NoteData_Hold start, NoteData_Hold end)
        {
            ResetSpansIfChartChanged();

            CurvedSpan span = FindSpan(start, end);
            if (span != null) { return span; }

            SpanState state = GetSpanState(start, end);
            span = new CurvedSpan { Start = start, End = end, Applied = state, Checked = state };
            curvedSpans.Add(span);
            return span;
        }

        private void ResetSpansIfChartChanged()
        {
            var chartData = dataGetter.ChartData.Value;
            if (ReferenceEquals(chartData, curvedSpansChart)) { return; }

            curvedSpans.Clear();
            curvedSpansChart = chartData;
        }

        /// <summary>
        /// ホールドのノーツが選ばれた
        /// </summary>
        private void OnHoldSelected(NoteData_Hold hold)
        {
            if (!hasEverCurved) { return; }
            if (hold.NoteType == DeploymentNoteType.HoldMeshRelay && !NoteLock.IsLocked(hold)) { return; }

            newlySelectedNodes.Add(hold);
        }

        /// <summary>
        /// 制御点が動かしてある区間で、まだ覚えていなければ、覚える (複製した曲線など)
        /// 間のメッシュ中継点が、今の曲線のとおりに置いてあるときだけ
        /// </summary>
        private void RegisterSpanIfCurved(ChartData chartData, NoteData_Hold start, NoteData_Hold end)
        {
            if (start == null || end == null || start.Address == null || end.Address == null) { return; }
            if (!start.CurveControl.HasValue) { return; }
            if (FindSpan(start, end) != null) { return; }

            var list = GetChain(start);
            if (list == null) { return; }
            int startIndex = IndexOf(list, start);
            int endIndex = IndexOf(list, end);
            if (startIndex < 0 || endIndex <= startIndex) { return; }

            SpanState state = GetSpanState(start, end);
            if (!TryDerive(chartData, state)) { return; }
            if (!IsOnCurve(list, startIndex, endIndex)) { return; }

            var span = new CurvedSpan { Start = start, End = end, Applied = state, Checked = state };
            AdoptRelays(span, list, startIndex, endIndex);
            curvedSpans.Add(span);
        }

        #endregion

        #region 曲げた区間の中継点を、節の動きに合わせて置き直す

        /// <summary>
        /// 覚えている区間の節が動いていたら (動かした、幅を変えた、元に戻した)、間の中継点を曲線に合わせて置き直す
        /// </summary>
        private void FollowCurvedSpans()
        {
            var chartData = dataGetter.ChartData.Value;
            if (dataGetter.IsLoadingChart.Value || chartData == null)
            {
                curvedSpans.Clear();
                newlySelectedNodes.Clear();
                return;
            }

            ResetSpansIfChartChanged();

            // 選ばれたばかりの節の、前後の区間を覚える (この時点では、まだ動かされていない)
            if (newlySelectedNodes.Count > 0)
            {
                foreach (var node in newlySelectedNodes)
                {
                    if (node.Address == null || node.ChainIndex.Value < 0) { continue; }

                    RegisterSpanIfCurved(chartData, node, FindNeighbor(node, true));
                    RegisterSpanIfCurved(chartData, FindNeighbor(node, false), node);
                }
                newlySelectedNodes.Clear();
            }

            for (int i = 0; i < curvedSpans.Count; i++)
            {
                var span = curvedSpans[i];

                SpanState state = GetSpanState(span.Start, span.End);
                if (state.IsSame(span.Checked)) { continue; }
                span.Checked = state;

                if (FollowSpan(chartData, span, state)) { span.Applied = state; }
            }
        }

        /// <summary>
        /// 区間の中継点を、今の節と制御点の位置の曲線に合わせて置き直す
        ///
        /// 置き直すのは、最後に合わせたときのまま、中継点が手つかずで残っているときだけ。
        /// 手で動かしたり、足したり、消したりしてあれば、何もしない (制御点をもう一度動かすと、置き直される)。
        /// 置き直しは、履歴には記録しない。中継点の置き方は、節と制御点の位置だけで決まるので、
        /// 節の移動を元に戻せば、中継点も、そのときの置き方に戻る。
        /// </summary>
        /// <param name="state">今の状態</param>
        /// <returns>中継点が、今の曲線のとおりに置いてある状態になったか</returns>
        private bool FollowSpan(ChartData chartData, CurvedSpan span, SpanState state)
        {
            // 制御点が、動かしていない状態に戻っている (制御点の操作を元に戻したとき)
            if (!state.HasControl) { return false; }
            // 2つの節が、隣り合った節どうしではなくなっている (間に節を足した、節を消した、順番が入れ替わった)
            if (!ReferenceEquals(FindNeighbor(span.Start, true), span.End)) { return false; }

            var list = GetChain(span.Start);
            if (list == null) { return false; }
            int startIndex = IndexOf(list, span.Start);
            int endIndex = IndexOf(list, span.End);
            if (startIndex < 0 || endIndex <= startIndex) { return false; }

            FillChainSet(list);

            // もう、今の曲線のとおりに置いてある (ホールド全体をまとめて動かした、制御点の操作を元に戻した・やり直した)
            if (!TryDerive(chartData, state)) { return false; }
            if (IsOnCurve(list, startIndex, endIndex))
            {
                AdoptRelays(span, list, startIndex, endIndex);
                return true;
            }

            // 最後に合わせたときのまま残っていなければ、手で直してある
            if (!TryDerive(chartData, span.Applied)) { return false; }
            if (!IsUntouched(span, list, startIndex, endIndex)) { return false; }

            if (!TryDerive(chartData, state)) { return false; }
            ApplyDerived(chartData, span);
            return true;
        }

        /// <summary>
        /// 2つの節の間にあるノーツが、求めた中継点 (neededLines・neededLanes) と、ちょうど同じか
        /// </summary>
        private bool IsOnCurve(IReadOnlyReactiveCollection<IChainNoteData> list, int startIndex, int endIndex)
        {
            if (endIndex - startIndex - 1 != neededLines.Count) { return false; }

            for (int i = 0; i < neededLines.Count; i++)
            {
                var relay = list[startIndex + 1 + i] as NoteData_Hold;
                if (relay == null || relay.NoteType != DeploymentNoteType.HoldMeshRelay) { return false; }
                if (!IsAt(relay, neededLines[i], neededLanes[i])) { return false; }
            }
            return true;
        }

        /// <summary>
        /// この区間に置いた中継点が、求めた中継点 (neededLines・neededLanes。最後に合わせたときのもの) のまま、残っているか
        /// (chainSet に、つながりのノーツを入れておくこと)
        ///
        /// 節を時間方向に動かした後は、中継点が、2つの節の間からはみ出していることがある。
        /// それでも、中継点そのものが手つかずなら、残っているとみなす (はみ出したぶんは、置き直しで片付く)。
        /// </summary>
        private bool IsUntouched(CurvedSpan span, IReadOnlyReactiveCollection<IChainNoteData> list, int startIndex, int endIndex)
        {
            // 2つの節の間に、この区間の中継点ではないノーツがある (手で足した中継点、別の分線へ動かした中継点)
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                var note = list[i] as NoteData_Hold;
                if (note == null) { return false; }
                if (!span.Relays.TryGetValue(LineKey(note), out var known) || !ReferenceEquals(known, note)) { return false; }
            }

            // 譜面に入っている、この区間の中継点が、求めた中継点と同じ数で、同じ場所にある
            int inChart = 0;
            foreach (var pair in span.Relays)
            {
                if (chainSet.Contains(pair.Value)) { inChart++; }
            }
            if (inChart != neededLines.Count) { return false; }

            for (int i = 0; i < neededLines.Count; i++)
            {
                if (!span.Relays.TryGetValue(LineKey(neededLines[i].BarIndex, neededLines[i].SubDivisionIndex), out var relay)) { return false; }
                if (!chainSet.Contains(relay) || !IsAt(relay, neededLines[i], neededLanes[i])) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 2つの節の間にあるメッシュ中継点を、この区間に置いた中継点として覚える
        /// </summary>
        private void AdoptRelays(CurvedSpan span, IReadOnlyReactiveCollection<IChainNoteData> list, int startIndex, int endIndex)
        {
            relayBuffer.Clear();
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (list[i] is NoteData_Hold relay) { relayBuffer.Add(relay); }
            }

            // 別の分線の中継点として覚えていたもの (ホールド全体を、時間方向に動かした後) は、覚え直す
            keyBuffer.Clear();
            foreach (var pair in span.Relays)
            {
                if (pair.Key != LineKey(pair.Value) && relayBuffer.Contains(pair.Value)) { keyBuffer.Add(pair.Key); }
            }
            foreach (long key in keyBuffer) { span.Relays.Remove(key); }

            foreach (var relay in relayBuffer) { span.Relays[LineKey(relay)] = relay; }
        }

        #endregion

        #region メッシュ中継点を置く・外す

        /// <summary>
        /// 節と制御点が state の状態のときの曲線から、中継点を置く分線と、占めるレーンを求める
        /// (結果は neededLines・neededLanes に入る)
        /// </summary>
        /// <returns>求められたか (分線の場所が分からないときは false。間に分線が無いときは、結果が空で true)</returns>
        private bool TryDerive(ChartData chartData, SpanState state)
        {
            neededLines.Clear();
            neededLanes.Clear();

            if (!TryPrepareLines(chartData, state.StartBar, state.StartSub, state.EndBar, state.EndSub)) { return false; }
            if (lineAddresses.Count == 0) { return true; }

            var startNode = new HoldCurveMath.Node { Position = spanStartPosition, Left = state.StartLeft, Width = state.StartWidth };
            var endNode = new HoldCurveMath.Node { Position = spanEndPosition, Left = state.EndLeft, Width = state.EndWidth };

            HoldCurveMath.SelectRelays(
                startNode,
                endNode,
                state.HasControl ? state.Control : (Vector2?)null,
                linePositions,
                selectedIndices,
                selectedLanes);

            for (int i = 0; i < selectedIndices.Count; i++)
            {
                neededLines.Add(lineAddresses[selectedIndices[i]]);
                neededLanes.Add(selectedLanes[i]);
            }
            return true;
        }

        /// <summary>
        /// 求めた中継点 (neededLines・neededLanes) のとおりに、この区間の中継点を置き直す
        /// (chainSet に、つながりのノーツを入れておくこと)
        ///
        /// ・要らなくなった分線の中継点は、譜面から外す (覚えてはおく)
        /// ・要る分線に、もう中継点があれば、レーンだけ直す
        /// ・要る分線に、中継点が無ければ、前にその分線に置いていたものを譜面へ戻す。無ければ、新しく作る
        /// </summary>
        private void ApplyDerived(ChartData chartData, CurvedSpan span)
        {
            neededKeys.Clear();
            for (int i = 0; i < neededLines.Count; i++)
            {
                neededKeys.Add(LineKey(neededLines[i].BarIndex, neededLines[i].SubDivisionIndex));
            }

            // 外す (外すと、覚えている中継点の一覧を調べている途中で、つながりが変わるので、先に集める)
            // (ロックした中継点は、外さない・動かさない)
            relayBuffer.Clear();
            foreach (var pair in span.Relays)
            {
                if (!neededKeys.Contains(pair.Key) && chainSet.Contains(pair.Value) && !NoteLock.IsLocked(pair.Value)) { relayBuffer.Add(pair.Value); }
            }
            foreach (var relay in relayBuffer) { chartData.RemoveNote(relay, false); }

            // 置く・直す
            for (int i = 0; i < neededLines.Count; i++)
            {
                AddressInChart line = neededLines[i];
                long key = LineKey(line.BarIndex, line.SubDivisionIndex);

                span.Relays.TryGetValue(key, out NoteData_Hold relay);
                bool isInChart = relay != null && chainSet.Contains(relay);

                // 覚えていた中継点が、別の分線へ動かされている: それには触らず、新しく作る
                if (isInChart && LineKey(relay) != key)
                {
                    relay = null;
                    isInChart = false;
                }

                // 覚えていた中継点が、つながりから外されて (接続の解除)、譜面には残っている: それには触らず、新しく作る
                // (譜面に入っているノーツを、もう一度譜面へ足してしまわないように)
                if (relay != null && !isInChart && notesGetter.GetNoteObject(relay) != null)
                {
                    relay = null;
                }

                if (isInChart)
                {
                    if (!IsAt(relay, line, neededLanes[i]) && !NoteLock.IsLocked(relay)) { relay.SetAddress(CreateAddress(line, neededLanes[i])); }
                    continue;
                }

                if (relay == null)
                {
                    relay = new NoteData_Hold();
                    span.Relays[key] = relay;
                }

                relay.SetAddress(CreateAddress(line, neededLanes[i]));
                relay.SetNoteType(DeploymentNoteType.HoldMeshRelay);
                relay.SetChainIndex(span.Start.ChainIndex.Value);
                chartData.AddNote(relay);
            }
        }

        /// <summary>
        /// 区間から片付ける中継点を集める (結果は relayBuffer に入る)
        /// 2つの節の間にあるメッシュ中継点 (手で置いたものも含む) と、この区間に置いた中継点で、譜面に入っているもの
        /// </summary>
        private void CollectRelaysToClear(CurvedSpan span, bool includesLockedPlaced = false)
        {
            relayBuffer.Clear();

            var list = GetChain(span.Start);
            FillChainSet(list);
            if (list == null) { return; }

            int startIndex = IndexOf(list, span.Start);
            int endIndex = IndexOf(list, span.End);
            // (ロックしたメッシュ中継点は、外さない)
            for (int i = startIndex + 1; startIndex >= 0 && i < endIndex; i++)
            {
                if (list[i] is NoteData_Hold relay && relay.NoteType == DeploymentNoteType.HoldMeshRelay && !NoteLock.IsLocked(relay)) { relayBuffer.Add(relay); }
            }

            // (この区間が置いた中継点は、元に戻す・やり直すときは、後からロックされていても外す。残すと、元の中継点と重なる)
            foreach (var pair in span.Relays)
            {
                if (!chainSet.Contains(pair.Value) || relayBuffer.Contains(pair.Value)) { continue; }
                if (NoteLock.IsLocked(pair.Value) && !includesLockedPlaced) { continue; }
                relayBuffer.Add(pair.Value);
            }
        }

        /// <summary>
        /// 区間の中継点を、すべて譜面から外す
        /// </summary>
        private void ClearRelays(ChartData chartData, CurvedSpan span)
        {
            CollectRelaysToClear(span, true);
            foreach (var relay in relayBuffer)
            {
                if (NoteLock.IsLocked(relay)) { NoteLock.Unlock(new IDeployableNoteData[] { relay }); }
                chartData.RemoveNote(relay, false);
            }
        }

        /// <summary>
        /// 2つの場所の間にある、すべての分線と、その位置を調べる (結果は lineAddresses・linePositions などに入る)
        /// </summary>
        /// <returns>調べられたか (間に分線が無いときは、結果が空で true)</returns>
        private bool TryPrepareLines(ChartData chartData, int startBar, int startSub, int endBar, int endSub)
        {
            // 同じフレームの中 (と、制御点を掴んでいる間) は、同じ場所を調べ直さない
            bool isSame = ReferenceEquals(preparedChart, chartData) &&
                          preparedStartBar == startBar && preparedStartSub == startSub && preparedEndBar == endBar && preparedEndSub == endSub;
            if (isSame && (preparedFrame == Time.frameCount || (isDragging && hasApplied))) { return true; }

            preparedStartBar = -1;
            lineAddresses.Clear();
            linePositions.Clear();

            if (!TryGetLinePosition(chartData, startBar, startSub, out spanStartPosition)) { return false; }
            if (!TryGetLinePosition(chartData, endBar, endSub, out spanEndPosition)) { return false; }

            // 2つの場所の間 (両端は含まない) にある分線を、時間の順に集める
            var bars = chartData.BarDatas;
            for (int bar = startBar; bar <= endBar && bar < bars.Count; bar++)
            {
                int firstSub = bar == startBar ? startSub + 1 : 0;
                int lastSub = bar == endBar ? endSub - 1 : bars[bar].SubDivisionDatas.Count - 1;

                for (int sub = firstSub; sub <= lastSub; sub++)
                {
                    if (!TryGetLinePosition(chartData, bar, sub, out float position))
                    {
                        lineAddresses.Clear();
                        linePositions.Clear();
                        return false;
                    }

                    lineAddresses.Add(new AddressInChart(bar, sub, 0f));
                    linePositions.Add(position);
                }
            }

            preparedChart = chartData;
            preparedStartBar = startBar;
            preparedStartSub = startSub;
            preparedEndBar = endBar;
            preparedEndSub = endSub;
            preparedFrame = Time.frameCount;
            return true;
        }

        /// <summary>
        /// 分線の、時間方向の位置 (譜面の画面(3D)の中の奥行き。後ろの分線ほど大きい)
        /// </summary>
        private bool TryGetLinePosition(ChartData chartData, int bar, int sub, out float position)
        {
            position = 0f;

            var bars = chartData.BarDatas;
            if (bar < 0 || bar >= bars.Count || sub < 0 || sub >= bars[bar].SubDivisionDatas.Count) { return false; }

            Transform location = GetLocation(chartData, bar, sub, 0f);
            if (location == null) { return false; }

            position = location.position.z;
            return true;
        }

        private static AddressWithinRange CreateAddress(AddressInChart line, HoldCurveMath.Lanes lanes)
        {
            var range = new List<float>(lanes.Width);
            for (int i = 0; i < lanes.Width; i++) { range.Add(lanes.Left + i); }

            return new AddressWithinRange(line.BarIndex, line.SubDivisionIndex, range);
        }

        #endregion
    }
}
