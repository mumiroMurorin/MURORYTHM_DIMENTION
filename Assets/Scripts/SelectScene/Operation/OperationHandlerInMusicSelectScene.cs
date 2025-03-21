using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Refactoring
{
    /// <summary>
    /// ‘€ìŠÖŒW‚Ì“Š‡ƒNƒ‰ƒX
    /// </summary>
    public class OperationHandlerInMusicSelectScene : MonoBehaviour, IOperationSetter
    {
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        void IOperationSetter.SetOperate(int[] sliderIndex, Action action)
        {
            inputHandler?.Value.OnTouchSlider(sliderIndex, action);
        }

        void IOperationSetter.Dispose()
        {
            inputHandler?.Value.Dispose();
        }
    }

    public interface IOperationSetter
    {
        void SetOperate(int[] sliderIndex, Action action);

        void Dispose();
    }
}