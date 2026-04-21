using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ChartEditor
{
    public class NoteSpeedSliderView : SliderView
    {
        [SerializeField] TextMeshProUGUI noteSpeedValueText;

        public override void OnValueChanged(float value)
        {
            base.OnValueChanged(value);

            if (noteSpeedValueText != null)
            {
                noteSpeedValueText.text = value.ToString("F1");
            }
        }
    }
}