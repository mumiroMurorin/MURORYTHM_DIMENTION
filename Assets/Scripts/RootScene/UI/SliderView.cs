using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class SliderView : MonoBehaviour
{
    [SerializeField] Slider slider;

    public event Action<float> OnSliderChangedListener;

    public virtual void OnValueChanged(float value)
    {
        if (slider == null) { return; }
        slider.value = value;
    }

    public virtual void OnSliderValueChanged(float value)
    {
        OnSliderChangedListener?.Invoke(value);
    }
}