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
        /// �G�f�B�b�g���[�h�̍X�V
        /// </summary>
        private void SetEditorMode()
        {
            EditMode raycastEditMode = GetEditModeUnderCursor();

            if (raycastEditMode == EditMode.none) { return; }

            chartEditorDataSetter.SetEditMode(raycastEditMode);
        }

        /// <summary>
        /// �J�[�\���ɏ�������Ă���R���C�_�[�̃G�f�B�b�g���[�h��Ԃ�
        /// </summary>
        /// <returns></returns>
        private EditMode GetEditModeUnderCursor()
        {
            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // �����Ȃ��Ƃ���none��Ԃ�
            if (!Physics.Raycast(ray, out hit)) { return EditMode.none; }

            GameObject hitObject = hit.collider.gameObject;

            // ������
            if(!hitObject.TryGetComponent(out IInteractableCollider interactable)) { return EditMode.none; }

            return interactable.GetEditMode();
        }
    }

}
