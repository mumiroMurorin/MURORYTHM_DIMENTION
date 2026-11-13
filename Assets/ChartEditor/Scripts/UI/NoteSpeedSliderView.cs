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
            button.OnPushButtonListner += OnPushApplyButton;
            slider.OnSliderChangedListener += OnValueChanged;
        }

        private void OnDisable()
        {
            // 登録時と同じメソッドを渡さないと解除されない (ラムダ式は毎回別物になる)
            button.OnPushButtonListner -= OnPushApplyButton;
            slider.OnSliderChangedListener -= OnValueChanged;
        }

        private void OnPushApplyButton()
        {
            OnNoteSpeedApplyListener?.Invoke(slider.SliderValue);
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