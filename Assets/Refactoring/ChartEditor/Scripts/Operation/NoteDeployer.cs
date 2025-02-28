using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class NoteDeployer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
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
            Bind();
        }

        private void Bind()
        {
            chartEditorDataGetter.CurrentEditMode
                .Subscribe(editMode => ActiveNote(editMode == EditMode.deploy))
                .AddTo(this.gameObject);
        }

        void Update()
        {
            UpdateNotePosition();
            if (Input.GetMouseButtonDown(0)) { DeployNote(); }
        }

        /// <summary>
        /// �z�u���̃m�[�c�̈ʒu���X�V����
        /// </summary>
        private void UpdateNotePosition()
        {
            // �z�u���[�h�łȂ��ۂ͕Ԃ�
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }

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
            // �C���^���N�g�I�u�W�F�N�g�o�Ȃ����null��Ԃ�
            GameObject hitObject = cursorInteracter.Value.GetObjectUnderCursor();
            if (hitObject == null) { return null; }
            if (!hitObject.TryGetComponent(out IDeployableCollider deployable)) { return null; }

            return hitObject.transform;
        }

        /// <summary>
        /// �m�[�c�̔z�u
        /// </summary>
        private void DeployNote()
        {
            // �z�u���[�h�łȂ��ۂ͕Ԃ�
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.deploy) { return; }
            if (GetTransformUnderCursor() == null) { return; }

            if (deployingNote.TryGetComponent(out IDeployableObject deployable))
            {
                deployable.OnDeploy();
            }

            InstantiateNote();
        }

        /// <summary>
        /// �m�[�g�̐���
        /// </summary>
        private void InstantiateNote()
        {
            deployingNote = Instantiate(noteObj);

            if (deployingNote.TryGetComponent(out IDeployableObject deployable))
            {
                deployable.OnInstantiate();
            }
        }

        /// <summary>
        /// �m�[�g��(��)�A�N�e�B�u��
        /// </summary>
        /// <param name="isActive"></param>
        private void ActiveNote(bool isActive)
        {
            if(deployingNote == null) { InstantiateNote(); }
            deployingNote?.SetActive(isActive);
        }
    }
}

