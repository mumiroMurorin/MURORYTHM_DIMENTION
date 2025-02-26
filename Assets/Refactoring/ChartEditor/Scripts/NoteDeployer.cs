using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteDeployer : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform noteParent;
        [SerializeField] GameObject noteObj;

        GameObject deployingNote;
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Start()
        {
            InstantiateNote(); // ��
            Bind();
        }

        private void Bind()
        {

        }

        void Update()
        {
            UpdateNotePosition();
            DeployNote();
        }

        /// <summary>
        /// �z�u���̃m�[�c�̈ʒu���X�V����
        /// </summary>
        private void UpdateNotePosition()
        {
            // �z�u���[�h�łȂ��ۂ͕Ԃ�
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.modify) { return; }

            Transform interactedTransform = GetTransformUnderCursor();
            if (interactedTransform == null) { return; }
            if (deployingNote.transform.position == interactedTransform.position)  { return; }

            deployingNote.transform.position = interactedTransform.position;
            deployingNote.transform.SetParent(interactedTransform);
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

        /// <summary>
        /// �m�[�c�̔z�u
        /// </summary>
        private void DeployNote()
        {
            // �z�u���[�h�łȂ��ۂ͕Ԃ�
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.modify) { return; }
            if (!Input.GetMouseButtonDown(0)) { return; }

            InstantiateNote();
        }

        /// <summary>
        /// �m�[�g�̐���
        /// </summary>
        private void InstantiateNote()
        {
            deployingNote = Instantiate(noteObj);
        }
    }
}

