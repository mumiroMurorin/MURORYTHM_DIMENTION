using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

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
    public SliderTouchData(int[] sliderIndices, Action callback, Color imageColor = default, string text = default, SliderCoolDownHandler coolDownHandler = default)
    {
        SliderIndices = sliderIndices;
        Callback = callback;
        ImageColor = imageColor;
        Text = text;
        this.coolDownHandler = coolDownHandler;
    }

    SliderCoolDownHandler coolDownHandler;

    public int[] SliderIndices { get; set; }

    public Action Callback { get; set; }

    public Color ImageColor { get; set; }

    public string Text { get; set; }

    public void ExecuteAction()
    {
        if (coolDownHandler != null && coolDownHandler.IsWaiting) { return; }

        Callback.Invoke();
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
