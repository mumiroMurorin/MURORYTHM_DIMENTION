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
    /// 編集画面のミニプレビューに映っているノーツを、クリックで選択し、ドラッグで動かす
    ///
    /// 【選択】プレビューのノーツと譜面のノーツは番号などで結び付けていないので、
    /// 「時刻」と「レーン」(スペースホールドは接続番号)が一致する譜面側のノーツを探して対応を取る。
    /// ホールドの帯をクリックしたときは、そのホールドにつながるノーツをまとめて選択する。
    ///
    /// 【移動】ノーツを押したままカーソルを動かすと、選択中のノーツがカーソルに付いて動く。
    /// カーソルが指している「レーン」と「分線」を PreviewTrackSurface で求め、掴んだ位置との差だけ動かす。
    /// 動かすたびにプレビューへすぐ反映するので、プレビューの中でも動いて見える。
    /// </summary>
    public class PreviewNotePicker : ITickable
    {
        // 時刻の一致とみなす誤差(秒)
        const float TIMING_TOLERANCE_SECONDS = 0.002f;
        // スペースレイヤーのノーツを表すレーン番号
        const float SPACE_LANE_INDEX = 100f;
        // レーン番号の最大値
        const int MAX_LANE_INDEX = 15;
        // 押してからこの距離(ピクセル)以上動かしたら、クリックではなくドラッグとして扱う
        const float DRAG_START_PIXELS = 6f;
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

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;
        readonly INoteSpawnDataOptionGetter optionHolder;

        // プレビューを映している画面部品と、その映像を撮っているカメラの組
        readonly List<(RawImage Image, Camera Camera)> previewViews = new List<(RawImage, Camera)>();
        bool hasSearchedScene;
        ChartPreviewGenerator previewGenerator;
        ChartPreviewRefresher previewRefresher;

        readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        readonly List<INoteData> pickedNotes = new List<INoteData>();
        readonly List<IDeployableNoteData> foundNotes = new List<IDeployableNoteData>();

        // --- ドラッグ ---
        // ノーツを押している最中か (離すまで true)
        bool isPressing;
        bool isDragging;
        Vector2 pressScreenPosition;
        // 押したときに使ったプレビュー (ドラッグ中はカーソルが外に出ても、このプレビューを基準にする)
        (RawImage Image, Camera Camera) pressedView;
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
        // ドラッグ中は小節の数もテンポも変わらないので、ドラッグの開始時に1回だけ求める
        readonly List<float> subDivisionDistances = new List<float>();
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

        #region 押す・動かす・離す

        /// <summary>
        /// 押したとき。カーソル下のノーツを選択し、ドラッグに備えて掴んだ位置を覚える
        /// </summary>
        private void Press()
        {
            // 前のドラッグが終わっていなければ、ここで確定する (ボタンを離した瞬間を取りこぼしたとき)
            if (isDragging) { EndDrag(); }

            isPressing = false;
            isDragging = false;

            // 編集画面が見えていて、ほかの操作の途中でないときだけ (全画面プレビュー中は対象外)
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return; }
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }
            if (dataGetter.ChartData.Value == null) { return; }
            if (dataGetter.IsLoadingChart.Value) { return; }

            SearchSceneOnce();
            if (previewGenerator == null) { return; }

            // クリック位置をプレビューの映像上の光線に変換し、当たったノーツを手前から順に調べる
            if (!TryGetPreviewRay(Input.mousePosition, out Ray ray, out var view)) { return; }

            INoteData pressedPreviewNote = null;
            bool isSpaceNote = false;
            previewGenerator.PickNotes(ray, pickedNotes);
            foreach (INoteData previewNoteData in pickedNotes)
            {
                // 譜面側に対応するノーツが無いもの(ホールド途中の判定点など)は飛ばして、次の候補を調べる
                if (!TryFindEditorNotes(previewNoteData, out isSpaceNote)) { continue; }

                pressedNoteAddress = new AddressInChart(foundNotes[0].Address.BarIndex, foundNotes[0].Address.SubDivisionIndex, 0);
                Select(isSpaceNote);
                pressedPreviewNote = previewNoteData;
                break;
            }

            pickedNotes.Clear();
            foundNotes.Clear();

            if (pressedPreviewNote == null) { return; }

            // ドラッグに備えて、押した位置を覚える。求められなければ選択だけ
            isRailDrag = isSpaceNote;
            if (isRailDrag)
            {
                if (!TryGrabOnRail(ray, pressedPreviewNote)) { return; }
            }
            else
            {
                if (!PreviewTrackSurface.TryGetPoint(ray, previewGenerator.NoteParent, optionHolder.NoteCurveRadius.Value, out grabDistance, out grabLane)) { return; }
            }

            isPressing = true;
            pressScreenPosition = Input.mousePosition;
            pressedView = view;
        }

        /// <summary>
        /// 押したまま。一定以上動かしたらドラッグを始め、カーソルに合わせてノーツを動かす
        /// </summary>
        private void Hold()
        {
            if (!isDragging)
            {
                Vector2 moved = (Vector2)Input.mousePosition - pressScreenPosition;
                if (moved.sqrMagnitude < DRAG_START_PIXELS * DRAG_START_PIXELS) { return; }

                // 同じクリックで譜面側の操作(ノーツの移動など)が始まっていたら、そちらに任せる
                if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes) || dataGetter.IsLoadingChart.Value)
                {
                    isPressing = false;
                    return;
                }

                if (!BeginDrag())
                {
                    isPressing = false;
                    return;
                }
            }

            UpdateDrag();
        }

        /// <summary>
        /// 離したとき
        /// </summary>
        private void Release()
        {
            if (isDragging) { EndDrag(); }

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
            if (!BuildSubDivisionDistances()) { return false; }

            grabSubIndex = PreviewPickMath.FindNearestIndex(subDivisionDistances, grabDistance, GetSubIndex(pressedNoteAddress));
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

            // 譜面の上でノーツをドラッグしているときと同じエディットモードにする
            // (ドラッグの途中で、削除・コピー・Undo などの操作が割り込まないように)
            editModeBeforeDrag = dataGetter.CurrentEditMode.Value;
            dragEditMode = dragTargets[0].IsSpaceNote ? EditMode.SpaceMoving : EditMode.Moving;
            dataSetter.SetEditMode(dragEditMode);

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
            if (!TryGetRayOnView(pressedView, Input.mousePosition, false, out Ray ray)) { return; }

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
            subDivisionDistances.Clear();

            // エディットモードを、ドラッグを始める前に戻す (途中でほかの処理が切り替えていたら、そのままにする)
            if (dataGetter.CurrentEditMode.Value == dragEditMode)
            {
                dataSetter.SetEditMode(editModeBeforeDrag);
            }

            // 履歴に登録されると、その通知でプレビューも更新される
            UndoRedo.Notes.NotesMoveRecord.RecordNotesMovingIfChanged(previous, current);
        }

        /// <summary>
        /// すべての分線について、プレビュー上の距離を求める (結果は subDivisionDistances に、譜面の先頭から順に入る)
        /// </summary>
        private bool BuildSubDivisionDistances()
        {
            subDivisionDistances.Clear();

            var positionCalculator = previewGenerator.PositionCalculator;
            if (positionCalculator == null) { return false; }

            var chartData = dataGetter.ChartData.Value;
            float noteSpeed = optionHolder.NoteSpeed.Value;
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
        private int GetSubIndex(AddressInChart address)
        {
            var chartData = dataGetter.ChartData.Value;
            if (address == null || address.BarIndex < 0 || address.BarIndex >= chartData.BarDatas.Count) { return -1; }

            int index = 0;
            for (int i = 0; i < address.BarIndex; i++)
            {
                index += chartData.BarDatas[i].SubDivisionDatas.Count;
            }

            return index + address.SubDivisionIndex;
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

            // 映像を画面部品に映しているカメラ = 撮影先のテクスチャが、画面部品のテクスチャと同じカメラ
            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            foreach (var image in UnityEngine.Object.FindObjectsOfType<RawImage>(true))
            {
                if (image.texture is not RenderTexture renderTexture) { continue; }

                foreach (var camera in cameras)
                {
                    if (camera.targetTexture == renderTexture) { previewViews.Add((image, camera)); }
                }
            }
        }

        /// <summary>
        /// 画面上の位置が、いずれかのプレビューの映像の上にあれば、その位置を通るプレビュー世界の光線を返す
        /// </summary>
        private bool TryGetPreviewRay(Vector2 screenPosition, out Ray ray, out (RawImage Image, Camera Camera) hitView)
        {
            ray = default;
            hitView = default;

            foreach (var view in previewViews)
            {
                if (view.Image == null || view.Camera == null) { continue; }
                if (!view.Image.isActiveAndEnabled || !view.Camera.isActiveAndEnabled) { continue; }
                if (!IsTopmostUI(view.Image, screenPosition)) { continue; }
                if (!TryGetRayOnView(view, screenPosition, true, out ray)) { continue; }

                hitView = view;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 画面上の位置を、指定したプレビューの映像の中での位置に直し、そこを通る光線を返す
        /// </summary>
        /// <param name="isInsideOnly">true のとき、映像の外の位置なら false を返す</param>
        private static bool TryGetRayOnView((RawImage Image, Camera Camera) view, Vector2 screenPosition, bool isInsideOnly, out Ray ray)
        {
            ray = default;
            if (view.Image == null || view.Camera == null) { return false; }

            // 画面上の位置 → 画面部品の中での位置
            RectTransform rectTransform = view.Image.rectTransform;
            Canvas canvas = view.Image.canvas;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            if (isInsideOnly && !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, uiCamera)) { return false; }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPosition)) { return false; }

            // 画面部品の中での位置 → 映像の中での位置 (左下が0、右上が1)
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) { return false; }

            float u = (localPosition.x - rect.xMin) / rect.width;
            float v = (localPosition.y - rect.yMin) / rect.height;

            Rect uvRect = view.Image.uvRect;
            u = uvRect.x + u * uvRect.width;
            v = uvRect.y + v * uvRect.height;

            ray = view.Camera.ViewportPointToRay(new Vector3(u, v, 0f));
            return true;
        }

        /// <summary>
        /// 指定した位置で、プレビューの映像がいちばん手前の画面部品か
        /// (ボタンやパネルが上に重なっている場所のクリックは、そちらへの操作として扱う)
        /// </summary>
        private bool IsTopmostUI(RawImage image, Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) { return true; }

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerData, uiRaycastResults);

            bool isTopmost = uiRaycastResults.Count > 0 && uiRaycastResults[0].gameObject == image.gameObject;
            uiRaycastResults.Clear();
            return isTopmost;
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
        /// 見つけたノーツ(foundNotes)を選択する
        /// </summary>
        private void Select(bool isSpaceNote)
        {
            // ノーツのあるレイヤーに切り替える (選択の前に行う。切り替えで編集モードが変わるため)
            var targetLayer = isSpaceNote ? EditNoteType.Space : EditNoteType.Ground;
            if (dataGetter.EditNoteType.Value != targetLayer)
            {
                dataSetter.SetEditNoteType(targetLayer);
            }

            if (!ModifierKey.Ctrl)
            {
                // すでに選択中のノーツを押したときは、選択を変えない
                // (複数選択したノーツのうち1つを掴んで、まとめて動かせるように)
                if (IsAllFoundNotesSelected()) { return; }

                // そうでなければ、今の選択を解除してから選ぶ
                notesSetter.ClearSelectingNotes();
            }

            foreach (var note in foundNotes)
            {
                notesSetter.TryAddSelectingNotes(note);
            }
        }

        private bool IsAllFoundNotesSelected()
        {
            foreach (var note in foundNotes)
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
