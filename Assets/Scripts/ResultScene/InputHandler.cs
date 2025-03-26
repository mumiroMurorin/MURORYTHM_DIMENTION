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
        [Header("タッチ後のクールタイム")]
        [SerializeField] float invalidSeconds = 0.02f;

        ISliderInputGetter sliderInputGetter;
        CompositeDisposable disposables = new CompositeDisposable();
        float invalidCount = 0f;

        [Inject]
        public void Construct(ISliderInputGetter sliderInputGetter)
        {
            this.sliderInputGetter = sliderInputGetter;
        }

        private void Update()
        {
            // 操作無効時間の更新
            if (invalidSeconds > invalidCount) { invalidCount += Time.deltaTime; }
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
                    // タッチされた時
                    .Where(value => value)
                    // 無効時間を過ぎているとき
                    .Where(_ => invalidSeconds <= invalidCount)
                    // 実行とカウントのリセット
                    .Subscribe(_ => { 
                        callback.Invoke();
                        invalidCount = 0f;
                    })
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