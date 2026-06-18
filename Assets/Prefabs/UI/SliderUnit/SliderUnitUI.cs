using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

public class SliderUnitUI : MonoBehaviour
{
    [SerializeField] Image image;

    bool isTouching;
    CancellationTokenSource cts = new CancellationTokenSource();
    CompositeDisposable disposables;

    public void SetSliderData(SliderTouchData sliderTouchData)
    {
        disposables?.Dispose();
        if (disposables == null || disposables.IsDisposed)
        {
            disposables = new CompositeDisposable();
        }

        Bind(sliderTouchData);
    }

    private void Bind(SliderTouchData sliderTouchData)
    {
        SetSliderColor(sliderTouchData.ThemeColor);
    }

    private void SetSliderColor(Color color)
    {
        // 色の変更
        if (image) { image.color = color; }
        if (isTouching)
        {
            cts.Cancel();
            isTouching = false;
        }
    }

    /// <summary>
    /// スライダーUIの反応
    /// </summary>
    /// <param name="sliderTouchData"></param>
    /// <param name="pressedDecrmentionColorValue"></param>
    public void InteractSlider(SliderTouchData sliderTouchData, float pressedDecrmentionColorValue, float duration)
    {
        if (!image) { return; }
        if (isTouching) { return; }

        isTouching = true;
        image.color = new Color(
            Mathf.Clamp(sliderTouchData.ThemeColor.r - pressedDecrmentionColorValue, 0, 1),
            Mathf.Clamp(sliderTouchData.ThemeColor.g - pressedDecrmentionColorValue, 0, 1),
            Mathf.Clamp(sliderTouchData.ThemeColor.b - pressedDecrmentionColorValue, 0, 1),
            sliderTouchData.ThemeColor.a);

        // 遅らせて元に戻す
        cts?.CancelAndDispose();
        cts = DelayUtility.Run(duration, () => DisConnectSlider(sliderTouchData));
    }

    /// <summary>
    /// スライダーUIの反応を元に戻す
    /// </summary>
    /// <param name="sliderTouchData"></param>
    /// <param name="pressedDecrmentionColorValue"></param>
    private void DisConnectSlider(SliderTouchData sliderTouchData)
    {
        if (!image) { return; }

        isTouching = false;
        image.color = sliderTouchData.ThemeColor;
    }

    private void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}
