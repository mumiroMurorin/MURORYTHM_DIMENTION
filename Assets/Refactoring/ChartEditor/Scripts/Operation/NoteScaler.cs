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
            if (Input.GetMouseButtonDown(0)) { ScaleNote(); }
        }

        private void ScaleNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.scale) { return; }

            IScalableObject scalableObject = GetScalableObjectUnderCursor();
            if (scalableObject == null) { return; }

            scalableObject.OnScale();
        }

        /// <summary>
        /// �J�[�\���ɏ�����Ă���R���C�_�[��MovableObject��Ԃ�
        /// </summary>
        /// <returns></returns>
        private IScalableObject GetScalableObjectUnderCursor()
        {
            GameObject hitObject = cursorInteracter.Value.GetObjectUnderCursor();
            if (hitObject == null) { return null; }

            // ��������I�u�W�F�N�g�łȂ����null��Ԃ�
            if (!hitObject.transform.parent.TryGetComponent(out IScalableObject scalable)) { return null; }

            return scalable;
        }
    }

}