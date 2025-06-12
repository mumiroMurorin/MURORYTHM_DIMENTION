using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteScaler : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        IChartEditorDataGetter chartEditorDataGetter;
        IScalableObject scaledNote;
        AddressInChart scaledAddress;

        bool isRightAnchored;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) { StartScaleNote(); }
            if (Input.GetMouseButton(0)) { ScaleNote(); }
            if (Input.GetMouseButtonUp(0)) { FinishScaleNote(); }
        }

        private void StartScaleNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Scale) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IScalableCollider>();
            if(collider == null) { return; }

            var scalableObject = collider.Note;
            if (scalableObject == null) { return; }

            scaledAddress = null;
            scalableObject.OnStartScale();
            scaledNote = scalableObject;
            isRightAnchored = !collider.IsRightEdge;
        }

        private void ScaleNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Scale) { return; }
            if (scaledNote == null) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IDeployableCollider>();
            if (deployable == null) { return; }

            // アドレスの取得
            AddressInChart address = deployable.Address;
            if (scaledAddress == null) { scaledAddress = new AddressInChart(address); }

            // データの更新
            scaledNote.Note.NoteData.ChangeRange(address.SliderIndex, isRightAnchored);

            // オブジェクトの動作
            scaledNote.OnScale();
        }

        private void FinishScaleNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Scale) { return; }

            scaledNote?.OnFinishScale();
            scaledNote = null;
        }
    }

}