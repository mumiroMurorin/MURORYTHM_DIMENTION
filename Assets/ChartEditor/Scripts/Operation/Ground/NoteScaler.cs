using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace ChartEditor
{
    public class NoteScaler : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] MultiNoteSelector notesSelector;
        [SerializeField] NoteObjectsController notesController;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        Dictionary<IScalableObject, int> scalableAndDelta = new Dictionary<IScalableObject, int>();
        IScalableObject baseNote;
        AddressInChart scaledAddress;
        bool isRightAnchored;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        private void Update()
        {
            // 配置モードのみ
            if (dataGetter.CurrentEditMode.Value != EditMode.Scale &&
                dataGetter.CurrentEditMode.Value != EditMode.Scaling) { return; }

            if (Input.GetMouseButtonDown(0)) { StartScaleNote(); }
            if (Input.GetMouseButton(0)) { ScaleNote(); }
            if (Input.GetMouseButtonUp(0)) { FinishScaleNote(); }
        }

        /// <summary>
        /// クリック時(拡大縮小開始)
        /// </summary>
        private void StartScaleNote()
        {
            // 拡大縮小できるノーツでなければ返す
            var collider = dataGetter.GetInteractableCollider<IScalableCollider>();
            var scalableObject = collider?.Note;

            if (collider == null) { return; }
            if (scalableObject == null) { return; }

            Initialize();
            
            baseNote = scalableObject;
            isRightAnchored = !collider.IsRightEdge;
            var baseNoteData = baseNote.Note.NoteData;

            // 複数選択されたオブジェクトから拡大縮小できるやつを取り出す
            foreach (var data in notesSelector.SelectingNotes)
            {
                var obj = notesController.DataToObj.GetObject(data);
                if (!obj.TryGetComponent(out IScalableObject scalable)) { continue; }

                // ノーツオブジェクト側の動作
                scalable.OnStartScale();

                // リストに保存
                int delta;
                var address = data.Address;
                if (isRightAnchored) { delta = (int)(address.Range[0] - baseNoteData.Address.Range[0]); }
                else { delta = (int)(address.Range[0] + address.Range.Count - baseNoteData.Address.Range[0] - baseNoteData.Address.Range.Count); }

                scalableAndDelta.TryAdd(scalable, delta);
            }

            dataSetter?.SetEditMode(EditMode.Scaling);
        }

        /// <summary>
        /// クリック中(拡大縮小中)
        /// </summary>
        private void ScaleNote()
        {
            if (scalableAndDelta == null || scalableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = dataGetter.GetInteractableCollider<IDeployableCollider>();
            if (deployable == null) { return; }

            // アドレスの取得
            AddressInChart address = deployable.Address;
            if (scaledAddress == null) { scaledAddress = new AddressInChart(address); }

            foreach (var pair in scalableAndDelta)
            {
                // データの更新
                var noteAddress = pair.Key.Note.NoteData.Address;
                ChangeRange(noteAddress, pair.Value + address.SliderIndex, isRightAnchored);

                // オブジェクトの動作
                pair.Key.OnScale();
            }
        }

        /// <summary>
        /// ノート範囲を変える
        /// </summary>
        /// <param name="address"></param>
        /// <param name="index"></param>
        /// <param name="isRightAnchored"></param>
        public void ChangeRange(AddressWithinRange address, float index, bool isRightAnchored)
        {
            List<float> shifted = new List<float>();
            float min = address.Range.First();
            float max = address.Range.Last();

            // 右固定で左側とindexが一緒のとき返す
            if (isRightAnchored && (int)min == index) { return; }
            // 左固定で右側とindexが一緒のとき返す
            if (!isRightAnchored && (int)max == index) { return; }

            // 右固定で左側に伸ばす
            if (isRightAnchored && index <= max)
            {
                for (float i = index; i <= max; i++) { shifted.Add(i); }
            }
            // 左固定で右側に伸ばす
            else if (!isRightAnchored && index >= min)
            {
                for (float i = min; i <= index; i++) { shifted.Add(i); }
            }
            else
            {
                return;
            }

            address.SetRange(shifted);
            Debug.Log($"【拡大】\n {address.Range.First()} ～ {address.Range.Last()}");
        }

        /// <summary>
        /// クリック終了時(拡大縮小終了)
        /// </summary>
        private void FinishScaleNote()
        {
            foreach (var pair in scalableAndDelta)
            {
                pair.Key?.OnFinishScale();
            }

            Initialize();
        }

        private void Initialize()
        {
            scaledAddress = null;
            scalableAndDelta.Clear();
        }
    }

}