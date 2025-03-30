using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class SliderTopicTextController : MonoBehaviour
{
    [SerializeField] CircularText circularText;
    [SerializeField] TextMeshProUGUI tmp;

    /// <summary>
    /// ‘€ìî•ñ‚ğUI‰»
    /// </summary>
    /// <param name="sliderTouchData"></param>
    public void SetSliderTouchData(SliderTouchData sliderTouchData)
    {
        // Šp“x‚ÌŒvZ
        float range = sliderTouchData.SliderIndices.Max() - sliderTouchData.SliderIndices.Min() + 1;
        circularText.CenterAngle = (sliderTouchData.SliderIndices.Min() + range / 2f) * 11.25f - 180f;

        tmp.faceColor = sliderTouchData.ImageColor;
        tmp.text = sliderTouchData.Text;
    }
}
