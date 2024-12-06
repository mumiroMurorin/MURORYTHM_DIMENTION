using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �X���C�_�[�̓��̓f�[�^
    /// </summary>
    public class SliderInputData : ISliderInputGetter
    {
        const int SLIDER_INPUT_NUM = 16;

        bool[] sliderInput = new bool[SLIDER_INPUT_NUM];

        public void SetSliderInput(int index) 
        {
            if (index > sliderInput.Length) { Debug.LogError($"�ySystem�zout of range: {index}"); return; }
            sliderInput[index] = true;
        }

        public bool GetSliderInput(int index)
        {
            if(index > sliderInput.Length) { Debug.LogError($"�ySystem�zout of range: {index}"); return false; }
            return sliderInput[index];
        }
    }

    /// <summary>
    /// �X���C�_�[����̓��͂��󂯎��
    /// </summary>
    public interface ISliderInputGetter
    {
        bool GetSliderInput(int index);
    }

    public enum SpaceTrackingTag
    {
        RightHand,
        LeftHand,
    }

    /// <summary>
    /// �X���C�_�[�̓��̓f�[�^
    /// </summary>
    public class SpaceInputData : ISpaceInputGetter
    {
        Vector3 rightHandInput;
        Vector3 leftHandInput;

        public void SetSliderInput(SpaceTrackingTag tag, Vector3 pos)
        {
            if (tag == SpaceTrackingTag.RightHand) { rightHandInput = pos; }
            else if (tag == SpaceTrackingTag.LeftHand) { leftHandInput = pos; }
        }

        public Vector3 GetSpaceInput(SpaceTrackingTag tag)
        {
            if (tag == SpaceTrackingTag.RightHand) { return rightHandInput; }
            else if (tag == SpaceTrackingTag.LeftHand) { return leftHandInput; }

            return Vector3.zero;
        }
    }

    /// <summary>
    /// �X�y�[�X�̓��͂��󂯎��
    /// </summary>
    public interface ISpaceInputGetter
    {
        Vector3 GetSpaceInput(SpaceTrackingTag tag);
    }

    /// <summary>
    /// �X���C�_�[�̓��͏󋵂�ێ��A�Ď�
    /// </summary>
    public interface ISliderInputMonitor
    {
        // �����쎟��ł͏d�������H
        public IReadOnlyReactiveProperty<SliderInputData> InputReactiveProperty { get; }
    }

    /// <summary>
    /// ���(KINECT)�̓��͏󋵂�ێ��A�Ď�
    /// </summary>
    public interface ISpaceInputMonitor
    {
        // �����쎟��ł͏d�������H
        public IReadOnlyReactiveProperty<SpaceInputData> InputReactiveProperty { get; }
    }
}
