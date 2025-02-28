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
            // �z�u���[�h�łȂ��ۂ͕Ԃ�
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
        /// �J�[�\���ɏ�����Ă���R���C�_�[��MovableObject��Ԃ�
        /// </summary>
        /// <returns></returns>
        private IMovableObject GetMovableObjectUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // �I�u�W�F�N�g���Ȃ�������null��Ԃ�
            if (!Physics.Raycast(ray, out hit)) { return null; }

            // ��������I�u�W�F�N�g�łȂ����null��Ԃ�
            GameObject hitObject = hit.collider.gameObject;
            if (!hitObject.transform.parent.TryGetComponent(out IMovableObject movable)) { return null; }

            return movable;
        }

        /// <summary>
        /// �J�[�\���ɏ�����Ă���R���C�_�[��Transform��Ԃ�
        /// </summary>
        /// <returns></returns>
        private Transform GetTransformUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // �I�u�W�F�N�g���Ȃ�������null��Ԃ�
            if (!Physics.Raycast(ray, out hit)) { return null; }

            // �C���^���N�g�I�u�W�F�N�g�o�Ȃ����null��Ԃ�
            GameObject hitObject = hit.collider.gameObject;
            if (!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return null; }

            return hitObject.transform;
        }
    }
}
