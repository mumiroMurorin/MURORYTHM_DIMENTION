using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class InputHolder : ISliderInputSetter, ISpaceInputSetter, ISliderInputGetter, ISpaceInputGetter
    {
        const int SLIDER_MAX_COUNT = 16;

        // �X���C�_�[����̓���
        ReactiveProperty<bool>[] sliderInput;

        // ��ԓ���(�E��)
        ReactiveProperty<Vector3> rightHandInput = new ReactiveProperty<Vector3>();

        // ��ԓ���(����)
        ReactiveProperty<Vector3> leftHandInput = new ReactiveProperty<Vector3>();

        // ��ԓ��͒��H
        ReactiveProperty<bool> canGetSpaceInput = new ReactiveProperty<bool>();
        public IReadOnlyReactiveProperty<bool> CanGetSpaceInputReactiveProperty { get { return canGetSpaceInput; } }

        public InputHolder()
        {
            sliderInput = new ReactiveProperty<bool>[SLIDER_MAX_COUNT];
            for (int i = 0; i < sliderInput.Length; i++)
            {
                sliderInput[i] = new ReactiveProperty<bool>();
            }
        }

        /// <summary>
        /// (index)�Ԃ̃X���C�_�[��(isEnable)��Ԃɂ���
        /// </summary>
        /// <param name="index"></param>
        public void SetSliderInput(int index, bool isEnable)
        {
            if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"�yInput�zOut of range: {index}"); return; }
            if (sliderInput[index].Value == isEnable) { return; }

            sliderInput[index].Value = isEnable;
        }

        /// <summary>
        /// (tag)�^�O�̃|�W�V������pos�ɃZ�b�g
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pos"></param>
        public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos)
        {
            if(tag == SpaceTrackingTag.RightHand)
            {
                if (rightHandInput.Value == pos) { return; }
                rightHandInput.Value = pos;
            }
            else
            {
                if (leftHandInput.Value == pos) { return; }
                leftHandInput.Value = pos;
            }
        }

        /// <summary>
        /// �̂̃g���b�L���O���o���Ă��邩�Z�b�g
        /// </summary>
        /// <param name="isGet"></param>
        public void SetCanGetSpaceInput(bool isGet)
        {
            if (canGetSpaceInput.Value == isGet) { return; }
            canGetSpaceInput.Value = isGet;
        }

        /// <summary>
        /// �X���C�_�[����(ReactiveProperty)��Ԃ�
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IReadOnlyReactiveProperty<bool> GetSliderInputReactiveProperty(int index)
        {
            if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"�yInput�zOut of range: {index}"); return null; }

            return sliderInput[index];
        }

        /// <summary>
        /// ��ԓ���(ReactiveProperty)��Ԃ�
        /// </summary>
        /// <param name="spaceTrackingTag"></param>
        /// <returns></returns>
        public IReadOnlyReactiveProperty<Vector3> GetSpaceInputReactiveProperty(SpaceTrackingTag spaceTrackingTag)
        {
            if (spaceTrackingTag == SpaceTrackingTag.RightHand)
            {
                return rightHandInput;
            }
            else
            {
                return leftHandInput;
            }
        }
    }

    public interface ISliderInputSetter
    {
        public void SetSliderInput(int index, bool isEnable);
    }

    public interface ISpaceInputSetter
    {
        public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos);

        public void SetCanGetSpaceInput(bool isGet);
    }
}
