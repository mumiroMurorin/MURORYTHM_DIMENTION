using System;
using UniRx;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization.Tables;

/// <summary>
/// Central operation handler for slider-touch based inputs.
/// </summary>
public class OperationHandlerFromSlider : MonoBehaviour, IOperationSetter, IOperationGetter
{
    [SerializeField] private SerializeInterface<IInputHandler> inputHandler;

    // 今登録されているスライダー情報
    readonly ReactiveCollection<SliderTouchData> sliderTouchDatas = new ReactiveCollection<SliderTouchData>();
    IReadOnlyReactiveCollection<SliderTouchData> IOperationGetter.SliderTouchDatas => sliderTouchDatas;

    // タッチされた時のコールバック
    readonly Subject<SliderTouchData> onTouchSliderListner = new Subject<SliderTouchData>();
    IObservable<SliderTouchData> IOperationGetter.OnTouchSliderListner => onTouchSliderListner;

    void IOperationSetter.SetOperate(SliderTouchData sliderTouchData)
    {
        sliderTouchDatas.Add(sliderTouchData);
        inputHandler?.Value.SubscriveForTouchSlider(sliderTouchData.SliderIndices, 
            () => { 
                sliderTouchData.ExecuteAction();
                onTouchSliderListner.OnNext(sliderTouchData);
            });
    }

    public void Dispose()
    {
        foreach (var data in sliderTouchDatas) { data.Dispose(); }

        sliderTouchDatas.Clear();
        inputHandler?.Value.Dispose();
    }

    private void OnDestroy()
    {
        Dispose();
        onTouchSliderListner?.Dispose();
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

    IObservable<SliderTouchData> OnTouchSliderListner { get; }
}

/// <summary>
/// Data for a slider touch operation.
/// </summary>
public class SliderTouchData
{
    public SliderTouchData(OperationAssetUnit asset, Action callback, TableReference textTableReference, SliderCoolDownHandler coolDownHandler = default)
    {
        SetSliderIndices(asset.SliderIndices);
        SetCallback(callback);
        SetThemeColor(asset.ThemeColor);
        SetControllerColor(asset.ControllerColor);
        SetControllerRainbow(asset.ControllerRainbow);
        SetTextKey(asset.TextKey);
        SetTextTableReference(textTableReference);
        this.coolDownHandler = coolDownHandler;
    }

    public SliderTouchData(int[] sliderIndices, Action callback, Color themeColor = default, Color controllerColor = default, bool controllerRainbow = false, string textKey = default, TableReference textTableReference = default, SliderCoolDownHandler coolDownHandler = default)
    {
        SetSliderIndices(sliderIndices);
        SetCallback(callback);
        SetThemeColor(themeColor);
        SetControllerColor(controllerColor == default ? themeColor : controllerColor);
        SetControllerRainbow(controllerRainbow);
        SetTextKey(textKey);
        SetTextTableReference(textTableReference);
        this.coolDownHandler = coolDownHandler;
    }

    private readonly SliderCoolDownHandler coolDownHandler;


    private int[] sliderIndices;
    public IEnumerable<int> SliderIndices { get { return sliderIndices; } }
    private void SetSliderIndices(int[] indices)
    {
        sliderIndices = indices;
    }


    private ReactiveProperty<Action> callBack = new ReactiveProperty<Action>();
    private void SetCallback(Action action)
    {
        callBack.Value = action;
    }


    // ThemeColor
    public Color ThemeColor { get; private set; } = Color.red;
    private void SetThemeColor(Color color)
    {
        ThemeColor = color;
    }


    // ControllerColor
    public Color ControllerColor { get; private set; } = Color.red;
    private void SetControllerColor(Color color)
    {
        ControllerColor = color;
    }


    // ControllerRainbowColor
    public bool ControllerRainbow { get; private set; }
    private void SetControllerRainbow(bool value)
    {
        ControllerRainbow = value;
    }


    // TextKey
    public string TextKey { get; private set; }
    private void SetTextKey(string value)
    {
        TextKey = value;
    }


    // TableReference
    public TableReference TextTableReference { get; private set; }
    private void SetTextTableReference(TableReference value)
    {
        TextTableReference = value;
    }


    public void ExecuteAction()
    {
        if (coolDownHandler != null && coolDownHandler.IsWaiting) { return; }

        callBack?.Value?.Invoke();
        coolDownHandler?.ResetCoolTime();
    }

    public void Dispose()
    {
        callBack.Value = null;
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
