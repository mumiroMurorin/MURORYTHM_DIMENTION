using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

namespace ChartEditor
{
    public class NoteSpeedSliderView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI noteSpeedValueText;
        [SerializeField] SliderView slider;
        [SerializeField] ButtonView button;

        public event Action<float> OnNoteSpeedApplyListener;

        private void OnEnable()
        {
            button.OnPushButtonListner += () => OnNoteSpeedApplyListener(slider.SliderValue);
            slider.OnSliderChangedListener += OnValueChanged;
        }

        private void OnDisable()
        {
            button.OnPushButtonListner -= () => OnNoteSpeedApplyListener(slider.SliderValue);
            slider.OnSliderChangedListener -= OnValueChanged;
        }

        public void OnValueChanged(float value)
        {
            slider.OnValueChanged(value);

            if (noteSpeedValueText != null)
            {
                noteSpeedValueText.text = value.ToString("F1");
            }
        }
    }
}