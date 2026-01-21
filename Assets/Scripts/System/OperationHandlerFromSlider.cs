using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;
using NaughtyAttributes;

/// <summary>
/// 操作関係の統括クラス
/// </summary>
public class OperationHandlerFromSlider : MonoBehaviour, IOperationSetter, IOperationGetter
{
    [SerializeField] SerializeInterface<IInputHandler> inputHandler;

    ReactiveCollection<SliderTouchData> sliderTouchDatas = new ReactiveCollection<SliderTouchData>();
    IReadOnlyReactiveCollection<SliderTouchData> IOperationGetter.SliderTouchDatas => sliderTouchDatas;

    void IOperationSetter.SetOperate(SliderTouchData sliderTouchData)
    {
        sliderTouchDatas.Add(sliderTouchData);
        inputHandler?.Value.OnTouchSlider(sliderTouchData.SliderIndices, sliderTouchData.ExecuteAction);
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
    public SliderTouchData(OperationAsset asset, Action callback, SliderCoolDownHandler coolDownHandler = default)
    {
        SetSliderIndices(asset.SliderIndices);
        AddCallback(callback);
        SetImageColor(asset.ThemeColor);
        SetText(asset.Text);
        this.coolDownHandler = coolDownHandler;
    }

    public SliderTouchData(int[] sliderIndices, Action callback, Color imageColor = default, string text = default, SliderCoolDownHandler coolDownHandler = default)
    {
        SetSliderIndices(sliderIndices);
        AddCallback(callback);
        SetImageColor(imageColor);
        SetText(text);
        this.coolDownHandler = coolDownHandler;
    }

    SliderCoolDownHandler coolDownHandler;

    // スライダーインデックス
    ReactiveCollection<int> sliderIndices = new ReactiveCollection<int>();
    public IReadOnlyReactiveCollection<int> SliderIndices => sliderIndices;
    public void SetSliderIndices(int[] indices)
    {
        sliderIndices.Clear();
        foreach(int i in indices)
        {
            sliderIndices.Add(i);
        }
    }

    // タッチされた時の挙動
    ReactiveProperty<Action> callBack = new ReactiveProperty<Action>();
    public IReadOnlyReactiveProperty<Action> Callback => callBack;
    public void AddCallback(Action action)
    {
        callBack.Value += action;
    }
    public void DisposeAction()
    {
        callBack.Value = null;
    }

    // 色
    ReactiveProperty<Color> imageColor = new ReactiveProperty<Color>();
    public IReadOnlyReactiveProperty<Color> ImageColor => imageColor;
    public void SetImageColor(Color color)
    {
        imageColor.Value = color;
    }

    // テキスト
    ReactiveProperty<string> text = new ReactiveProperty<string>();
    public IReadOnlyReactiveProperty<string> Text => text;
    public void SetText(string text)
    {
        this.text.Value = text;
    }

    public void ExecuteAction()
    {
        if (coolDownHandler != null && coolDownHandler.IsWaiting) { return; }

        Callback?.Value?.Invoke();
        coolDownHandler?.ResetCoolTime();
    }
}

/// <summary>
/// SliderTouchDataの共通待ち時間に使う
/// </summary>
public class SliderCoolDownHandler
{
    public SliderCoolDownHandler(float coolDownSeconds)
    {
        this.coolDownSeconds = coolDownSeconds;
    }

    private float coolDownSeconds;

    public bool IsWaiting { get; private set; }

    public void ResetCoolTime()
    {
        IsWaiting = true;
        _ = DelayedExecutor.ExecuteAfterDelay(coolDownSeconds, () => { IsWaiting = false; });
    }
}
