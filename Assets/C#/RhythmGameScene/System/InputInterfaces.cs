using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// スライダーの入力データ
    /// </summary>
    public class SliderInputData : ISliderInputGetter
    {
        const int SLIDER_INPUT_NUM = 16;

        bool[] sliderInput = new bool[SLIDER_INPUT_NUM];

        public void SetSliderInput(int index) 
        {
            if (index > sliderInput.Length) { Debug.LogError($"【System】out of range: {index}"); return; }
            sliderInput[index] = true;
        }

        public bool GetSliderInput(int index)
        {
            if(index > sliderInput.Length) { Debug.LogError($"【System】out of range: {index}"); return false; }
            return sliderInput[index];
        }
    }

    /// <summary>
    /// スライダーからの入力を受け取る
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
    /// スライダーの入力データ
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
    /// スペースの入力を受け取る
    /// </summary>
    public interface ISpaceInputGetter
    {
        Vector3 GetSpaceInput(SpaceTrackingTag tag);
    }

    /// <summary>
    /// スライダーの入力状況を保持、監視
    /// </summary>
    public interface ISliderInputMonitor
    {
        // ※動作次第では重いかも？
        public IReadOnlyReactiveProperty<SliderInputData> InputReactiveProperty { get; }
    }

    /// <summary>
    /// 空間(KINECT)の入力状況を保持、監視
    /// </summary>
    public interface ISpaceInputMonitor
    {
        // ※動作次第では重いかも？
        public IReadOnlyReactiveProperty<SpaceInputData> InputReactiveProperty { get; }
    }
}
