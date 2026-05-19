using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using TMPro;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SliderTopicTextController : MonoBehaviour
{
    [SerializeField] CircularText circularText;
    [SerializeField] TextMeshProUGUI tmp;
    [SerializeField] float angleOfCharacterJa = 3f;
    [SerializeField] float angleOfCharacterEn = 15f;
    [SerializeField] float angleOfCharacterFallback = 15f;
    [SerializeField] bool autoAdjustmentAngleRange = true;

    private LocalizedString localizedString;

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
        sliderTouchData?.ThemeColor
            .Subscribe(UpdateColor)
            .AddTo(this.gameObject);

        // 表示テキストキー
        sliderTouchData?.TextKey
            .Subscribe(textKey => ApplyLocalizedText(sliderTouchData, textKey))
            .AddTo(this.gameObject);
    }

    private void ApplyLocalizedText(SliderTouchData sliderTouchData, string textKey)
    {
        ClearLocalizedTextBinding();

        if (string.IsNullOrWhiteSpace(textKey))
        {
            UpdateText(string.Empty);
            return;
        }

        var tableReference = sliderTouchData.TextTableReference.Value;
        if (tableReference.ReferenceType == UnityEngine.Localization.Tables.TableReference.Type.Empty)
        {
            UpdateText(textKey);
            return;
        }

        localizedString = new LocalizedString(tableReference, textKey);
        localizedString.StringChanged += UpdateText;
        localizedString.RefreshString();
    }

    private void ClearLocalizedTextBinding()
    {
        if (localizedString == null)
        {
            return;
        }

        localizedString.StringChanged -= UpdateText;
        localizedString = null;
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
        circularText.SetAngleOfCharacter(GetAngleOfCharacterByLocale());
        circularText.SetAutoAdjustmentAngleRange(autoAdjustmentAngleRange);
        circularText.RefreshLayout();
    }

    private float GetAngleOfCharacterByLocale()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
        {
            return angleOfCharacterFallback;
        }

        var code = locale.Identifier.Code;
        if (code == "ja")
        {
            return angleOfCharacterJa;
        }

        if (code == "en")
        {
            return angleOfCharacterEn;
        }

        return angleOfCharacterFallback;
    }

    private void OnDestroy()
    {
        ClearLocalizedTextBinding();
    }
}
