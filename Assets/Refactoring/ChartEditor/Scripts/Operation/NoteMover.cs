using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteMover : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;

        IChartEditorDataGetter chartEditorDataGetter;
        IMovableObject movedNote;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Start()
        {

        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { StartMoveNote(); }
            else if (Input.GetMouseButtonUp(0)) { EndMoveNote(); }
        }

        private void StartMoveNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.move) { return; }

            IMovableObject movableObject = GetMovableObjectUnderCursor();
            if (movableObject == null) { return; }

            movableObject.OnMoveStart();
            movedNote = movableObject;
        }
        
        private void MoveNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.move) { return; }
            if (movedNote == null) { return; }

            Transform interactedTransform = GetTransformUnderCursor();
            if (interactedTransform == null) { return; }
            if (movedNote.gameObject.transform.position == interactedTransform.position) { return; }

            movedNote.gameObject.transform.position = interactedTransform.position;
            movedNote.gameObject.transform.SetParent(interactedTransform);
        }

        private void EndMoveNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.move) { return; }

            movedNote?.OnMoveEnd();
            movedNote = null;
        }

        /// <summary>
        /// カーソルに乗っかているコライダーのMovableObjectを返す
        /// </summary>
        /// <returns></returns>
        private IMovableObject GetMovableObjectUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit)) { return null; }

            // 動かせるオブジェクトでなければnullを返す
            GameObject hitObject = hit.collider.gameObject;
            if (!hitObject.transform.parent.TryGetComponent(out IMovableObject movable)) { return null; }

            return movable;
        }

        /// <summary>
        /// カーソルに乗っかているコライダーのTransformを返す
        /// </summary>
        /// <returns></returns>
        private Transform GetTransformUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // オブジェクトがなかったらnullを返す
            if (!Physics.Raycast(ray, out hit)) { return null; }

            // インタラクトオブジェクト出なければnullを返す
            GameObject hitObject = hit.collider.gameObject;
            if (!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return null; }

            return hitObject.transform;
        }
    }
}
