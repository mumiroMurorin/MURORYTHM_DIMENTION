using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using Cysharp.Threading.Tasks;

public class SliderUnitUI : MonoBehaviour
{
    [SerializeField] Image image;

    bool isTouching;
    CancellationTokenSource cts = new CancellationTokenSource();

    public void SetSliderData(SliderTouchData sliderTouchData)
    {
        // 色の変更
        if (image) { image.color = sliderTouchData.ImageColor.Value; }
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
            Mathf.Clamp(sliderTouchData.ImageColor.Value.r - pressedDecrmentionColorValue, 0, 1),
            Mathf.Clamp(sliderTouchData.ImageColor.Value.g - pressedDecrmentionColorValue, 0, 1),
            Mathf.Clamp(sliderTouchData.ImageColor.Value.b - pressedDecrmentionColorValue, 0, 1),
            sliderTouchData.ImageColor.Value.a);

        // 遅らせて元に戻す
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        cts = new CancellationTokenSource();
        _ = DelayedExecutor.ExecuteAfterDelay(duration, () => DisConnectSlider(sliderTouchData), cts.Token);
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
        image.color = sliderTouchData.ImageColor.Value;
    }

    private void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
}
