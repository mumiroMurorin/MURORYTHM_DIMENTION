using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

namespace Refactoring
{
    public class SliderTopicTextController : MonoBehaviour
    {
        [SerializeField] CircularText circularText;
        [SerializeField] TextMeshProUGUI tmp;

        /// <summary>
        /// ëÄçÏèÓïÒÇUIâª
        /// </summary>
        /// <param name="sliderTouchData"></param>
        public void SetSliderTouchData(SliderTouchData sliderTouchData)
        {
            // äpìxÇÃåvéZ
            float range = sliderTouchData.SliderIndices.Max() - sliderTouchData.SliderIndices.Min() + 1;
            circularText.CenterAngle = (sliderTouchData.SliderIndices.Min() + range / 2f) * 11.25f - 180f;

            tmp.faceColor = sliderTouchData.ImageColor;
            tmp.text = sliderTouchData.Text;
        }
    }
}
