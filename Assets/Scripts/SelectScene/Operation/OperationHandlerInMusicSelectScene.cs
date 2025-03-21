using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace Refactoring
{
    /// <summary>
    /// 操作関係の統括クラス
    /// </summary>
    public class OperationHandlerInMusicSelectScene : MonoBehaviour, IOperationSetter, IOperationGetter
    {
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        ReactiveCollection<SliderTouchData> sliderTouchDatas = new ReactiveCollection<SliderTouchData>();
        IReadOnlyReactiveCollection<SliderTouchData> IOperationGetter.SliderTouchDatas => sliderTouchDatas;

        void IOperationSetter.SetOperate(SliderTouchData sliderTouchData)
        {
            sliderTouchDatas.Add(sliderTouchData);
            inputHandler?.Value.OnTouchSlider(sliderTouchData.SliderIndices, sliderTouchData.Callback);
        }

        void IOperationSetter.Dispose()
        {
            sliderTouchDatas.Clear();
            inputHandler?.Value.Dispose();
        }
    }

    public interface IOperationSetter
    {
        void SetOperate(SliderTouchData sliderTouchData);

        void Dispose();
    }

    public interface IOperationGetter
    {
        IReadOnlyReactiveCollection<SliderTouchData> SliderTouchDatas { get; }
    }

    /// <summary>
    /// スライダータッチの際のデータ
    /// </summary>
    public class SliderTouchData
    {
        public SliderTouchData(int[] sliderIndices, Action callback, Color imageColor)
        {
            SliderIndices = sliderIndices;
            Callback = callback;
            ImageColor = imageColor;
        }

        public int[] SliderIndices { get; set; }

        public Action Callback { get; set; }

        public Color ImageColor { get; set; }
    }
}