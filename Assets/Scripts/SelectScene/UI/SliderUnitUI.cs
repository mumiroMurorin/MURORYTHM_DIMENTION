using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Refactoring
{
    public class SliderUnitUI : MonoBehaviour
    {
        [SerializeField] Image image;

        public void SetSliderData(SliderTouchData sliderTouchData)
        {
            // êFÇÃïœçX
            if (image) { image.color = sliderTouchData.ImageColor; }
        }
    }

}