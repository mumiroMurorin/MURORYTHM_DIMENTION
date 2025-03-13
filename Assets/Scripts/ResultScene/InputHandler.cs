using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VContainer;
using UniRx;

namespace Refactoring
{
    public class InputHandler : MonoBehaviour, IInputHandler
    {
        ISliderInputGetter sliderInputGetter;
        CompositeDisposable disposables = new CompositeDisposable();

        [Inject]
        public void Construct(ISliderInputGetter sliderInputGetter)
        {
            this.sliderInputGetter = sliderInputGetter;
        }

        void IInputHandler.OnTouchSlider(int[] indexes, Action callback)
        {
            if(disposables == null || disposables.IsDisposed)
            {
                disposables = new CompositeDisposable();
            }

            foreach(int index in indexes)
            {
                sliderInputGetter.GetSliderInputReactiveProperty(index)
                    .Where(value => value)
                    .Subscribe(_ => callback.Invoke())
                    .AddTo(disposables)
                    .AddTo(this.gameObject);
            }
        }

        void IInputHandler.Dispose()
        {
            disposables.Dispose();
        }
    }

    public interface IInputHandler
    {
        void OnTouchSlider(int[] indexes, Action callback);

        void Dispose();
    }
}