using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace Refactoring
{
    public class InputHolder : ISliderInputSetter, ISpaceInputSetter, ISliderInputGetter, ISpaceInputGetter
    {
        const int SLIDER_MAX_COUNT = 16;
        const int MAX_RECORD_SPACE_INDEX = 60;

        // スライダーからの入力
        ReactiveProperty<bool>[] sliderInput;

        // 空間入力(右手)
        LimitedReactiveDictionary<float, Vector3> rightHandInput = new LimitedReactiveDictionary<float, Vector3>(MAX_RECORD_SPACE_INDEX);

        // 空間入力(左手)
        LimitedReactiveDictionary<float, Vector3> leftHandInput = new LimitedReactiveDictionary<float, Vector3>(MAX_RECORD_SPACE_INDEX);

        // 空間入力中？
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
        /// (index)番のスライダーを(isEnable)状態にする
        /// </summary>
        /// <param name="index"></param>
        public void SetSliderInput(int index, bool isEnable)
        {
            if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"【Input】Out of range: {index}"); return; }
            if (sliderInput[index].Value == isEnable) { return; }

            sliderInput[index].Value = isEnable;
        }

        /// <summary>
        /// (tag)タグのポジションをposにセット
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pos"></param>
        public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos, float time)
        {
            switch (tag)
            {
                case SpaceTrackingTag.RightHand:
                    if (rightHandInput.Dictionary.Count > 0 && 
                        (rightHandInput.Dictionary.Last().Value == pos || rightHandInput.Dictionary.Last().Key == time)) { break; }
                    rightHandInput.Add(time, pos);
                    break;
                case SpaceTrackingTag.LeftHand:
                    if (leftHandInput.Dictionary.Count > 0 &&
                        (leftHandInput.Dictionary.Last().Value == pos || leftHandInput.Dictionary.Last().Key == time)) { break; }
                    leftHandInput.Add(time, pos);
                    break;
                default:
                    Debug.LogWarning($"【Input】設定されていないタグです: {tag}");
                    return;
            }
        }

        /// <summary>
        /// 体のトラッキングが出来ているかセット
        /// </summary>
        /// <param name="isGet"></param>
        public void SetCanGetSpaceInput(bool isGet)
        {
            if (canGetSpaceInput.Value == isGet) { return; }
            canGetSpaceInput.Value = isGet;
        }

        /// <summary>
        /// スライダー入力(ReactiveProperty)を返す
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IReadOnlyReactiveProperty<bool> GetSliderInputReactiveProperty(int index)
        {
            if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"【Input】Out of range: {index}"); return null; }

            return sliderInput[index];
        }

        /// <summary>
        /// 空間入力(ReactiveProperty)を返す
        /// </summary>
        /// <param name="spaceTrackingTag"></param>
        /// <returns></returns>
        public IReadOnlyReactiveDictionary<float,Vector3> GetSpaceInputReactiveDictionary(SpaceTrackingTag spaceTrackingTag)
        {
            switch (spaceTrackingTag)
            {
                case SpaceTrackingTag.RightHand:
                    return rightHandInput.Dictionary;
                case SpaceTrackingTag.LeftHand:
                    return leftHandInput.Dictionary;
                default:
                    Debug.LogWarning($"【Input】設定されていないタグです: {spaceTrackingTag}");
                    return null;
            }
        }

        /// <summary>
        /// 最大差を計算して返す
        /// </summary>
        /// <param name="timeRange"></param>
        /// <returns></returns>
        public Vector3 GetMaxDifference(float timeRange)
        {
            Vector3 rightDiff = MaxDifference(rightHandInput.Dictionary, timeRange);
            Vector3 leftDiff = MaxDifference(leftHandInput.Dictionary, timeRange);

            return new Vector3(
                Mathf.Max(rightDiff.x, leftDiff.x),
                Mathf.Max(rightDiff.y, leftDiff.y),
                Mathf.Max(rightDiff.z, leftDiff.z)
                );
        }

        private Vector3 MaxDifference(IReadOnlyReactiveDictionary<float,Vector3> timeToPosition, float timeRange)
        {
            if (timeToPosition == null || timeToPosition.Count < 2)
            {
                return Vector3.zero;
            }

            var xValues = timeToPosition.Where(v => v.Key > timeRange).Select(v => v.Value.x).ToList();
            var yValues = timeToPosition.Where(v => v.Key > timeRange).Select(v => v.Value.y).ToList();
            var zValues = timeToPosition.Where(v => v.Key > timeRange).Select(v => v.Value.z).ToList();
            float maxDiffX = xValues.Max() - xValues.Min();
            float maxDiffY = yValues.Max() - yValues.Min();
            float maxDiffZ = zValues.Max() - zValues.Min();

            return new Vector3(maxDiffX, maxDiffY, maxDiffZ);
        }
    }

    public interface ISliderInputSetter
    {
        public void SetSliderInput(int index, bool isEnable);
    }

    public interface ISpaceInputSetter
    {
        public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos, float time);

        public void SetCanGetSpaceInput(bool isGet);
    }
}
