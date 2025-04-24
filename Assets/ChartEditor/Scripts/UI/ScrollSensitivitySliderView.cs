using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace ChartEditor
{
    [RequireComponent(typeof(Slider))]
    public class ScrollSensitivitySliderView : MonoBehaviour
    {
        Slider slider;

        public Action<float> OnSliderChangedListener;

        private void Start()
        {
            slider = GetComponent<Slider>();
        }

        public void OnSensitivityChanged(float value)
        {
            slider.value = value;
        }

        public void OnSliderValueChanged(float value)
        {
            OnSliderChangedListener?.Invoke(value);
        }
    }

}
