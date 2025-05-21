using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using TMPro;
using System.Linq;

public class SliderTopicTextController : MonoBehaviour
{
    [SerializeField] CircularText circularText;
    [SerializeField] TextMeshProUGUI tmp;

    /// <summary>
    /// 操作情報をUI化
    /// </summary>
    /// <param name="sliderTouchData"></param>
    public void SetSliderTouchData(SliderTouchData sliderTouchData)
    {
        // 範囲だけは初期化されないので明示的に更新
        UpdateRange(sliderTouchData.SliderIndices.ToArray());
        Bind(sliderTouchData);
    }

    private void Bind(SliderTouchData sliderTouchData)
    {
        // 表示範囲更新
        sliderTouchData?.SliderIndices.ObserveCountChanged()
            .Subscribe(_ => UpdateRange(sliderTouchData.SliderIndices.ToArray()))
            .AddTo(this.gameObject);

        // 表示色
        sliderTouchData?.ImageColor
            .Subscribe(UpdateColor)
            .AddTo(this.gameObject);

        // 表示テキスト
        sliderTouchData?.Text
            .Subscribe(UpdateText)
            .AddTo(this.gameObject);
    }

    private void UpdateRange(int[] indices)
    {
        // 角度の計算
        float range = indices.Max() - indices.Min() + 1;
        circularText.CenterAngle = (indices.Min() + range / 2f) * 11.25f - 180f;
    }

    private void UpdateColor(Color color)
    {
        tmp.faceColor = color;
    }

    private void UpdateText(string text)
    {
        tmp.text = text;
    }
}
