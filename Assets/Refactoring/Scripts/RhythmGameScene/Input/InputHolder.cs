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

        // スライダーからの入力
        ReactiveProperty<bool>[] sliderInput;

        // 空間入力(右手)
        ReactiveDictionary<float, Vector3> rightHandInput = new ReactiveDictionary<float, Vector3>();

        // 空間入力(左手)
        ReactiveDictionary<float, Vector3> leftHandInput = new ReactiveDictionary<float, Vector3>();

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
                    if (rightHandInput.Last().Value == pos) { break; }
                    rightHandInput.Add(time, pos);
                    break;
                case SpaceTrackingTag.LeftHand:
                    if (leftHandInput.Last().Value == pos) { break; }
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
                    return rightHandInput;
                case SpaceTrackingTag.LeftHand:
                    return leftHandInput;
                default:
                    Debug.LogWarning($"【Input】設定されていないタグです: {spaceTrackingTag}");
                    return null;
            }
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
