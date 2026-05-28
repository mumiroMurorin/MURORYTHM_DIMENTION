using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using VContainer;
using UniRx;

public abstract class InputHandlerForSlider : MonoBehaviour, IInputHandler
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

        EachUpdate();
    }

    protected abstract void EachUpdate();

    void IInputHandler.OnTouchSlider(IReadOnlyReactiveCollection<int> indices, Action callback)
    {
        // 最初だけ明示的に実行
        BindForIndices(indices, callback);

        indices?.ObserveCountChanged()
            .Subscribe(_ => BindForIndices(indices, callback))
            .AddTo(this.gameObject);
    }

    private void BindForIndices(IReadOnlyReactiveCollection<int> indices, Action callback)
    {
        if (disposables == null || disposables.IsDisposed)
        {
            disposables = new CompositeDisposable();
        }

        foreach (int index in indices)
        {
            sliderInputGetter.GetSliderInputReactiveProperty(index)
                // 最初からタッチされているときの誤動作防止
                .Skip(1)
                // タッチされた時
                .Where(value => value)
                // 無効時間を過ぎているとき
                .Where(_ => invalidSeconds <= invalidCount)
                // 実行とカウントのリセット
                .Subscribe(_ =>
                {
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
    void OnTouchSlider(IReadOnlyReactiveCollection<int> indices, Action callback);

    void Dispose();
}
