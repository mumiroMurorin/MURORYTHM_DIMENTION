using System;
using System.Collections.Generic;
using ChartConvert;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// プレビュー(編集画面のミニプレビューと、全画面プレビュー)に映っているノーツを、クリックで選択し、ドラッグで動かす
    ///
    /// 【選択】プレビューのノーツと譜面のノーツは番号などで結び付けていないので、
    /// 「時刻」と「レーン」(スペースホールドは接続番号)が一致する譜面側のノーツを探して対応を取る。
    /// ホールドの帯をクリックしたときは、そのホールドにつながるノーツをまとめて選択する。
    /// ノーツが重なって見える場所では、いちばん小さく見えるノーツを選ぶ。
    /// 同じ場所をもう一度クリックすると、その場所に重なっている次のノーツへ選択が移る。
    /// 複数選択中のノーツを1つクリックすると、そのノーツだけを選び直す。
    ///
    /// 【移動】ノーツを押したままカーソルを動かすと、選択中のノーツがカーソルに付いて動く。
    /// カーソルが指している「レーン」と「分線」を PreviewTrackSurface で求め、掴んだ位置との差だけ動かす。
    /// 動かすたびにプレビューへすぐ反映するので、プレビューの中でも動いて見える。
    ///
    /// 【頂点】スペースのノーツを選ぶと、頂点と中心点のつまみが出る (PreviewVertexHandles)。
    /// つまみを動かすと、頂点だけ・または形ごと動かせる。
    ///
    /// 【複数選択】Ctrl を押しながらクリックすると、選択に加える (選択中のノーツなら外す)。
    /// ノーツの無い場所からドラッグすると枠が出て、中に映っているノーツをまとめて選ぶ (範囲選択)。
    /// Shift を押しながらドラッグすると、ノーツの上からでも範囲選択を始められる。
    /// 選んだノーツは、ドラッグでまとめて動かせるほか、編集画面と同じキーで削除・コピー・貼り付けができる
    /// (キーの処理は NoteDestroyer と NotesCopier。貼り付け先は、カーソルがプレビューの中で指している分線)。
    /// </summary>
    public class PreviewNotePicker : ITickable, IPostLateTickable, IDisposable, IPreviewPointer
    {
        // 時刻の一致とみなす誤差(秒)
        const float TIMING_TOLERANCE_SECONDS = 0.002f;
        // スペースレイヤーのノーツを表すレーン番号
        const float SPACE_LANE_INDEX = 100f;
        // レーン番号の最大値
        const int MAX_LANE_INDEX = 15;
        // 押してからこの距離(ピクセル)以上動かしたら、クリックではなくドラッグとして扱う
        const float DRAG_START_PIXELS = 6f;
        // 直前のクリックからこの距離(ピクセル)以内をクリックしたら、「同じ場所をもう一度クリックした」として扱う
        const float SAME_PLACE_PIXELS = 8f;
        // 1回のクリックで調べる、重なっているノーツの数の上限
        const int MAX_PRESS_CANDIDATES = 32;
        // スペースのノーツをドラッグで動かせる、視点の正面からの最小のずれ (これより正面に近い点は掴んでも動かせない)
        const float RAIL_MIN_OFFSET = 0.5f;

        // 操作の途中なので、プレビューのクリックを受け付けないモード
        // (選択するとレイヤーや編集モードが切り替わり、入力途中の設定が確定されたりするため)
        static readonly EditMode[] ignoreEditModes =
        {
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.Connecting,
            EditMode.SpaceMoving,
            EditMode.VertexMoving,
            EditMode.VerticesRotating,
            EditMode.VerticesScaling,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
            EditMode.Preview,
        };

        // 画面を覆う表示が出ていて、つまみを出さないモード (説明書や、小節・分線の設定の入力欄の上に重ならないように)
        static readonly EditMode[] handleHiddenEditModes =
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;
        readonly INoteSpawnDataOptionGetter optionHolder;

        // プレビューを映している画面 (ミニプレビューと全画面プレビュー)
        readonly List<PreviewView> previewViews = new List<PreviewView>();
        bool hasSearchedScene;
        ChartPreviewGenerator previewGenerator;
        ChartPreviewRefresher previewRefresher;
        PreviewUIPresenter previewUIPresenter;

        // スペースのノーツの頂点・中心点のつまみ
        readonly PreviewVertexHandles vertexHandles;
        // 押したのが、ノーツではなくつまみか
        bool isHandlePress;

        // --- 押したノーツ ---
        // 押した場所に重なっていたノーツ (選ばれやすい順)
        readonly List<PressCandidate> pressCandidates = new List<PressCandidate>();
        // そのうち、押したことにしたノーツの番号
        int pressedCandidateIndex;
        // 押したノーツがすでに選択中で、押した時点では選択を変えなかったか
        bool isKeptPress;
        // 押したノーツを、ドラッグで動かせるか (掴んだ位置を求められたか)
        bool canDragPressedNote;
        // 直前にノーツをクリックした位置
        bool hasLastClick;
        Vector2 lastClickPosition;

        // --- 範囲選択 ---
        // 押したのが、ノーツもつまみも無い場所か (そのまま動かすと範囲選択になる)
        bool isRangePress;
        // 動かさずに離したとき、選択を解除するか (Shift を押して範囲選択を始めたときは解除しない)
        bool isRangeClearOnClick;
        // 範囲選択の枠を描いている最中か
        bool isRangeSelecting;
        readonly PreviewRangeRect rangeRect = new PreviewRangeRect();
        readonly List<IDeployableNoteData> rangeNotes = new List<IDeployableNoteData>();

        readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        readonly List<INoteData> pickedNotes = new List<INoteData>();
        readonly List<IDeployableNoteData> foundNotes = new List<IDeployableNoteData>();

        // --- ドラッグ ---
        // ノーツを押している最中か (離すまで true)
        bool isPressing;
        bool isDragging;
        Vector2 pressScreenPosition;
        // 押したときに使ったプレビュー (ドラッグ中はカーソルが外に出ても、このプレビューを基準にする)
        PreviewView pressedView;
        // 押した位置の、譜面上の距離とレーン
        float grabDistance;
        float grabLane;
        // スペースのノーツを掴んだか。宙に浮いているので、面の上ではなく「掴んだ点を通るレール」の上で位置を求める
        bool isRailDrag;
        float railX;
        float railY;
        // レールの上で、直前にカーソルが指していた位置
        float railDistance;
        // 押したノーツのある分線
        AddressInChart pressedNoteAddress;
        // 押した位置の分線の番号(譜面の先頭から数えた通し番号)とレーン番号
        int grabSubIndex;
        int grabLaneIndex;
        // 直前にカーソルが指していた分線の通し番号
        int pointedSubIndex;
        // 分線ごとの、プレビュー上の距離 (譜面の先頭から順)
        // 求め直すのは、プレビューが更新されたとき(= 位置の計算が新しくなったとき)と、設定が変わったときだけ
        readonly List<float> subDivisionDistances = new List<float>();
        INotePositionCalculator distancesCalculator;
        ChartData distancesChartData;
        float distancesNoteSpeed;
        float distancesOffset;
        // ドラッグ中のエディットモードと、ドラッグを始める前のエディットモード
        EditMode dragEditMode;
        EditMode editModeBeforeDrag;
        // 動かしているノーツ
        readonly List<DragTarget> dragTargets = new List<DragTarget>();
        // 今の移動量と、動かせる範囲 (譜面やレーンの端からはみ出さない範囲)
        int currentSubDelta;
        int currentLaneDelta;
        int minSubDelta;
        int maxSubDelta;
        int minLaneDelta;
        int maxLaneDelta;

        /// <summary>
        /// 押した場所にあったノーツ (プレビューのノーツ1つと、それに対応する譜面側のノーツ)
        /// </summary>
        private class PressCandidate
        {
            public INoteData PreviewNote;
            public bool IsSpaceNote;
            // 譜面側のノーツ (ホールドの帯では、そのホールドにつながるノーツすべて)
            public readonly List<IDeployableNoteData> Notes = new List<IDeployableNoteData>();
        }

        /// <summary>
        /// ドラッグで動かしているノーツ
        /// </summary>
        private class DragTarget
        {
            public IDeployableNoteData Data;
            public NoteObject NoteObject;
            public bool IsSpaceNote;
            // ドラッグを始めたときのアドレス
            public AddressWithinRange OriginAddress;
            public Action OnMove;
            public Action OnMoveEnd;
        }

        public PreviewNotePicker(
            IChartEditorDataGetter dataGetter,
            IChartEditorDataSetter dataSetter,
            INotesDataGetter notesGetter,
            INotesDataSetter notesSetter,
            INoteSpawnDataOptionGetter optionHolder)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.optionHolder = optionHolder;

            vertexHandles = new PreviewVertexHandles(notesGetter, optionHolder);
        }

        void IDisposable.Dispose()
        {
            vertexHandles.Dispose();
            rangeRect.Dispose();
        }

        void ITickable.Tick()
        {
            // ドラッグの途中でほかの処理にモードを切り替えられたら、その場で移動を確定する (譜面の読み込みが始まったときなど)
            if (isDragging && dataGetter.CurrentEditMode.Value != dragEditMode)
            {
                Release();
            }

            if (Input.GetMouseButtonDown(0))
            {
                Press();
            }
            else if (isPressing && Input.GetMouseButton(0))
            {
                Hold();
            }

            if (isPressing && !Input.GetMouseButton(0))
            {
                Release();
            }
        }

        void IPostLateTickable.PostLateTick()
        {
            // 頂点・中心点のつまみを、今の頂点の位置に合わせる
            // (再生中は地面が動くので、そのフレームの移動がすべて終わった後に行う。先に行うと1フレーム遅れて見える)
            RefreshVertexHandles();
        }

        #region 押す・動かす・離す

        /// <summary>
        /// 押したとき。カーソル下のノーツを選択し、ドラッグに備えて掴んだ位置を覚える
        /// </summary>
        private void Press()
        {
            // 前のドラッグが終わっていなければ、ここで確定する (ボタンを離した瞬間を取りこぼしたとき)
            if (isDragging) { EndDrag(); }
            EndRange();

            isPressing = false;
            isDragging = false;
            isKeptPress = false;
            canDragPressedNote = false;
            pressCandidates.Clear();

            // ほかの操作の途中でないときだけ
            if (!CanOperate()) { return; }

            SearchSceneOnce();
            if (previewGenerator == null) { return; }

            // クリック位置をプレビューの映像上の光線に変換する
            if (!TryGetPreviewRay(Input.mousePosition, out Ray ray, out var view)) { return; }

            // 編集した直後で、まだプレビューに反映されていない変更があれば、先に反映する
            // (古いプレビューを基準に位置を計算すると、動かし始めたときにノーツが飛ぶ)
            if (previewRefresher != null && previewRefresher.FlushPendingRefresh()) { RefreshVertexHandles(); }

            // Shift を押しているときは、ノーツやつまみの上からでも範囲選択を始める
            // (スペースホールドの帯は画面の広い範囲を覆うので、帯の上からも枠を描けるように)
            bool isForcedRange = ModifierKey.Shift;

            // 頂点・中心点のつまみを押したときは、それを掴む (ノーツより手前にあるものとして扱う)
            isHandlePress = !isForcedRange && vertexHandles.TryGrab(view, Input.mousePosition, ray, previewGenerator.NoteParent);
            if (isHandlePress)
            {
                isPressing = true;
                pressScreenPosition = Input.mousePosition;
                pressedView = view;
                return;
            }

            // 押した場所に重なっているノーツを、選ばれやすい順に集める
            if (!isForcedRange) { CollectPressCandidates(ray); }

            if (pressCandidates.Count == 0)
            {
                // ノーツもつまみも無い場所 (または Shift を押している): そのまま動かすと範囲選択になる
                // 動かさずに離したときは選択を解除する (編集画面で、何もない場所をクリックしたときと同じ)
                isRangePress = true;
                isRangeClearOnClick = !isForcedRange;
                isPressing = true;
                pressScreenPosition = Input.mousePosition;
                pressedView = view;
                return;
            }

            // すでに選択中のノーツがその場所にあれば、選択は変えずに、それを掴む
            // ・複数選択したノーツのうち1つを掴んで、まとめて動かせるように
            // ・ほかのノーツが重なって見えていても、選択中のノーツを動かせるように
            // (Ctrl を押しているときは、いちばん選ばれやすいノーツを選択に加える・外す)
            int selectedIndex = ModifierKey.Ctrl ? -1 : FindSelectedCandidateIndex();
            pressedCandidateIndex = Mathf.Max(selectedIndex, 0);
            PressCandidate pressed = pressCandidates[pressedCandidateIndex];

            pressedNoteAddress = new AddressInChart(pressed.Notes[0].Address.BarIndex, pressed.Notes[0].Address.SubDivisionIndex, 0);

            if (selectedIndex >= 0)
            {
                isKeptPress = true;
                ShowLayer(pressed.IsSpaceNote, view.IsFullScreen);
            }
            else if (!Select(pressed, view.IsFullScreen))
            {
                // Ctrl を押しながら選択中のノーツを押して、選択から外したとき: 掴まない
                return;
            }

            // ドラッグに備えて、押した位置を覚える。求められなければ選択だけ (ドラッグでは動かせない)
            isRailDrag = pressed.IsSpaceNote;
            canDragPressedNote = isRailDrag
                ? TryGrabOnRail(ray, pressed.PreviewNote)
                : PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out grabDistance, out grabLane);

            isPressing = true;
            pressScreenPosition = Input.mousePosition;
            pressedView = view;
        }

        /// <summary>
        /// 押した場所に重なっているノーツを、選ばれやすい順に集める (結果は pressCandidates に入る)
        /// </summary>
        private void CollectPressCandidates(Ray ray)
        {
            pressCandidates.Clear();

            previewGenerator.PickNotes(ray, pickedNotes);
            foreach (INoteData previewNoteData in pickedNotes)
            {
                if (pressCandidates.Count >= MAX_PRESS_CANDIDATES) { break; }

                // 譜面側に対応するノーツが無いもの(ホールド途中の判定点など)は飛ばす
                if (!TryFindEditorNotes(previewNoteData, out bool isSpaceNote)) { continue; }

                // 同じノーツに対応するものが、すでに挙がっているときは加えない
                if (HasCandidateOf(foundNotes)) { continue; }

                var candidate = new PressCandidate { PreviewNote = previewNoteData, IsSpaceNote = isSpaceNote };
                candidate.Notes.AddRange(foundNotes);
                pressCandidates.Add(candidate);
            }

            pickedNotes.Clear();
            foundNotes.Clear();
        }

        private bool HasCandidateOf(List<IDeployableNoteData> notes)
        {
            foreach (var candidate in pressCandidates)
            {
                if (candidate.Notes.Count != notes.Count) { continue; }

                bool isSame = true;
                foreach (var note in notes)
                {
                    if (!candidate.Notes.Contains(note))
                    {
                        isSame = false;
                        break;
                    }
                }

                if (isSame) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 押した場所にあるノーツのうち、すでに選択中のもの(のうち、いちばん選ばれやすいもの)の番号を返す。無ければ -1
        /// </summary>
        private int FindSelectedCandidateIndex()
        {
            for (int i = 0; i < pressCandidates.Count; i++)
            {
                if (IsAllSelected(pressCandidates[i].Notes)) { return i; }
            }

            return -1;
        }

        /// <summary>
        /// ノーツを、動かさずにクリックしたとき (ボタンを離した時点で呼ぶ)
        ///
        /// 押した時点で選択を変えなかった場合(すでに選択中のノーツを押した場合)に、ここで選択を決める。
        /// ・同じ場所をもう一度クリックしたとき: その場所に重なっている、次のノーツを選ぶ
        ///   (重なっていて直接は選べないノーツも、クリックを繰り返せば選べる)
        /// ・それ以外: その場所でいちばん選ばれやすいノーツだけを選ぶ
        ///   (複数選択中のノーツを1つクリックすると、そのノーツだけになる)
        /// </summary>
        private void ClickPressedNote()
        {
            bool isSamePlace = hasLastClick &&
                (pressScreenPosition - lastClickPosition).sqrMagnitude <= SAME_PLACE_PIXELS * SAME_PLACE_PIXELS;
            hasLastClick = true;
            lastClickPosition = pressScreenPosition;

            if (!isKeptPress || ModifierKey.Ctrl || pressCandidates.Count == 0) { return; }
            if (!CanOperate() || pressedView == null) { return; }

            int targetIndex = 0;
            if (isSamePlace && IsExactSelection(pressCandidates[pressedCandidateIndex].Notes))
            {
                targetIndex = (pressedCandidateIndex + 1) % pressCandidates.Count;
            }

            PressCandidate target = pressCandidates[targetIndex];
            if (IsExactSelection(target.Notes)) { return; }

            ShowLayer(target.IsSpaceNote, pressedView.IsFullScreen);
            notesSetter.ClearSelectingNotes();
            foreach (var note in target.Notes)
            {
                // 押してから離すまでの間に削除されたノーツは選ばない
                if (notesGetter.GetNoteObject(note) == null) { continue; }

                notesSetter.TryAddSelectingNotes(note);
            }
        }

        /// <summary>
        /// 押したまま。一定以上動かしたらドラッグを始め、カーソルに合わせてノーツを動かす
        /// </summary>
        private void Hold()
        {
            if (isRangePress)
            {
                HoldRange();
                return;
            }

            if (!isDragging)
            {
                Vector2 moved = (Vector2)Input.mousePosition - pressScreenPosition;
                if (moved.sqrMagnitude < DRAG_START_PIXELS * DRAG_START_PIXELS) { return; }

                // 同じクリックで譜面側の操作(ノーツの移動など)が始まっていたら、そちらに任せる
                // 掴んだ位置を求められなかったノーツは、ドラッグでは動かせない
                if (!CanOperate() || (!isHandlePress && !canDragPressedNote))
                {
                    CancelPress();
                    return;
                }

                bool isStarted = isHandlePress ? BeginHandleDrag() : BeginDrag();
                if (!isStarted)
                {
                    CancelPress();
                    return;
                }
            }

            if (isHandlePress) { UpdateHandleDrag(); }
            else { UpdateDrag(); }
        }

        /// <summary>
        /// 押している状態をやめる (ドラッグを始められなかったとき)
        /// </summary>
        private void CancelPress()
        {
            if (isHandlePress) { vertexHandles.Release(); }
            EndRange();

            isPressing = false;
        }

        /// <summary>
        /// プレビューの上で、選択やドラッグを始めてよい状態か
        /// </summary>
        private bool CanOperate()
        {
            if (dataGetter.ChartData.Value == null) { return false; }
            if (dataGetter.IsLoadingChart.Value) { return false; }

            var editMode = dataGetter.CurrentEditMode.Value;

            // 全画面プレビューの間は、エディットモードは Preview のまま
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return editMode == EditMode.Preview; }

            // 編集画面では、ほかの操作の途中でないこと
            return !editMode.IsInEditModeList(ignoreEditModes);
        }

        /// <summary>
        /// ドラッグ中のエディットモードに切り替える
        ///
        /// 編集画面では、譜面の上でノーツをドラッグしているときと同じモードにする
        /// (ドラッグの途中で、削除・コピー・Undo などの操作が割り込まないように)。
        /// 全画面プレビューでは、もともとそれらの操作を受け付けないモード(Preview)なので切り替えない。
        /// </summary>
        private void EnterDragMode(bool isSpaceNote)
        {
            editModeBeforeDrag = dataGetter.CurrentEditMode.Value;

            var layer = dataGetter.EditNoteType.Value;
            if (layer == EditNoteType.Preview)
            {
                dragEditMode = editModeBeforeDrag;
                return;
            }

            // 頂点レイヤーでは、頂点をドラッグしているときのモードにする
            // (頂点のコピー・貼り付け・削除は、このモードの間だけ止まるようになっている)
            if (layer == EditNoteType.Vertices) { dragEditMode = EditMode.VertexMoving; }
            else { dragEditMode = isSpaceNote ? EditMode.SpaceMoving : EditMode.Moving; }

            dataSetter.SetEditMode(dragEditMode);
        }

        /// <summary>
        /// エディットモードを、ドラッグを始める前に戻す (途中でほかの処理が切り替えていたら、そのままにする)
        /// </summary>
        private void LeaveDragMode()
        {
            if (dataGetter.CurrentEditMode.Value == dragEditMode)
            {
                dataSetter.SetEditMode(editModeBeforeDrag);
            }
        }

        #endregion

        #region 頂点・中心点のつまみ

        /// <summary>
        /// つまみの位置を、今の頂点の位置に合わせる。プレビューが出ていないときは隠す
        /// </summary>
        private void RefreshVertexHandles()
        {
            SearchSceneOnce();

            PreviewView activeView = null;
            if (previewGenerator != null &&
                dataGetter.ChartData.Value != null &&
                !dataGetter.IsLoadingChart.Value &&
                !dataGetter.CurrentEditMode.Value.IsInEditModeList(handleHiddenEditModes))
            {
                foreach (var view in previewViews)
                {
                    if (!view.IsActive) { continue; }

                    activeView = view;
                    break;
                }
            }

            if (activeView == null)
            {
                vertexHandles.Hide();
                return;
            }

            // 頂点レイヤーを開いている間は、そこで編集中のノーツにもつまみを出す
            bool includesEditingNote = dataGetter.EditNoteType.Value == EditNoteType.Vertices;
            vertexHandles.Refresh(activeView, previewGenerator.NoteParent, TryGetVisibleNoteDistance, includesEditingNote);
        }

        /// <summary>
        /// プレビューに表示されているノーツの、プレビュー上の距離を求める
        /// プレビューに表示されていない(再生位置から離れている)ノーツでは false を返す
        /// (そういうノーツには、つまみを出さない。範囲選択でも選ばない)
        /// </summary>
        private bool TryGetVisibleNoteDistance(IDeployableNoteData note, out float distance)
        {
            if (!TryGetNoteDistance(note, out distance)) { return false; }

            return previewGenerator.IsInVisibleRange(distance);
        }

        /// <summary>
        /// ノーツの、プレビュー上の距離を求める
        /// </summary>
        private bool TryGetNoteDistance(IDeployableNoteData note, out float distance)
        {
            distance = 0f;
            if (note == null || note.Address == null) { return false; }
            if (!EnsureSubDivisionDistances()) { return false; }

            int subIndex = GetSubIndex(note.Address.BarIndex, note.Address.SubDivisionIndex);
            if (subIndex < 0 || subIndex >= subDivisionDistances.Count) { return false; }

            distance = subDivisionDistances[subIndex];
            return true;
        }

        /// <summary>
        /// つまみのドラッグの開始
        /// </summary>
        private bool BeginHandleDrag()
        {
            if (!vertexHandles.IsGrabbing) { return false; }

            EnterDragMode(true);
            isDragging = true;
            return true;
        }

        /// <summary>
        /// つまみのドラッグ中。カーソルの指す位置へ頂点を動かす
        /// </summary>
        private void UpdateHandleDrag()
        {
            // カーソルが指している位置 (プレビューの外に出ても、押したプレビューを基準に求める)
            if (pressedView == null || !pressedView.TryGetRay(Input.mousePosition, false, out Ray ray)) { return; }
            if (!vertexHandles.Drag(ray, previewGenerator.NoteParent)) { return; }

            // 動かした結果を、すぐプレビューにも反映する
            if (previewRefresher != null) { previewRefresher.RefreshImmediately(); }
        }

        #endregion

        #region ノーツを動かす

        /// <summary>
        /// 離したとき
        /// </summary>
        private void Release()
        {
            if (isRangePress)
            {
                if (isRangeSelecting) { FinishRangeSelection(); }
                // ノーツの無い場所を、動かさずに離したとき: 選択を解除する
                else if (isRangeClearOnClick && !ModifierKey.Ctrl && CanOperate()) { notesSetter.ClearSelectingNotes(); }

                EndRange();
            }
            else if (isDragging) { EndDrag(); }
            // つまみを押しただけで動かさなかったとき
            else if (isHandlePress) { vertexHandles.Release(); }
            // ノーツを、動かさずにクリックしたとき
            else if (isPressing) { ClickPressedNote(); }

            isPressing = false;
            isDragging = false;
        }

        /// <summary>
        /// スペースのノーツを掴む。光線がノーツに当たった点と、その点を通るレールを覚える
        /// </summary>
        private bool TryGrabOnRail(Ray ray, INoteData previewNote)
        {
            Transform noteParent = previewGenerator.NoteParent;
            float curveRadius = optionHolder.NoteCurveRadius.Value;

            if (!previewGenerator.TryGetHitPoint(ray, previewNote, out Vector3 hitPoint)) { return false; }
            if (!PreviewTrackSurface.TryGetSection(hitPoint, noteParent, curveRadius, out grabDistance, out railX, out railY)) { return false; }

            // 視点のほぼ真正面を奥へ伸びるレールは、時刻を動かしても画面の上ではほとんど動かない
            // カーソルの動きから位置を決められないので、その場合は選択だけにする
            if (!PreviewTrackSurface.TryGetSection(ray.origin, noteParent, curveRadius, out _, out float eyeX, out float eyeY)) { return false; }

            float offsetX = railX - eyeX;
            float offsetY = railY - eyeY;
            if (offsetX * offsetX + offsetY * offsetY < RAIL_MIN_OFFSET * RAIL_MIN_OFFSET) { return false; }

            grabLane = 0f;
            railDistance = grabDistance;
            return true;
        }

        /// <summary>
        /// ドラッグの開始。選択中のノーツを動かす対象にして、動かせる範囲を求める
        /// </summary>
        private bool BeginDrag()
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || chartData.BarDatas.Count == 0) { return false; }

            // 押した位置にいちばん近い分線
            // (同じ距離を何度も通る譜面では、押したノーツのある分線に近いものを選ぶ)
            if (!EnsureSubDivisionDistances()) { return false; }

            int pressedSubIndex = pressedNoteAddress != null ? GetSubIndex(pressedNoteAddress.BarIndex, pressedNoteAddress.SubDivisionIndex) : -1;
            grabSubIndex = PreviewPickMath.FindNearestIndex(subDivisionDistances, grabDistance, pressedSubIndex);
            if (grabSubIndex < 0) { return false; }

            pointedSubIndex = grabSubIndex;
            grabLaneIndex = Mathf.Clamp(Mathf.RoundToInt(grabLane), 0, MAX_LANE_INDEX);

            // 譜面の先頭と末尾
            var firstAddress = new AddressInChart(0, 0, 0);
            int lastBarIndex = chartData.BarDatas.Count - 1;
            var lastAddress = new AddressInChart(lastBarIndex, chartData.BarDatas[lastBarIndex].SubDivisionDatas.Count - 1, 0);

            dragTargets.Clear();
            minSubDelta = int.MinValue;
            maxSubDelta = int.MaxValue;
            minLaneDelta = -MAX_LANE_INDEX;
            maxLaneDelta = MAX_LANE_INDEX;

            foreach (var data in notesGetter.SelectingNotes)
            {
                var noteObject = notesGetter.GetNoteObject(data);
                if (noteObject == null) { continue; }

                var target = new DragTarget
                {
                    Data = data,
                    NoteObject = noteObject,
                    OriginAddress = new AddressWithinRange(data.Address),
                };

                // 譜面上でドラッグするときと同じ「移動の開始・移動中・終了」の処理を呼ぶ
                Action onMoveStart;
                if (noteObject.TryGetComponent(out IMovableObject movable))
                {
                    onMoveStart = movable.OnMoveStart;
                    target.OnMove = movable.OnMove;
                    target.OnMoveEnd = movable.OnMoveEnd;
                }
                else if (noteObject.TryGetComponent(out IFreedomMovableObject freedomMovable))
                {
                    target.IsSpaceNote = true;
                    onMoveStart = freedomMovable.OnMoveStart;
                    target.OnMove = freedomMovable.OnMove;
                    target.OnMoveEnd = freedomMovable.OnMoveEnd;
                }
                else
                {
                    continue;
                }

                // 複数のノーツをまとめて動かすとき、どれか1つでも端に着いたらそこで止める
                // (端からはみ出したノーツだけが押し戻されて、並びが崩れるのを防ぐ)
                var origin = new AddressInChart(target.OriginAddress.BarIndex, target.OriginAddress.SubDivisionIndex, 0);
                maxSubDelta = Mathf.Min(maxSubDelta, chartData.GetSubdivisionDelta(lastAddress, origin));
                minSubDelta = Mathf.Max(minSubDelta, chartData.GetSubdivisionDelta(firstAddress, origin));

                if (!target.IsSpaceNote)
                {
                    var lanes = target.OriginAddress.Range;
                    minLaneDelta = Mathf.Max(minLaneDelta, -Mathf.RoundToInt(lanes[0]));
                    maxLaneDelta = Mathf.Min(maxLaneDelta, MAX_LANE_INDEX - Mathf.RoundToInt(lanes[lanes.Count - 1]));
                }

                onMoveStart?.Invoke();
                dragTargets.Add(target);
            }

            if (dragTargets.Count == 0) { return false; }

            EnterDragMode(dragTargets[0].IsSpaceNote);

            currentSubDelta = 0;
            currentLaneDelta = 0;
            isDragging = true;
            return true;
        }

        /// <summary>
        /// ドラッグ中。カーソルが指している分線・レーンと、押した位置との差だけノーツを動かす
        /// </summary>
        private void UpdateDrag()
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null) { return; }

            // カーソルが指している位置 (プレビューの外に出ても、押したプレビューを基準に求める)
            if (pressedView == null || !pressedView.TryGetRay(Input.mousePosition, false, out Ray ray)) { return; }
            if (!EnsureSubDivisionDistances()) { return; }

            Transform noteParent = previewGenerator.NoteParent;
            float curveRadius = optionHolder.NoteCurveRadius.Value;
            float distance;
            float lane = grabLane;
            if (isRailDrag)
            {
                // スペースのノーツ: 掴んだ点を通るレールの上で、カーソルにいちばん近い位置
                // (レールが曲がって奥でもう一度カーソルの近くを通ることがあるので、直前の位置に近い方を選ぶ)
                if (!PreviewTrackSurface.TryGetDistanceOnRail(ray, noteParent, curveRadius, railX, railY, railDistance, out distance)) { return; }
                railDistance = distance;
            }
            else
            {
                // グラウンドのノーツ: ノーツの流れる面の上で、カーソルが指している位置
                if (!PreviewTrackSurface.TryGetPoint(ray, noteParent, curveRadius, out distance, out lane)) { return; }
            }

            // 指している分線 (同じ距離を何度も通る譜面では、直前に指していた分線に近いものを選ぶ)
            int subIndex = PreviewPickMath.FindNearestIndex(subDivisionDistances, distance, pointedSubIndex);
            if (subIndex < 0) { return; }
            pointedSubIndex = subIndex;

            int subDelta = subIndex - grabSubIndex;
            int laneDelta = isRailDrag ? 0 : Mathf.Clamp(Mathf.RoundToInt(lane), 0, MAX_LANE_INDEX) - grabLaneIndex;

            subDelta = Mathf.Clamp(subDelta, minSubDelta, maxSubDelta);
            laneDelta = Mathf.Clamp(laneDelta, minLaneDelta, maxLaneDelta);

            // 指している分線・レーンが変わったときだけ動かす
            if (subDelta == currentSubDelta && laneDelta == currentLaneDelta) { return; }
            currentSubDelta = subDelta;
            currentLaneDelta = laneDelta;

            foreach (var target in dragTargets)
            {
                // ドラッグ中に削除されたノーツには触らない
                if (target.NoteObject == null) { continue; }

                var origin = target.OriginAddress;
                var newAddress = chartData.AddressAddition(
                    new AddressInChart(origin.BarIndex, origin.SubDivisionIndex, origin.Range[0]), subDelta);

                // スペースのノーツは時刻だけ、グラウンドのノーツはレーンも動かす
                if (!target.IsSpaceNote) { newAddress.SetSliderIndex(origin.Range[0] + laneDelta); }

                target.Data.SetAddress(new AddressWithinRange(newAddress, origin.Range.Count));
                target.OnMove?.Invoke();
            }

            // 動かした結果を、すぐプレビューにも反映する
            if (previewRefresher != null) { previewRefresher.RefreshImmediately(); }
        }

        /// <summary>
        /// ドラッグの終了。実際に動いていればUndo履歴に登録する
        /// </summary>
        private void EndDrag()
        {
            // つまみのドラッグ: 頂点の移動をUndo履歴に登録する
            if (isHandlePress)
            {
                LeaveDragMode();
                vertexHandles.Release();
                return;
            }

            var previous = new List<NoteDataToAddress>();
            var current = new List<NoteDataToAddress>();

            foreach (var target in dragTargets)
            {
                // ドラッグ中に削除されたノーツには触らない
                if (target.NoteObject == null) { continue; }

                target.OnMoveEnd?.Invoke();

                previous.Add(new NoteDataToAddress(target.Data, target.OriginAddress));
                current.Add(new NoteDataToAddress(target.Data, target.Data.Address));
            }

            dragTargets.Clear();

            LeaveDragMode();

            // 履歴に登録されると、その通知でプレビューも更新される
            UndoRedo.Notes.NotesMoveRecord.RecordNotesMovingIfChanged(previous, current);
        }

        /// <summary>
        /// すべての分線について、プレビュー上の距離を用意する (結果は subDivisionDistances に、譜面の先頭から順に入る)
        /// 前に求めたときから、プレビューも設定も変わっていなければ、求め直さない
        /// </summary>
        private bool EnsureSubDivisionDistances()
        {
            if (previewGenerator == null) { return false; }

            var positionCalculator = previewGenerator.PositionCalculator;
            var chartData = dataGetter.ChartData.Value;
            if (positionCalculator == null || chartData == null)
            {
                subDivisionDistances.Clear();
                distancesCalculator = null;
                return false;
            }

            float noteSpeed = optionHolder.NoteSpeed.Value;
            float offset = dataGetter.Offset.Value + optionHolder.OffsetMs.Value;

            // 譜面を編集するとプレビューが更新され、位置の計算(PositionCalculator)が新しいものに替わる
            if (ReferenceEquals(distancesCalculator, positionCalculator) &&
                ReferenceEquals(distancesChartData, chartData) &&
                distancesNoteSpeed == noteSpeed &&
                distancesOffset == offset)
            {
                return subDivisionDistances.Count > 0;
            }

            distancesCalculator = positionCalculator;
            distancesChartData = chartData;
            distancesNoteSpeed = noteSpeed;
            distancesOffset = offset;

            subDivisionDistances.Clear();
            var calcTiming = new CalcTimingClass(dataGetter.Offset.Value, optionHolder.OffsetMs.Value);

            foreach (var bar in chartData.BarDatas)
            {
                foreach (var sub in bar.SubDivisionDatas)
                {
                    // プレビューでノーツを置くときと同じ式: 時刻 → 位置 → ノーツ速度を掛けた距離
                    subDivisionDistances.Add(positionCalculator.GetPosition(calcTiming.CurrentTiming) * noteSpeed);

                    calcTiming.AddTiming(bar.BeatUnit.Value, bar.DivisionNum.Value, sub.Bpm.Value);
                }
            }

            return subDivisionDistances.Count > 0;
        }

        /// <summary>
        /// 分線の通し番号 (譜面の先頭から数えて何本目か) を求める
        /// </summary>
        private int GetSubIndex(int barIndex, int subDivisionIndex)
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || barIndex < 0 || barIndex >= chartData.BarDatas.Count) { return -1; }

            int index = 0;
            for (int i = 0; i < barIndex; i++)
            {
                index += chartData.BarDatas[i].SubDivisionDatas.Count;
            }

            return index + subDivisionIndex;
        }

        #endregion

        #region 範囲選択

        /// <summary>
        /// ノーツの無い場所を押したまま。一定以上動かしたら範囲選択を始め、枠を描く
        /// </summary>
        private void HoldRange()
        {
            if (!isRangeSelecting)
            {
                Vector2 moved = (Vector2)Input.mousePosition - pressScreenPosition;
                if (moved.sqrMagnitude < DRAG_START_PIXELS * DRAG_START_PIXELS) { return; }
            }

            // 同じクリックで譜面側の操作が始まっていたり、プレビューが閉じられたりしたら、範囲選択をやめる
            if (!CanOperate() || pressedView == null || !pressedView.IsActive)
            {
                CancelPress();
                return;
            }

            isRangeSelecting = true;
            rangeRect.Show(GetRangeRect());
        }

        /// <summary>
        /// 範囲選択をしていない状態に戻し、枠を消す
        /// </summary>
        private void EndRange()
        {
            isRangePress = false;
            isRangeSelecting = false;
            rangeRect.Hide();
        }

        /// <summary>
        /// 押した位置と今のカーソルの位置を対角とする、画面上の範囲 (プレビューの映像の外にははみ出さない)
        /// </summary>
        private Rect GetRangeRect()
        {
            Vector2 start = pressScreenPosition;
            Vector2 end = Input.mousePosition;

            if (pressedView != null && pressedView.TryGetScreenRect(out Rect viewRect))
            {
                start = ClampToRect(start, viewRect);
                end = ClampToRect(end, viewRect);
            }

            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 ClampToRect(Vector2 point, Rect rect)
        {
            return new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));
        }

        /// <summary>
        /// 範囲選択の終了。枠の中に映っているノーツを選択する
        /// </summary>
        private void FinishRangeSelection()
        {
            if (!CanOperate() || pressedView == null || !pressedView.IsActive) { return; }

            CollectNotesInRect(pressedView, GetRangeRect(), rangeNotes);

            // 選ぶノーツのあるレイヤーを開く (選択の前に行う。切り替えで編集モードが変わるため)
            bool hasGround = false;
            bool hasSpace = false;
            foreach (var note in rangeNotes)
            {
                if (IsSpaceLanes(note.Address.Range)) { hasSpace = true; }
                else { hasGround = true; }
            }

            if (hasGround != hasSpace)
            {
                ShowLayer(hasSpace, pressedView.IsFullScreen);
            }
            else if (hasGround && !pressedView.IsFullScreen && dataGetter.EditNoteType.Value == EditNoteType.Vertices)
            {
                // グラウンドとスペースの両方が入っているときは、今のレイヤーのままにする
                // ただし頂点レイヤーでは選んだノーツを操作できないので、スペースのレイヤーへ出る
                ShowLayer(true, false);
            }

            // Ctrl を押しているときは、今の選択に加える (選択中のノーツは外す)。押していなければ選び直す
            bool isToggle = ModifierKey.Ctrl;
            if (!isToggle) { notesSetter.ClearSelectingNotes(); }

            foreach (var note in rangeNotes)
            {
                if (notesSetter.TryAddSelectingNotes(note)) { continue; }
                if (isToggle) { notesSetter.TryRemoveSelectingNotes(note); }
            }

            rangeNotes.Clear();
        }

        /// <summary>
        /// 画面上の範囲の中に映っている、譜面側のノーツを集める
        /// (ノーツの中心が範囲に入っているものを対象にする。編集画面の範囲選択と同じ考え方)
        /// </summary>
        private void CollectNotesInRect(PreviewView view, Rect screenRect, List<IDeployableNoteData> results)
        {
            results.Clear();
            if (view == null || previewGenerator == null) { return; }
            if (!EnsureSubDivisionDistances()) { return; }

            Transform noteParent = previewGenerator.NoteParent;
            if (noteParent == null) { return; }

            float curveRadius = optionHolder.NoteCurveRadius.Value;

            foreach (var pair in notesGetter.DataToNoteObject)
            {
                var note = pair.Data;

                // プレビューに表示されていないノーツ(再生位置から離れているもの)は選ばない
                if (!TryGetVisibleNoteDistance(note, out float distance)) { continue; }

                Vector3 localPoint = GetNoteCenter(note, distance, curveRadius);
                if (!view.TryGetScreenPoint(noteParent.TransformPoint(localPoint), out Vector2 screenPoint)) { continue; }
                if (!screenRect.Contains(screenPoint)) { continue; }

                results.Add(note);
            }
        }

        /// <summary>
        /// 譜面側のノーツが、プレビューの中で置かれている位置 (ノーツの中心。ノーツの親の座標系)
        /// </summary>
        private static Vector3 GetNoteCenter(IDeployableNoteData note, float distance, float curveRadius)
        {
            var lanes = note.Address.Range;

            // グラウンドのノーツ: ノーツの流れる面の上、またがっているレーンの真ん中
            if (!IsSpaceLanes(lanes))
            {
                float lane = lanes.Count > 0 ? (lanes[0] + lanes[lanes.Count - 1]) * 0.5f : 0f;
                return PreviewTrackSurface.GetLanePoint(distance, lane, curveRadius);
            }

            // スペースのノーツ: 頂点の平均の位置 (頂点・中心点のつまみの「中心点」と同じ場所)
            Vector2 center = Vector2.zero;
            if (note is IVerticesControlableNoteData verticesNote &&
                verticesNote.SpaceVertices != null &&
                verticesNote.SpaceVertices.Vertices.Count > 0)
            {
                foreach (var vertex in verticesNote.SpaceVertices.Vertices) { center += vertex.Position.Value; }
                center /= verticesNote.SpaceVertices.Vertices.Count;
            }

            return PreviewTrackSurface.GetPoint(
                distance,
                center.x * PreviewTrackSurface.VERTEX_SCALE,
                center.y * PreviewTrackSurface.VERTEX_SCALE,
                curveRadius);
        }

        #endregion

        #region カーソルが指している場所 (貼り付け先)

        bool IPreviewPointer.IsPointerOnPreview
        {
            get
            {
                SearchSceneOnce();
                return TryGetPreviewRay(Input.mousePosition, out _, out _);
            }
        }

        bool IPreviewPointer.TryGetPointedAddress(out AddressInChart address)
        {
            address = null;

            SearchSceneOnce();
            if (previewGenerator == null || dataGetter.ChartData.Value == null) { return false; }
            if (!TryGetPreviewRay(Input.mousePosition, out Ray ray, out _)) { return false; }

            // まだプレビューに反映されていない変更があれば、先に反映する (古い位置の計算を基準にしないように)
            if (previewRefresher != null) { previewRefresher.FlushPendingRefresh(); }

            // カーソルが指している、ノーツの流れる面の上の位置
            if (!PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out float distance, out _)) { return false; }
            if (!EnsureSubDivisionDistances()) { return false; }

            // その位置にいちばん近い分線 (同じ距離を何度も通る譜面では、今の再生位置に近いものを選ぶ)
            int playheadSubIndex = PreviewPickMath.FindNearestIndex(subDivisionDistances, previewGenerator.CurrentDistance, -1);
            int subIndex = PreviewPickMath.FindNearestIndex(subDivisionDistances, distance, playheadSubIndex);

            return TryGetAddress(subIndex, out address);
        }

        /// <summary>
        /// 分線の通し番号から、小節と分線の番号を求める (GetSubIndex の逆)
        /// </summary>
        private bool TryGetAddress(int subIndex, out AddressInChart address)
        {
            address = null;

            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || subIndex < 0) { return false; }

            for (int barIndex = 0; barIndex < chartData.BarDatas.Count; barIndex++)
            {
                int count = chartData.BarDatas[barIndex].SubDivisionDatas.Count;
                if (subIndex < count)
                {
                    address = new AddressInChart(barIndex, subIndex, 0);
                    return true;
                }

                subIndex -= count;
            }

            return false;
        }

        #endregion

        #region プレビュー上の位置 → 光線

        /// <summary>
        /// プレビューの生成・更新の処理と、プレビューを映している画面部品・カメラを探す (最初の1回だけ)
        /// </summary>
        private void SearchSceneOnce()
        {
            if (hasSearchedScene) { return; }
            hasSearchedScene = true;

            previewGenerator = UnityEngine.Object.FindObjectOfType<ChartPreviewGenerator>(true);
            previewRefresher = UnityEngine.Object.FindObjectOfType<ChartPreviewRefresher>(true);
            previewUIPresenter = UnityEngine.Object.FindObjectOfType<PreviewUIPresenter>(true);

            // ミニプレビュー: 映像を画面部品に映しているカメラ = 撮影先のテクスチャが、画面部品のテクスチャと同じカメラ
            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            foreach (var image in UnityEngine.Object.FindObjectsOfType<RawImage>(true))
            {
                if (image.texture is not RenderTexture renderTexture) { continue; }

                foreach (var camera in cameras)
                {
                    if (camera.targetTexture == renderTexture) { previewViews.Add(new PreviewView(image, camera)); }
                }
            }

            // 全画面プレビュー: 画面に直接映しているカメラ
            var cameraController = UnityEngine.Object.FindObjectOfType<PreviewCameraController>(true);
            Camera screenCamera = cameraController != null ? cameraController.ScreenCamera : null;
            if (screenCamera != null) { previewViews.Add(new PreviewView(screenCamera)); }
        }

        /// <summary>
        /// 画面上の位置が、いずれかのプレビューの映像の上にあれば、その位置を通るプレビュー世界の光線を返す
        /// </summary>
        private bool TryGetPreviewRay(Vector2 screenPosition, out Ray ray, out PreviewView hitView)
        {
            ray = default;
            hitView = null;

            foreach (var view in previewViews)
            {
                if (!view.IsActive) { continue; }
                if (!view.IsPointerOn(screenPosition, uiRaycastResults)) { continue; }
                if (!view.TryGetRay(screenPosition, true, out ray)) { continue; }

                hitView = view;
                return true;
            }

            return false;
        }

        #endregion

        #region プレビューのノーツ → 譜面のノーツ

        /// <summary>
        /// プレビューのノーツに対応する、譜面側のノーツを探す (結果は foundNotes に入る)
        /// </summary>
        /// <returns>見つかったとき true</returns>
        private bool TryFindEditorNotes(INoteData previewNoteData, out bool isSpaceNote)
        {
            foundNotes.Clear();
            isSpaceNote = false;

            // 譜面エディタ側にも同名のクラスがあるため、本編側のクラスは global:: を付けて指定する
            switch (previewNoteData)
            {
                // ホールドの帯: 始点の時刻とレーンで始点のノーツを探し、つながるノーツをまとめて選ぶ
                case global::NoteData_HoldMesh holdMesh:
                    if (holdMesh.TimeToRanges == null || holdMesh.TimeToRanges.Count == 0) { return false; }
                    return TryFindGroundNote(holdMesh.TimeToRanges[0].Timing, ToLanes(holdMesh.TimeToRanges[0].Range), true);

                // スペースホールドの帯: 接続番号でつながるノーツをまとめて選ぶ
                // (プレビュー側の番号は、譜面側の接続番号をそのまま引き継いでいる)
                case global::NoteData_SpaceHoldMesh spaceHoldMesh:
                    isSpaceNote = true;
                    return TryFindSpaceHoldNotes(spaceHoldMesh.HoldNumber, null);

                // スペースホールドの節: 同じ接続番号のうち、同じ時刻のノーツ
                case global::NoteData_SpaceHoldRelay spaceHoldRelay:
                    isSpaceNote = true;
                    return TryFindSpaceHoldNotes(spaceHoldRelay.HoldNumber, spaceHoldRelay.Timing);

                case global::NoteData_SpaceHoldRelayHidden spaceHoldRelayHidden:
                    isSpaceNote = true;
                    return TryFindSpaceHoldNotes(spaceHoldRelayHidden.HoldNumber, spaceHoldRelayHidden.Timing);

                case global::NoteData_SpaceBreak spaceBreak:
                    isSpaceNote = true;
                    return TryFindSpaceBreakNote(spaceBreak.Timing);

                // そのほかのノーツ: 時刻とレーンが一致するもの
                default:
                    var lanes = previewNoteData.GetType().GetProperty("Range")?.GetValue(previewNoteData) as int[];
                    if (lanes == null) { return false; }
                    return TryFindGroundNote(previewNoteData.Timing, lanes, false);
            }
        }

        /// <summary>
        /// 時刻とレーンが一致するグラウンドのノーツを探す
        /// </summary>
        /// <param name="isChain">true のとき、見つけたノーツにつながるノーツもまとめて対象にする</param>
        private bool TryFindGroundNote(float timing, int[] lanes, bool isChain)
        {
            var subDivision = FindSubDivisionByTiming(timing);
            if (subDivision == null || lanes == null) { return false; }

            foreach (var note in subDivision.NoteDatas)
            {
                var noteLanes = note.Address.Range;
                if (IsSpaceLanes(noteLanes) || !IsSameLanes(noteLanes, lanes)) { continue; }

                // つながっているノーツをまとめる
                if (isChain && note is IChainNoteData chainNote && chainNote.ChainIndex.Value >= 0)
                {
                    var chainList = notesGetter.GetChainNoteList(chainNote.ChainIndex.Value);
                    if (chainList != null)
                    {
                        foreach (var chained in chainList.ChainNoteList) { foundNotes.Add(chained); }
                    }
                }

                if (foundNotes.Count == 0) { foundNotes.Add(note); }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 接続番号が一致するスペースホールドのノーツを探す
        /// </summary>
        /// <param name="timing">指定したとき、その時刻のノーツだけを対象にする。null のとき、つながるノーツすべて</param>
        private bool TryFindSpaceHoldNotes(int chainIndex, float? timing)
        {
            var chainList = notesGetter.GetChainNoteList(chainIndex);
            if (chainList == null) { return false; }

            SubDivisionDataInBeat subDivision = null;
            if (timing.HasValue)
            {
                subDivision = FindSubDivisionByTiming(timing.Value);
                if (subDivision == null) { return false; }
            }

            foreach (var chained in chainList.ChainNoteList)
            {
                // 同じ番号がグラウンドのホールドに使われている場合(古い譜面)は対象にしない
                if (!IsSpaceLanes(chained.Address.Range)) { continue; }

                if (subDivision != null &&
                    (chained.Address.BarIndex != subDivision.BarData.BarIndex ||
                     chained.Address.SubDivisionIndex != subDivision.SubDivisionIndex)) { continue; }

                foundNotes.Add(chained);
            }

            return foundNotes.Count > 0;
        }

        /// <summary>
        /// 時刻が一致するスペースブレイクのノーツを探す
        /// </summary>
        private bool TryFindSpaceBreakNote(float timing)
        {
            var subDivision = FindSubDivisionByTiming(timing);
            if (subDivision == null) { return false; }

            foreach (var note in subDivision.NoteDatas)
            {
                if (note.NoteType != DeploymentNoteType.SpaceBreak) { continue; }

                foundNotes.Add(note);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 指定した時刻にある分線を探す
        /// プレビュー用のデータを作るときと同じ計算で、各分線の時刻を先頭から求めて照合する
        /// </summary>
        private SubDivisionDataInBeat FindSubDivisionByTiming(float timing)
        {
            var chartData = dataGetter.ChartData.Value;
            var calcTiming = new CalcTimingClass(dataGetter.Offset.Value, optionHolder.OffsetMs.Value);

            SubDivisionDataInBeat nearest = null;
            float nearestError = TIMING_TOLERANCE_SECONDS;

            foreach (var bar in chartData.BarDatas)
            {
                foreach (var sub in bar.SubDivisionDatas)
                {
                    float error = Mathf.Abs(calcTiming.CurrentTiming - timing);
                    if (error < nearestError)
                    {
                        nearest = sub;
                        nearestError = error;
                    }

                    calcTiming.AddTiming(bar.BeatUnit.Value, bar.DivisionNum.Value, sub.Bpm.Value);
                }
            }

            return nearest;
        }

        /// <summary>
        /// 押したノーツを選択する
        /// (Ctrl を押していないとき: 今の選択を解除して選ぶ。押しているとき: 今の選択に加える・選択中なら外す)
        /// </summary>
        /// <param name="isFullScreen">全画面プレビューの上で選んだか</param>
        /// <returns>選択したとき true。選択から外したとき false</returns>
        private bool Select(PressCandidate candidate, bool isFullScreen)
        {
            bool isCtrl = ModifierKey.Ctrl;

            // Ctrl を押しながら選択中のノーツを押したときは、選択から外す (編集画面の複数選択と同じ)
            if (isCtrl && IsAllSelected(candidate.Notes))
            {
                foreach (var note in candidate.Notes)
                {
                    notesSetter.TryRemoveSelectingNotes(note);
                }

                return false;
            }

            // ノーツのあるレイヤーを開く (選択の前に行う。切り替えで編集モードが変わるため)
            ShowLayer(candidate.IsSpaceNote, isFullScreen);

            if (!isCtrl) { notesSetter.ClearSelectingNotes(); }

            foreach (var note in candidate.Notes)
            {
                notesSetter.TryAddSelectingNotes(note);
            }

            return true;
        }

        /// <summary>
        /// 選んだノーツのあるレイヤーを開く
        /// </summary>
        private void ShowLayer(bool isSpaceNote, bool isFullScreen)
        {
            var targetLayer = isSpaceNote ? EditNoteType.Space : EditNoteType.Ground;
            if (isFullScreen)
            {
                // 全画面プレビューは閉じない。編集画面へ戻るときに、ノーツのあるレイヤーが開くようにしておく
                if (previewUIPresenter != null) { previewUIPresenter.SetReturnLayer(targetLayer); }
            }
            else if (dataGetter.EditNoteType.Value != targetLayer)
            {
                dataSetter.SetEditNoteType(targetLayer);
            }
        }

        /// <summary>
        /// 今の選択が、指定したノーツとちょうど同じか (過不足なく同じノーツが選ばれているか)
        /// </summary>
        private bool IsExactSelection(List<IDeployableNoteData> notes)
        {
            return notesGetter.SelectingNotes.Count == notes.Count && IsAllSelected(notes);
        }

        /// <summary>
        /// 指定したノーツが、すべて選択中か
        /// </summary>
        private bool IsAllSelected(List<IDeployableNoteData> notes)
        {
            foreach (var note in notes)
            {
                bool isSelected = false;
                foreach (var selecting in notesGetter.SelectingNotes)
                {
                    if (selecting == note)
                    {
                        isSelected = true;
                        break;
                    }
                }

                if (!isSelected) { return false; }
            }

            return true;
        }

        private static bool IsSpaceLanes(List<float> lanes)
        {
            return lanes.Count > 0 && lanes[0] == SPACE_LANE_INDEX;
        }

        private static int[] ToLanes(float[] range)
        {
            if (range == null) { return null; }

            var lanes = new int[range.Length];
            for (int i = 0; i < range.Length; i++)
            {
                lanes[i] = Mathf.RoundToInt(range[i]);
            }

            return lanes;
        }

        private static bool IsSameLanes(List<float> noteLanes, int[] lanes)
        {
            if (lanes == null || noteLanes.Count != lanes.Length) { return false; }

            for (int i = 0; i < lanes.Length; i++)
            {
                if (Mathf.RoundToInt(noteLanes[i]) != lanes[i]) { return false; }
            }

            return true;
        }

        #endregion
    }
}
