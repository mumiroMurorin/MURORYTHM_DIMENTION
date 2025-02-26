using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class CursorInteracter : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;

        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            SetEditorMode();
        }

        /// <summary>
        /// エディットモードの更新
        /// </summary>
        private void SetEditorMode()
        {
            EditMode raycastEditMode = GetEditModeUnderCursor();

            if (raycastEditMode == EditMode.none) { return; }

            chartEditorDataSetter.SetEditMode(raycastEditMode);
        }

        /// <summary>
        /// カーソルに乗っかっているコライダーのエディットモードを返す
        /// </summary>
        /// <returns></returns>
        private EditMode GetEditModeUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 何もないときはnoneを返す
            if (!Physics.Raycast(ray, out hit)) { return EditMode.none; }

            GameObject hitObject = hit.collider.gameObject;

            // 同じく
            if(!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return EditMode.none; }

            return interactable.GetEditMode();
        }
    }

}
