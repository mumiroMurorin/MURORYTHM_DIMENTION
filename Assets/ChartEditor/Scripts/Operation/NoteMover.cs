using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        IChartEditorDataGetter chartEditorDataGetter;
        IMovableObject movedNote;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { StartMoveNote(); }
            else if (Input.GetMouseButton(0)) { MoveNote(); } 
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

            // カーソル下の親取得
            Transform noteParent = GetTransformUnderCursor();
            if (noteParent == null) { return; }
            if (movedNote.gameObject.transform.position == noteParent.position) { return; }
           
            movedNote.OnMove(noteParent);
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
            GameObject hitObject = cursorInteracter.Value.GetObjectUnderCursor();
            if(hitObject == null) { return null; }

            // 動かせるオブジェクトでなければnullを返す
            if (!hitObject.transform.parent.TryGetComponent(out IMovableObject movable)) { return null; }

            return movable;
        }

        /// <summary>
        /// カーソルに乗っかているコライダーのTransformを返す
        /// </summary>
        /// <returns></returns>
        private Transform GetTransformUnderCursor()
        {
            GameObject hitObject = cursorInteracter.Value.GetObjectUnderCursor();
            if (hitObject == null) { return null; }
            if (!hitObject.TryGetComponent(out IDeployableCollider d)) { return null; }

            return hitObject.transform;
        }
    }
}
