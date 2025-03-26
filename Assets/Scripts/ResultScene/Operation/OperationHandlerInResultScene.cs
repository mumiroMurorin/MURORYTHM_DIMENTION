using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace Refactoring
{
    /// <summary>
    /// ‘€ìŠÖŒW‚Ì“Š‡ƒNƒ‰ƒX
    /// </summary>
    public class OperationHandlerInResultScene : MonoBehaviour, IOperationSetter, IOperationGetter
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
}