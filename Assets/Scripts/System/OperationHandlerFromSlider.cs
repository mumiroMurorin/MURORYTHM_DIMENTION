using System;
using UniRx;
using UnityEngine;

/// <summary>
/// Central operation handler for slider-touch based inputs.
/// </summary>
public class OperationHandlerFromSlider : MonoBehaviour, IOperationSetter, IOperationGetter
{
    [SerializeField] private SerializeInterface<IInputHandler> inputHandler;

    private readonly ReactiveCollection<SliderTouchData> sliderTouchDatas = new ReactiveCollection<SliderTouchData>();
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
/// Data for a slider touch operation.
/// </summary>
public class SliderTouchData
{
    public SliderTouchData(OperationAssetUnit asset, Action callback, SliderCoolDownHandler coolDownHandler = default)
    {
        SetSliderIndices(asset.SliderIndices);
        AddCallback(callback);
        SetThemeColor(asset.ThemeColor);
        SetControllerColor(asset.ControllerColor);
        SetControllerRainbow(asset.ControllerRainbow);
        SetText(asset.Text);
        this.coolDownHandler = coolDownHandler;
    }

    public SliderTouchData(int[] sliderIndices, Action callback, Color themeColor = default, Color controllerColor = default, bool controllerRainbow = false, string text = default, SliderCoolDownHandler coolDownHandler = default)
    {
        SetSliderIndices(sliderIndices);
        AddCallback(callback);
        SetThemeColor(themeColor);
        SetControllerColor(controllerColor == default ? themeColor : controllerColor);
        SetControllerRainbow(controllerRainbow);
        SetText(text);
        this.coolDownHandler = coolDownHandler;
    }

    private readonly SliderCoolDownHandler coolDownHandler;

    private readonly ReactiveCollection<int> sliderIndices = new ReactiveCollection<int>();
    public IReadOnlyReactiveCollection<int> SliderIndices => sliderIndices;

    public void SetSliderIndices(int[] indices)
    {
        sliderIndices.Clear();
        foreach (int index in indices)
        {
            sliderIndices.Add(index);
        }
    }

    private readonly ReactiveProperty<Action> callBack = new ReactiveProperty<Action>();
    public IReadOnlyReactiveProperty<Action> Callback => callBack;

    public void AddCallback(Action action)
    {
        callBack.Value += action;
    }

    public void DisposeAction()
    {
        callBack.Value = null;
    }


    // ThemeColor
    private readonly ReactiveProperty<Color> themeColor = new ReactiveProperty<Color>();
    public IReadOnlyReactiveProperty<Color> ThemeColor => themeColor;

    public void SetThemeColor(Color color)
    {
        themeColor.Value = color;
    }


    // ControllerColor
    private readonly ReactiveProperty<Color> controllerColor = new ReactiveProperty<Color>();
    public IReadOnlyReactiveProperty<Color> ControllerColor => controllerColor;

    public void SetControllerColor(Color color)
    {
        controllerColor.Value = color;
    }

    private readonly ReactiveProperty<bool> controllerRainbow = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> ControllerRainbow => controllerRainbow;

    public void SetControllerRainbow(bool value)
    {
        controllerRainbow.Value = value;
    }


    // Text
    private readonly ReactiveProperty<string> text = new ReactiveProperty<string>();
    public IReadOnlyReactiveProperty<string> Text => text;

    public void SetText(string value)
    {
        text.Value = value;
    }


    public void ExecuteAction()
    {
        if (coolDownHandler != null && coolDownHandler.IsWaiting)
        {
            return;
        }

        Callback?.Value?.Invoke();
        coolDownHandler?.ResetCoolTime();
    }
}

/// <summary>
/// Shared cooldown for SliderTouchData.
/// </summary>
public class SliderCoolDownHandler
{
    public SliderCoolDownHandler(float coolDownSeconds)
    {
        this.coolDownSeconds = coolDownSeconds;
    }

    private readonly float coolDownSeconds;

    public bool IsWaiting { get; private set; }

    public void ResetCoolTime()
    {
        IsWaiting = true;
        _ = DelayedExecutor.ExecuteAfterDelay(coolDownSeconds, () => { IsWaiting = false; });
    }
}
