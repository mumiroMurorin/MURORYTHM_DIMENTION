using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        List<IScalableObject> scalableObjects;
        IScalableObject scaledNote;
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
            
            scaledNote = scalableObject;
            isRightAnchored = !collider.IsRightEdge;

            // 複数選択されたオブジェクトから拡大縮小できるやつを取り出す
            foreach (var data in notesSelector.SelectingNotes)
            {
                var obj = notesController.DataToObj.GetObject(data);
                if (!obj.TryGetComponent(out IScalableObject scalable)) { continue; }

                // ノーツオブジェクト側の動作
                scalable.OnStartScale();
                
                // リストに保存
                scalableObjects.Add(scalable);
            }

            dataSetter?.SetEditMode(EditMode.Scaling);
        }

        private void ScaleNote()
        {
            if (scalableObjects == null || scalableObjects.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = dataGetter.GetInteractableCollider<IDeployableCollider>();
            if (deployable == null) { return; }

            // アドレスの取得
            AddressInChart address = deployable.Address;
            if (scaledAddress == null) { scaledAddress = new AddressInChart(address); }

            foreach (var scalable in scalableObjects)
            {
                // データの更新
                scalable.Note.NoteData.ChangeRange(address.SliderIndex - , isRightAnchored);

                // オブジェクトの動作
                scalable.OnScale();
            }
        }

        private void FinishScaleNote()
        {
            if (dataGetter.CurrentEditMode.Value != EditMode.Scale) { return; }

            scaledNote?.OnFinishScale();
            scaledNote = null;
        }

        private void Initialize()
        {
            scaledAddress = null;
            scalableObjects.Clear();
        }
    }

}