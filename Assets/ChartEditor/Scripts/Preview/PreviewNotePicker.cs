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
    /// 【ミニプレビュー】選ぶことだけできる (クリック・Ctrl+クリック・範囲選択。押したノーツのレイヤーが開く)。
    /// ノーツを動かす・幅を変える・頂点のつまみ・カーソルの位置への貼り付けは、全画面プレビューだけ
    /// (2026-11-17: ミニプレビューの上で編集する機能はいらない、クリックしたノーツにフォーカスする機能は残す)
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
    /// 【幅】グラウンドのノーツの左右の端を押したままカーソルを動かすと、ノーツの幅が変わる。
    /// 複数選択しているときは、選択中のノーツの同じ側の端が、同じレーン数だけ動く (編集画面で端を掴んだときと同じ)。
    ///
    /// 【頂点】スペースのノーツを選ぶと、頂点と中心点のつまみが出る (PreviewVertexHandles)。
    /// つまみを動かすと、頂点だけ・または形ごと動かせる。
    ///
    /// 【カーソル】動かせる場所にカーソルを置くと4方向の矢印、幅を変えられる場所(ノーツの端)では左右の矢印になる。
    /// ドラッグを始めると、標準のカーソルに戻る (編集画面のレーンの上でノーツを操作するときと同じ)。
    /// 絵は、編集画面でノーツの上にカーソルを置いたときと同じもの (CursorTextureChanger に登録されているもの)。
    /// 全画面プレビューの頂点の道具を選んでいる間は、その道具の絵になる (頂点レイヤーと同じ絵):
    /// 回転・拡大縮小は、頂点の上 (と、回している・拡大縮小している間)、編集は、頂点を置ける所。
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
        // カーソルの下を調べ直す間隔(秒)
        // ・カーソルを動かしている間: 動かすたびに調べると重いので、この間隔を空ける
        // ・カーソルを止めている間: 再生中はノーツの方が動いてくるので、止めていてもこの間隔で調べ直す
        const float HOVER_CHECK_INTERVAL_MOVING_SECONDS = 0.05f;
        const float HOVER_CHECK_INTERVAL_IDLE_SECONDS = 0.2f;

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
            // SpaceHold の3D編集を開いている間 (ミニプレビューは3Dの画面に隠れているのに、つまみだけが手前に出てしまう)
            EditMode.SpaceHoldView,
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
        CursorTextureChanger cursorTextureChanger;

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

        // --- 幅の変更 ---
        // 押したのが、ノーツの端か (そのまま動かすと、動かす代わりに幅が変わる)
        bool isScalePress;
        // 掴んだのが右の端か (false のときは左の端)
        bool isScaleRightEdge;
        // 幅を変えているノーツ
        readonly List<ScaleTarget> scaleTargets = new List<ScaleTarget>();

        // --- カーソルの形 ---
        // 今プレビューの上で出しているカーソル
        // (EditMode.Move = 移動のカーソル、EditMode.Scale = 幅の変更のカーソル、EditMode.None = 指定なし = 標準のカーソル。
        //  頂点の道具では、VerticesRotating = 回転、VerticesScaling = 拡大縮小、VertexEdit = 頂点を置く、のカーソル)
        EditMode previewCursor = EditMode.None;
        // 頂点の道具 (回転・拡大縮小) で押したとき、離すまで出しておくカーソル (道具で押していないときは None)
        EditMode toolPressCursor = EditMode.None;
        // 頂点の道具「編集」で置いて、まだ元に戻す履歴に登録していない頂点 (離したときに、置いた操作として1回ぶん登録する)
        SpaceVertices placedVertices;
        VertexData placedVertex;
        // カーソルの下を最後に調べたときの結果と、そのときのカーソルの位置
        EditMode hoverCursor = EditMode.None;
        Vector2 lastHoverPosition;
        float lastHoverCheckTime = float.NegativeInfinity;
        // カーソルの下に重なっているノーツ (調べている間だけ使う)
        readonly List<PressCandidate> hoverCandidates = new List<PressCandidate>();
        // 使い終わった PressCandidate (カーソルを動かすたびに作り直さないよう、使い回す)
        readonly List<PressCandidate> candidatePool = new List<PressCandidate>();

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

        /// <summary>
        /// ドラッグで幅を変えているノーツ
        /// </summary>
        private class ScaleTarget
        {
            public IDeployableNoteData Data;
            public NoteObject NoteObject;
            public IScalableObject Scalable;
            // 幅を変え始めたときのアドレス
            public AddressWithinRange OriginAddress;
            // 幅を変え始めたときの、左端と右端のレーン番号
            public int OriginFirstLane;
            public int OriginLastLane;
            // 今の、左端と右端のレーン番号
            public int FirstLane;
            public int LastLane;
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

            instance = this;
        }

        // 今動いているもの (シーンに1つ)
        static PreviewNotePicker instance;

        void IDisposable.Dispose()
        {
            if (instance == this) { instance = null; }

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

            // 全画面プレビューの頂点の道具で「編集」を選んでいる間は、頂点を右クリックすると、その頂点を消す
            if (Input.GetMouseButtonDown(1)) { TryRemoveVertexWithVertexTool(); }

            UpdateCursor();
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
            // 置いた頂点を、まだ履歴に登録していなければ、ここで登録する (同じく、離した瞬間を取りこぼしたとき)
            if (placedVertex != null) { ReleaseHandle(); }
            EndRange();

            isPressing = false;
            isDragging = false;
            isKeptPress = false;
            canDragPressedNote = false;
            isScalePress = false;
            toolPressCursor = EditMode.None;
            ReleaseCandidates(pressCandidates);

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

            // 全画面プレビューの頂点の道具 (編集・回転・拡大縮小) を選んでいるときは、その操作を先に試す
            // (Shift = 範囲選択、Ctrl = 複数選択 のときは、ふつうの操作にする)
            if (!isForcedRange && !ModifierKey.Ctrl && TryPressWithVertexTool(view, ray)) { return; }

            // 頂点・中心点のつまみを押したときは、それを掴む (ノーツより手前にあるものとして扱う。つまみは全画面プレビューだけ)
            isHandlePress = !isForcedRange && view.IsFullScreen && vertexHandles.TryGrab(view, Input.mousePosition, ray, previewGenerator.NoteParent);
            if (isHandlePress)
            {
                isPressing = true;
                pressScreenPosition = Input.mousePosition;
                pressedView = view;
                return;
            }

            // 押した場所に重なっているノーツを、選ばれやすい順に集める
            if (!isForcedRange) { CollectCandidates(ray, pressCandidates); }

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
            int selectedIndex = ModifierKey.Ctrl ? -1 : FindSelectedCandidateIndex(pressCandidates);
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

            // ミニプレビューでは、選ぶだけ (ドラッグでは動かさない・幅も変えない)
            if (!view.IsFullScreen)
            {
                isPressing = true;
                pressScreenPosition = Input.mousePosition;
                pressedView = view;
                return;
            }

            // ドラッグに備えて、押した位置を覚える。求められなければ選択だけ (ドラッグでは動かせない)
            isRailDrag = pressed.IsSpaceNote;

            // ノーツの端を押したときは、ドラッグで幅を変える。それ以外の場所は、ドラッグで動かす
            isScalePress = TryGetScaleEdge(pressed, ray, out isScaleRightEdge, out int edgeLaneIndex);
            if (isScalePress)
            {
                // 掴んだ端のあるレーンを基準にする (ドラッグ中、端はカーソルが指しているレーンへ動く)
                grabLaneIndex = edgeLaneIndex;
                canDragPressedNote = true;
            }
            else
            {
                canDragPressedNote = isRailDrag
                    ? TryGrabOnRail(ray, pressed.PreviewNote)
                    : PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out grabDistance, out grabLane);

                // 掴んだ位置を、譜面の先頭からの距離に直しておく (分線の距離と比べるため)
                if (canDragPressedNote) { grabDistance = ToChartDistance(grabDistance); }
            }

            isPressing = true;
            pressScreenPosition = Input.mousePosition;
            pressedView = view;
        }

        /// <summary>
        /// 全画面プレビューの頂点の道具 (PreviewVertexTools) で、押した操作を受け取る
        ///
        /// ・編集: 押した位置 (選択中のノーツのある断面の上) に、頂点を置く。そのまま動かすと、置いた頂点の位置を調整できる
        ///         (頂点・中心点のつまみの上を押したときは、置かずに、ふつうに掴んで動かす)
        /// ・回転 / 拡大縮小: 押した頂点を掴んで、頂点すべてを回す / 拡大・縮小する
        /// 道具を選んでいないとき、頂点を持つノーツを1つだけ選んでいないとき、ミニプレビューでは、何もしない。
        /// </summary>
        /// <returns>道具の操作として受け取ったとき true (ふつうの選択・移動には進まない)</returns>
        private bool TryPressWithVertexTool(PreviewView view, Ray ray)
        {
            if (!view.IsFullScreen) { return false; }

            PreviewVertexTool tool = PreviewVertexTools.Current;
            if (tool == PreviewVertexTool.Move) { return false; }
            if (!PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out IDeployableNoteData note, out SpaceVertices vertices)) { return false; }

            Transform noteParent = previewGenerator.NoteParent;
            Vector2 screenPosition = Input.mousePosition;
            isHandlePress = false;

            switch (tool)
            {
                case PreviewVertexTool.Edit:
                {
                    // つまみの上を押したときは、ふつうに掴んで動かす
                    if (vertexHandles.IsOverHandle(view, screenPosition)) { return false; }
                    // 選択中のノーツが、プレビューに映っているときだけ
                    if (!TryGetVisibleNoteDistance(note, out float distance)) { return false; }
                    if (!vertexHandles.TryGetVertexPoint(ray, noteParent, distance, out Vector2 point)) { return false; }
                    // ノーツの面の外や、ほかの頂点の上には置かない (面の外を押したときは、ふつうのクリックとして扱う)
                    if (!PreviewVertexTools.CanPlaceAt(vertices, point)) { return false; }

                    // 頂点を置くと、頂点の選択は外れる (頂点レイヤーと同じ)
                    vertexHandles.ClearSelection();

                    // 置いて、位置を調整し終えたときに、1回の操作として履歴に登録する (置いた・動かした、の2回に分けない)
                    VertexData added = PreviewVertexTools.AddVertexWithoutRecord(vertices, point);
                    if (previewRefresher != null) { previewRefresher.RefreshImmediately(); }

                    // 置いた頂点を、そのまま掴む (ボタンを離さずに動かすと、位置を調整できる)
                    RefreshVertexHandles();
                    isHandlePress = vertexHandles.TryGrab(view, screenPosition, ray, noteParent);
                    if (isHandlePress)
                    {
                        placedVertices = vertices;
                        placedVertex = added;
                    }
                    else
                    {
                        // 掴めなかったとき: 置いただけで終わり
                        PreviewVertexTools.RecordAddedVertex(vertices, added);
                    }
                    break;
                }
                case PreviewVertexTool.Rotate:
                case PreviewVertexTool.Scale:
                {
                    if (!vertexHandles.TryGrabForTransform(view, screenPosition, ray, noteParent, vertices, tool == PreviewVertexTool.Rotate)) { return false; }

                    isHandlePress = true;
                    // 離すまで、道具のカーソルを出しておく (頂点レイヤーで、回している・拡大縮小している間と同じ)
                    toolPressCursor = GetToolCursor(tool);
                    break;
                }
                default:
                    return false;
            }

            // 掴んだときは、動かせるように、押した位置を覚える
            if (isHandlePress)
            {
                isPressing = true;
                pressScreenPosition = screenPosition;
                pressedView = view;
            }
            return true;
        }

        /// <summary>
        /// 掴んでいるつまみを離す。頂点が動いていれば、元に戻す履歴に登録する
        /// 頂点の道具「編集」で置いた頂点なら、置いた操作として1回ぶん登録する (動かした分は、置いた位置に含める)
        /// </summary>
        private void ReleaseHandle()
        {
            if (placedVertex == null)
            {
                vertexHandles.Release();
                return;
            }

            vertexHandles.ReleaseWithoutRecord();
            PreviewVertexTools.RecordAddedVertex(placedVertices, placedVertex);
            placedVertices = null;
            placedVertex = null;
        }

        /// <summary>
        /// 頂点のつまみを、動かさずにクリックしたとき: その頂点を選ぶ (Ctrl を押していれば、選択に加える・外す)
        /// 頂点を選べるのは、全画面プレビューで、頂点を持つノーツを1つだけ選んでいる間だけ (PreviewVertexHandles が確かめる)
        /// </summary>
        private void ClickVertexHandle()
        {
            if (pressedView == null || !pressedView.IsFullScreen) { return; }

            VertexData vertex = vertexHandles.GrabbedVertex;
            if (vertex == null) { return; }

            if (ModifierKey.Ctrl) { vertexHandles.ToggleSelection(vertexHandles.GrabbedVertices, vertex); }
            else { vertexHandles.SelectSingle(vertexHandles.GrabbedVertices, vertex); }
        }

        /// <summary>
        /// 全画面プレビューの頂点の道具で「編集」を選んでいる間に、頂点を右クリックしたとき: その頂点を消す
        /// </summary>
        private void TryRemoveVertexWithVertexTool()
        {
            if (PreviewVertexTools.Current != PreviewVertexTool.Edit) { return; }
            // ほかの操作の途中 (左ボタンで何かを掴んでいる間など) は、受け付けない
            if (isPressing || !CanOperate()) { return; }

            SearchSceneOnce();
            if (previewGenerator == null) { return; }
            if (!TryGetPreviewRay(Input.mousePosition, out _, out var view) || !view.IsFullScreen) { return; }
            if (!PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out _, out SpaceVertices vertices)) { return; }

            // 選択中のノーツの、頂点のつまみの上だけ
            if (!vertexHandles.TryFindVertex(view, Input.mousePosition, out SpaceVertices owner, out VertexData vertex)) { return; }
            if (!ReferenceEquals(owner, vertices)) { return; }

            if (PreviewVertexTools.RemoveVertex(vertices, vertex))
            {
                if (previewRefresher != null) { previewRefresher.RefreshImmediately(); }
            }
            else
            {
                Debug.Log($"【頂点】これ以上、頂点を消せません。形を作るには、頂点が {PreviewVertexTools.MIN_VERTEX_COUNT} つ以上必要です");
            }
        }

        /// <summary>
        /// 光線の当たる場所に重なっているノーツを、選ばれやすい順に集める (結果は results に入る)
        /// </summary>
        private void CollectCandidates(Ray ray, List<PressCandidate> results)
        {
            ReleaseCandidates(results);

            previewGenerator.PickNotes(ray, pickedNotes);
            foreach (INoteData previewNoteData in pickedNotes)
            {
                if (results.Count >= MAX_PRESS_CANDIDATES) { break; }

                // 譜面側に対応するノーツが無いもの(ホールド途中の判定点など)は飛ばす
                if (!TryFindEditorNotes(previewNoteData, out bool isSpaceNote)) { continue; }

                // 同じノーツに対応するものが、すでに挙がっているときは加えない
                if (HasCandidateOf(results, foundNotes)) { continue; }

                PressCandidate candidate = RentCandidate();
                candidate.PreviewNote = previewNoteData;
                candidate.IsSpaceNote = isSpaceNote;
                candidate.Notes.AddRange(foundNotes);
                results.Add(candidate);
            }

            pickedNotes.Clear();
            foundNotes.Clear();
        }

        /// <summary>
        /// 中身が空の PressCandidate を用意する (使い終わったものがあれば、それを使い回す)
        /// </summary>
        private PressCandidate RentCandidate()
        {
            if (candidatePool.Count == 0) { return new PressCandidate(); }

            PressCandidate candidate = candidatePool[candidatePool.Count - 1];
            candidatePool.RemoveAt(candidatePool.Count - 1);
            return candidate;
        }

        /// <summary>
        /// リストを空にして、入っていた PressCandidate を使い回せるようにする
        /// </summary>
        private void ReleaseCandidates(List<PressCandidate> candidates)
        {
            foreach (var candidate in candidates)
            {
                candidate.PreviewNote = null;
                candidate.Notes.Clear();
                candidatePool.Add(candidate);
            }

            candidates.Clear();
        }

        private static bool HasCandidateOf(List<PressCandidate> candidates, List<IDeployableNoteData> notes)
        {
            foreach (var candidate in candidates)
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
        /// その場所にあるノーツのうち、すでに選択中のもの(のうち、いちばん選ばれやすいもの)の番号を返す。無ければ -1
        /// </summary>
        private int FindSelectedCandidateIndex(List<PressCandidate> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (IsAllSelected(candidates[i].Notes)) { return i; }
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

                bool isStarted = isHandlePress ? BeginHandleDrag() : (isScalePress ? BeginScale() : BeginDrag());
                if (!isStarted)
                {
                    CancelPress();
                    return;
                }
            }

            if (isHandlePress) { UpdateHandleDrag(); }
            else if (isScalePress) { UpdateScale(); }
            else { UpdateDrag(); }
        }

        /// <summary>
        /// 押している状態をやめる (ドラッグを始められなかったとき)
        /// </summary>
        private void CancelPress()
        {
            if (isHandlePress) { ReleaseHandle(); }
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
            // 画面上のメニューを開いている間は、メニューの裏のプレビューを操作しない
            if (EditorMenuBar.IsBlockingChartInput) { return false; }

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
        /// <param name="editorDragMode">編集画面で同じ操作をしているときのモード (Moving / SpaceMoving / Scaling)</param>
        private void EnterDragMode(EditMode editorDragMode)
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
            dragEditMode = layer == EditNoteType.Vertices ? EditMode.VertexMoving : editorDragMode;

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

            // (ミニプレビューには、つまみを出さない。動かせるのは全画面プレビューだけ)
            if (activeView == null || !activeView.IsFullScreen)
            {
                vertexHandles.SetSelectionOwner(null);
                vertexHandles.Hide();
                return;
            }

            // 頂点を選べるのは、全画面プレビューで、頂点を持つノーツを1つだけ選んでいる間だけ (そのノーツの頂点)
            SpaceVertices selectableVertices = null;
            if (activeView.IsFullScreen) { PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out _, out selectableVertices); }
            vertexHandles.SetSelectionOwner(selectableVertices);

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

            EnterDragMode(EditMode.SpaceMoving);
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
                // (頂点を選んでいるときは、まず頂点の選択だけを外す。ノーツの選択は、もう一度クリックすると外れる)
                else if (isRangeClearOnClick && !ModifierKey.Ctrl && CanOperate())
                {
                    if (vertexHandles.SelectedCount > 0) { vertexHandles.ClearSelection(); }
                    else { notesSetter.ClearSelectingNotes(); }
                }

                EndRange();
            }
            else if (isDragging) { EndDrag(); }
            // つまみを押しただけで動かさなかったとき: 頂点のつまみなら、その頂点を選ぶ
            // (頂点の道具「編集」で、頂点を置いただけのときは選ばない。置くと、頂点の選択は外れる)
            else if (isHandlePress)
            {
                bool isPlaced = placedVertex != null;
                ReleaseHandle();
                if (!isPlaced) { ClickVertexHandle(); }
            }
            // ノーツを、動かさずにクリックしたとき
            else if (isPressing) { ClickPressedNote(); }

            isPressing = false;
            isDragging = false;

            // カーソルの形を、今カーソルの下にあるものに合わせ直す
            lastHoverCheckTime = float.NegativeInfinity;
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
        /// 押したノーツ (その場所に重なっているもののうち、押したもの) が、すべてロックしたノーツか
        /// </summary>
        private bool IsPressedNoteLocked()
        {
            if (!NoteLock.HasAny || pressedCandidateIndex < 0 || pressedCandidateIndex >= pressCandidates.Count) { return false; }

            foreach (var note in pressCandidates[pressedCandidateIndex].Notes)
            {
                if (!NoteLock.IsLocked(note)) { return false; }
            }
            return true;
        }

        /// <summary>
        /// ドラッグの開始。選択中のノーツを動かす対象にして、動かせる範囲を求める
        /// </summary>
        private bool BeginDrag()
        {
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || chartData.BarDatas.Count == 0) { return false; }
            // ロックしたノーツを押したときは、動かさない (選ぶことだけできる)
            if (IsPressedNoteLocked()) { return false; }

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
                // ロックしたノーツは動かさない
                if (NoteLock.IsLocked(data)) { continue; }
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

            EnterDragMode(dragTargets[0].IsSpaceNote ? EditMode.SpaceMoving : EditMode.Moving);

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
            distance = ToChartDistance(distance);
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
                ReleaseHandle();
                return;
            }

            // 幅の変更
            if (isScalePress)
            {
                EndScale();
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
        /// プレビューの中の位置から求めた距離を、譜面の先頭からの距離に直す
        ///
        /// ノーツの流れる軌道は円なので、位置から求めた距離は一周ぶんの範囲に丸まっている。
        /// 分線の距離(譜面の先頭からの距離)と比べる前に、今の再生位置にいちばん近い周回の距離に直す。
        /// (直さないと、譜面の後ろの方で、どこを指しても譜面の先頭の分線を指したことになり、ノーツが前後に動かなくなる)
        /// </summary>
        private float ToChartDistance(float sectionDistance)
        {
            return PreviewPickMath.UnwrapDistance(sectionDistance, previewGenerator.CurrentDistance, optionHolder.NoteCurveRadius.Value);
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

        #region ノーツの幅を変える

        /// <summary>
        /// 光線が、ノーツの左右どちらかの端を指しているかを調べる
        /// (端を掴んでドラッグすると、ノーツの幅が変わる。編集画面で、ノーツの端を掴んだときと同じ)
        /// </summary>
        /// <param name="isRightEdge">右の端のとき true、左の端のとき false</param>
        /// <param name="edgeLaneIndex">その端のあるレーン番号</param>
        private bool TryGetScaleEdge(PressCandidate candidate, Ray ray, out bool isRightEdge, out int edgeLaneIndex)
        {
            isRightEdge = false;
            edgeLaneIndex = 0;

            // 幅を変えられるのは、グラウンドのノーツ1つを指しているときだけ (ホールドの帯と、スペースのノーツは対象外)
            if (candidate.IsSpaceNote || candidate.Notes.Count != 1) { return false; }
            if (candidate.PreviewNote is global::NoteData_HoldMesh) { return false; }

            IDeployableNoteData note = candidate.Notes[0];
            NoteObject noteObject = notesGetter.GetNoteObject(note);
            if (noteObject == null || !noteObject.TryGetComponent(out IScalableObject _)) { return false; }

            var lanes = note.Address.RangeRP;
            if (lanes.Count == 0 || lanes[0] == SPACE_LANE_INDEX) { return false; }

            // カーソルが指しているレーン
            if (!PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out _, out float lane)) { return false; }

            // ノーツの左右の端に近いか (真ん中のときは、幅の変更ではなく移動になる)
            int edge = PreviewPickMath.GetNoteEdge(lane, lanes[0], lanes[lanes.Count - 1]);
            isRightEdge = edge > 0;
            edgeLaneIndex = Mathf.RoundToInt(isRightEdge ? lanes[lanes.Count - 1] : lanes[0]);
            return edge != 0;
        }

        /// <summary>
        /// 幅の変更の開始。選択中のノーツのうち、幅を変えられるものを対象にする
        /// </summary>
        private bool BeginScale()
        {
            scaleTargets.Clear();
            if (IsPressedNoteLocked()) { return false; }

            foreach (var data in notesGetter.SelectingNotes)
            {
                // ロックしたノーツの幅は変えない
                if (NoteLock.IsLocked(data)) { continue; }
                var noteObject = notesGetter.GetNoteObject(data);
                if (noteObject == null) { continue; }
                if (!noteObject.TryGetComponent(out IScalableObject scalable)) { continue; }

                var origin = new AddressWithinRange(data.Address);
                var lanes = origin.RangeRP;
                if (lanes.Count == 0 || lanes[0] == SPACE_LANE_INDEX) { continue; }

                var target = new ScaleTarget
                {
                    Data = data,
                    NoteObject = noteObject,
                    Scalable = scalable,
                    OriginAddress = origin,
                    OriginFirstLane = Mathf.RoundToInt(lanes[0]),
                    OriginLastLane = Mathf.RoundToInt(lanes[lanes.Count - 1]),
                };
                target.FirstLane = target.OriginFirstLane;
                target.LastLane = target.OriginLastLane;

                // 譜面上で端を掴んだときと同じ「幅の変更の開始」の処理を呼ぶ
                scalable.OnStartScale();
                scaleTargets.Add(target);
            }

            if (scaleTargets.Count == 0) { return false; }

            EnterDragMode(EditMode.Scaling);

            currentLaneDelta = 0;
            isDragging = true;
            return true;
        }

        /// <summary>
        /// 幅の変更中。掴んだ端を、カーソルが指しているレーンまで動かす
        /// </summary>
        private void UpdateScale()
        {
            // カーソルが指している位置 (プレビューの外に出ても、押したプレビューを基準に求める)
            if (pressedView == null || !pressedView.TryGetRay(Input.mousePosition, false, out Ray ray)) { return; }
            if (!PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out _, out float lane)) { return; }

            // 掴んだ端のあったレーンから、カーソルが指しているレーンまで、何レーンあるか
            int laneDelta = Mathf.Clamp(Mathf.RoundToInt(lane), 0, MAX_LANE_INDEX) - grabLaneIndex;
            if (laneDelta == currentLaneDelta) { return; }
            currentLaneDelta = laneDelta;

            bool isChanged = false;
            foreach (var target in scaleTargets)
            {
                // ドラッグ中に削除されたノーツには触らない
                if (target.NoteObject == null) { continue; }

                // 掴んだ側の端だけを動かす。反対の端より内側へは行かず(幅は1レーン以上)、レーンの外へも出ない
                PreviewPickMath.MoveNoteEdge(
                    target.OriginFirstLane, target.OriginLastLane, laneDelta, isScaleRightEdge, MAX_LANE_INDEX,
                    out int firstLane, out int lastLane);

                if (firstLane == target.FirstLane && lastLane == target.LastLane) { continue; }
                target.FirstLane = firstLane;
                target.LastLane = lastLane;

                var lanes = new List<float>(lastLane - firstLane + 1);
                for (int i = firstLane; i <= lastLane; i++) { lanes.Add(i); }

                target.Data.SetAddress(new AddressWithinRange(target.OriginAddress.BarIndex, target.OriginAddress.SubDivisionIndex, lanes));
                target.Scalable.OnScale();
                isChanged = true;
            }

            // 変えた結果を、すぐプレビューにも反映する
            if (isChanged && previewRefresher != null) { previewRefresher.RefreshImmediately(); }
        }

        /// <summary>
        /// 幅の変更の終了。実際に幅が変わっていればUndo履歴に登録する
        /// </summary>
        private void EndScale()
        {
            var previous = new List<NoteDataToAddress>();
            var current = new List<NoteDataToAddress>();

            foreach (var target in scaleTargets)
            {
                // ドラッグ中に削除されたノーツには触らない
                if (target.NoteObject == null) { continue; }

                target.Scalable.OnFinishScale();

                previous.Add(new NoteDataToAddress(target.Data, target.OriginAddress));
                current.Add(new NoteDataToAddress(target.Data, target.Data.Address));
            }

            scaleTargets.Clear();

            LeaveDragMode();

            // 履歴に登録されると、その通知でプレビューも更新される
            UndoRedo.Notes.NotesMoveRecord.RecordNotesMovingIfChanged(previous, current);

            // 選択中の表示を付け直す (譜面上で幅を変え終えたときと同じ)
            notesSetter.ReselectNotes();
        }

        #endregion

        #region カーソルの形

        /// <summary>
        /// プレビューの上でのカーソルの形を、今の操作やカーソルの下にあるものに合わせる (毎フレーム呼ぶ)
        /// </summary>
        private void UpdateCursor()
        {
            EditMode cursor;
            if (isDragging)
            {
                // ドラッグ中: 標準のカーソルに戻す (編集画面のレーンの上でノーツをドラッグしているときと同じ)
                // (頂点の道具で回している・拡大縮小している間は、道具のカーソルのまま。頂点レイヤーと同じ)
                cursor = toolPressCursor;
            }
            else if (isPressing)
            {
                // 押してから動かし始めるまで: 動かしたら始まる操作のカーソル。範囲選択は標準のカーソル
                if (isRangePress) { cursor = EditMode.None; }
                // ミニプレビューでは、押しても動かないので、標準のカーソル
                else if (pressedView != null && !pressedView.IsFullScreen) { cursor = EditMode.None; }
                else if (toolPressCursor != EditMode.None) { cursor = toolPressCursor; }
                else { cursor = isScalePress ? EditMode.Scale : EditMode.Move; }
            }
            else
            {
                cursor = GetHoverCursor();
            }

            if (cursor == previewCursor) { return; }
            previewCursor = cursor;

            SearchSceneOnce();
            if (cursorTextureChanger != null) { cursorTextureChanger.SetPreviewCursor(cursor); }
        }

        /// <summary>
        /// 押していないときのカーソルの形 (カーソルの下にあるものに合わせる)
        /// 毎フレームは調べない。一定の間隔を空けて調べ直し、その間は、前に調べた結果を返す
        /// </summary>
        private EditMode GetHoverCursor()
        {
            Vector2 position = Input.mousePosition;
            float now = Time.unscaledTime;

            float interval = position == lastHoverPosition ? HOVER_CHECK_INTERVAL_IDLE_SECONDS : HOVER_CHECK_INTERVAL_MOVING_SECONDS;
            if (now - lastHoverCheckTime < interval) { return hoverCursor; }

            lastHoverPosition = position;
            lastHoverCheckTime = now;

            hoverCursor = FindHoverCursor(position);
            return hoverCursor;
        }

        /// <summary>
        /// 画面上の位置を押したら何が始まるかを調べて、それに合うカーソルを返す
        /// (つまみ・ノーツの真ん中 = 移動、ノーツの端 = 幅の変更、何もない場所・プレビューの外 = 指定なし)
        /// </summary>
        private EditMode FindHoverCursor(Vector2 screenPosition)
        {
            if (!CanOperate()) { return EditMode.None; }

            SearchSceneOnce();
            if (previewGenerator == null) { return EditMode.None; }
            if (!TryGetPreviewRay(screenPosition, out Ray ray, out var view)) { return EditMode.None; }

            // Shift を押している間は、どこを押しても範囲選択になる
            if (ModifierKey.Shift) { return EditMode.None; }
            // ミニプレビューでは、動かせないので、標準のカーソル
            if (!view.IsFullScreen) { return EditMode.None; }

            // 全画面プレビューの頂点の道具を選んでいるとき: 押したら道具の操作になる所では、道具のカーソル (Press と同じ決め方)
            if (!ModifierKey.Ctrl && TryGetToolHoverCursor(view, ray, screenPosition, out EditMode toolCursor)) { return toolCursor; }

            // 頂点・中心点のつまみ (ノーツより手前にあるものとして扱う)
            if (vertexHandles.IsOverHandle(view, screenPosition)) { return EditMode.Move; }

            CollectCandidates(ray, hoverCandidates);
            if (hoverCandidates.Count == 0) { return EditMode.None; }

            // 押したときに掴むことになるノーツ (Press と同じ決め方)
            int selectedIndex = ModifierKey.Ctrl ? -1 : FindSelectedCandidateIndex(hoverCandidates);
            PressCandidate candidate = hoverCandidates[Mathf.Max(selectedIndex, 0)];

            EditMode cursor = TryGetScaleEdge(candidate, ray, out _, out _) ? EditMode.Scale : EditMode.Move;

            ReleaseCandidates(hoverCandidates);
            return cursor;
        }

        /// <summary>
        /// 全画面プレビューの頂点の道具で、その位置を押したら道具の操作になるなら、その道具のカーソルを返す
        /// (TryPressWithVertexTool と同じ決め方)
        /// ・回転 / 拡大縮小: 選択中のノーツの頂点のつまみの上
        /// ・編集: つまみの上ではなく、頂点を置ける所 (選択中のノーツが映っているとき)
        /// </summary>
        private bool TryGetToolHoverCursor(PreviewView view, Ray ray, Vector2 screenPosition, out EditMode cursor)
        {
            cursor = EditMode.None;
            if (!view.IsFullScreen) { return false; }

            PreviewVertexTool tool = PreviewVertexTools.Current;
            if (tool == PreviewVertexTool.Move) { return false; }
            if (!PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out IDeployableNoteData note, out SpaceVertices vertices)) { return false; }

            switch (tool)
            {
                case PreviewVertexTool.Rotate:
                case PreviewVertexTool.Scale:
                    if (!vertexHandles.TryFindVertex(view, screenPosition, out SpaceVertices owner, out _)) { return false; }
                    if (!ReferenceEquals(owner, vertices)) { return false; }
                    break;
                case PreviewVertexTool.Edit:
                    // つまみの上では、置かずに掴む (移動のカーソルのまま)
                    if (vertexHandles.IsOverHandle(view, screenPosition)) { return false; }
                    if (!TryGetVisibleNoteDistance(note, out float distance)) { return false; }
                    if (!vertexHandles.TryGetVertexPoint(ray, previewGenerator.NoteParent, distance, out _)) { return false; }
                    break;
                default:
                    return false;
            }

            cursor = GetToolCursor(tool);
            return true;
        }

        /// <summary>
        /// 頂点の道具のカーソル (頂点レイヤーで、同じ操作をしているときのエディットモードの絵)
        /// </summary>
        private static EditMode GetToolCursor(PreviewVertexTool tool)
        {
            switch (tool)
            {
                case PreviewVertexTool.Rotate: return EditMode.VerticesRotating;
                case PreviewVertexTool.Scale: return EditMode.VerticesScaling;
                case PreviewVertexTool.Edit: return EditMode.VertexEdit;
                default: return EditMode.None;
            }
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

            // 全画面プレビューで、頂点を持つノーツを1つだけ選んでいる間:
            // ・枠の中に、そのノーツの頂点があれば、頂点を選ぶ (ノーツの選択は変えない)
            // ・頂点も、ほかのノーツも無ければ、何も選び直さない (頂点を囲み損ねただけで、ノーツの選択が外れないように)
            // ・ほかのノーツがあれば、今までどおり、ノーツを選ぶ
            if (pressedView.IsFullScreen &&
                PreviewVertexTools.TryGetTarget(dataGetter, notesGetter, out IDeployableNoteData vertexNote, out _))
            {
                bool isVertexSelected = vertexHandles.SelectInRect(GetRangeRect(), ModifierKey.Ctrl);

                bool hasOtherNote = false;
                foreach (var note in rangeNotes)
                {
                    if (!ReferenceEquals(note, vertexNote)) { hasOtherNote = true; break; }
                }

                if (isVertexSelected || !hasOtherNote)
                {
                    if (!isVertexSelected && !ModifierKey.Ctrl) { vertexHandles.ClearSelection(); }
                    rangeNotes.Clear();
                    return;
                }
            }

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

                // ホールドを1本だけ編集している間は、ほかのノーツを選ばない
                if (HoldIsolation.IsBlocked(note)) { continue; }

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
            // ミニプレビューの上では、貼り付け先にしない (ミニプレビューでは編集しない)
            if (!TryGetPreviewRay(Input.mousePosition, out Ray ray, out var pointedView) || !pointedView.IsFullScreen) { return false; }

            // まだプレビューに反映されていない変更があれば、先に反映する (古い位置の計算を基準にしないように)
            if (previewRefresher != null) { previewRefresher.FlushPendingRefresh(); }

            // カーソルが指している、ノーツの流れる面の上の位置
            if (!PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out float distance, out _)) { return false; }
            if (!EnsureSubDivisionDistances()) { return false; }

            distance = ToChartDistance(distance);

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
            cursorTextureChanger = UnityEngine.Object.FindObjectOfType<CursorTextureChanger>(true);

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
        /// 画面上の位置が、いずれかのプレビューの映像の上にあるか (ミニプレビューと、全画面プレビュー)
        /// プレビューの映像は画面部品だが、その上でのホイールは、譜面の上と同じように、再生位置を動かす
        /// </summary>
        public static bool IsPointerOnPreview(Vector2 screenPosition)
        {
            return instance != null && instance.TryGetPreviewRay(screenPosition, out _, out _);
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
                // 先に、位置が映像の中にあるかを調べる (こちらの方が軽い。毎フレーム呼ばれるので、外にいるときはここで終える)
                if (!view.TryGetRay(screenPosition, true, out Ray viewRay)) { continue; }
                if (!view.IsPointerOn(screenPosition, uiRaycastResults)) { continue; }

                ray = viewRay;
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
            if (!TryFindAllEditorNotes(previewNoteData, out isSpaceNote)) { return false; }

            // ホールドを1本だけ編集している間のほかのノーツは、プレビューからも選ばせない (ロックしたノーツは選べる)
            if (HoldIsolation.IsActive) { foundNotes.RemoveAll(HoldIsolation.IsBlocked); }
            return foundNotes.Count > 0;
        }

        private bool TryFindAllEditorNotes(INoteData previewNoteData, out bool isSpaceNote)
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
