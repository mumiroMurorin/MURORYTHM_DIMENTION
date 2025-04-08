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

            IScalableObject scalableObject = chartEditorDataGetter.ScalableObject.Value;
            if (scalableObject == null) { return; }

            scalableObject.OnStartScale();
            scaledNote = scalableObject;
        }

        private void ScaleNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Scale) { return; }
            if (scaledNote == null) { return; }

            // カーソル下の親取得
            IDeployableCollider deployable = chartEditorDataGetter.DeployableCollider.Value;
            if (deployable == null) { return; }

            scaledNote.OnScale(deployable);
        }

        private void FinishScaleNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Scale) { return; }

            scaledNote?.OnFinishScale();
            scaledNote = null;
        }
    }

}