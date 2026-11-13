using System.Collections.Generic;
using ChartConvert;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面のミニプレビューに映っているノーツをクリックすると、譜面側の対応するノーツを選択する
    ///
    /// プレビューのノーツと譜面のノーツは番号などで結び付けていないので、
    /// 「時刻」と「レーン」(スペースホールドは接続番号)が一致する譜面側のノーツを探して対応を取る。
    /// ホールドの帯をクリックしたときは、そのホールドにつながるノーツをまとめて選択する。
    /// </summary>
    public class PreviewNotePicker : ITickable
    {
        // 時刻の一致とみなす誤差(秒)
        const float TIMING_TOLERANCE_SECONDS = 0.002f;
        // スペースレイヤーのノーツを表すレーン番号
        const float SPACE_LANE_INDEX = 100f;

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

        readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        readonly List<INoteData> pickedNotes = new List<INoteData>();
        readonly List<IDeployableNoteData> foundNotes = new List<IDeployableNoteData>();

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
            if (!Input.GetMouseButtonDown(0)) { return; }

            // 編集画面が見えていて、ほかの操作の途中でないときだけ (全画面プレビュー中は対象外)
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return; }
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }
            if (dataGetter.ChartData.Value == null) { return; }

            SearchSceneOnce();
            if (previewGenerator == null) { return; }

            // クリック位置をプレビューの映像上の光線に変換し、当たったノーツを手前から順に調べる
            if (!TryGetPreviewRay(Input.mousePosition, out Ray ray)) { return; }

            previewGenerator.PickNotes(ray, pickedNotes);
            foreach (INoteData previewNoteData in pickedNotes)
            {
                // 譜面側に対応するノーツが無いもの(ホールド途中の判定点など)は飛ばして、次の候補を調べる
                if (!TryFindEditorNotes(previewNoteData, out bool isSpaceNote)) { continue; }

                Select(isSpaceNote);
                break;
            }

            pickedNotes.Clear();
            foundNotes.Clear();
        }

        /// <summary>
        /// プレビューの生成処理と、プレビューを映している画面部品・カメラを探す (最初の1回だけ)
        /// </summary>
        private void SearchSceneOnce()
        {
            if (hasSearchedScene) { return; }
            hasSearchedScene = true;

            previewGenerator = Object.FindObjectOfType<ChartPreviewGenerator>(true);

            // 映像を画面部品に映しているカメラ = 撮影先のテクスチャが、画面部品のテクスチャと同じカメラ
            var cameras = Object.FindObjectsOfType<Camera>(true);
            foreach (var image in Object.FindObjectsOfType<RawImage>(true))
            {
                if (image.texture is not RenderTexture renderTexture) { continue; }

                foreach (var camera in cameras)
                {
                    if (camera.targetTexture == renderTexture) { previewViews.Add((image, camera)); }
                }
            }
        }

        /// <summary>
        /// 画面上の位置が、プレビューの映像の上にあれば、その位置を通るプレビュー世界の光線を返す
        /// </summary>
        private bool TryGetPreviewRay(Vector2 screenPosition, out Ray ray)
        {
            ray = default;

            foreach (var (image, camera) in previewViews)
            {
                if (image == null || camera == null) { continue; }
                if (!image.isActiveAndEnabled || !camera.isActiveAndEnabled) { continue; }
                if (!IsTopmostUI(image, screenPosition)) { continue; }

                // 画面上の位置 → 画面部品の中での位置
                RectTransform rectTransform = image.rectTransform;
                Canvas canvas = image.canvas;
                Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

                if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, uiCamera)) { continue; }
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPosition)) { continue; }

                // 画面部品の中での位置 → 映像の中での位置 (左下が0、右上が1)
                Rect rect = rectTransform.rect;
                if (rect.width <= 0f || rect.height <= 0f) { continue; }

                float u = (localPosition.x - rect.xMin) / rect.width;
                float v = (localPosition.y - rect.yMin) / rect.height;

                Rect uvRect = image.uvRect;
                u = uvRect.x + u * uvRect.width;
                v = uvRect.y + v * uvRect.height;

                ray = camera.ViewportPointToRay(new Vector3(u, v, 0f));
                return true;
            }

            return false;
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

            // Ctrlを押していなければ、今の選択を解除してから選ぶ
            if (!ModifierKey.Ctrl)
            {
                notesSetter.ClearSelectingNotes();
            }

            foreach (var note in foundNotes)
            {
                notesSetter.TryAddSelectingNotes(note);
            }
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
    }
}
