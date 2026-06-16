using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    public class TextGuidePanelView : MonoBehaviour
    {
        [SerializeField] GameObject[] displayElements;
        [SerializeField] TMP_InputField inputFieldText;
        [SerializeField] TMP_InputField inputFieldX;
        [SerializeField] TMP_InputField inputFieldY;
        [SerializeField] TMP_InputField inputFieldScale;
        [SerializeField] TMP_InputField inputFieldRotation;
        [SerializeField] TMP_InputField inputFieldAlpha;
        [SerializeField] TMP_Dropdown fontDropdown;
        [SerializeField] ToggleView textEnableToggle_view;
        [SerializeField] TextChangableButtonView openCloseButton_view;

        public event Action<string> OnFontNameChangedListener;
        public event Action<bool> OnTextEnableValueChangedListener;
        public event Action<string> OnTextValueChangedListener;
        public event Action<float> OnXValueChangedListener;
        public event Action<float> OnYValueChangedListener;
        public event Action<float> OnScaleValueChangedListener;
        public event Action<float> OnRotationValueChangedListener;
        public event Action<float> OnAlphaValueChangedListener;

        bool isOpenPanel;
        CancellationTokenSource rebuildLayoutCts;

        void Awake()
        {
            if (inputFieldText != null)
            {
                inputFieldText.onValueChanged.AddListener(OnTextValueChanged);
            }

            if (inputFieldX != null)
            {
                inputFieldX.onValueChanged.AddListener(OnXValueChanged);
            }

            if (inputFieldY != null)
            {
                inputFieldY.onValueChanged.AddListener(OnYValueChanged);
            }

            if (inputFieldScale != null)
            {
                inputFieldScale.onValueChanged.AddListener(OnScaleValueChanged);
            }

            if (inputFieldRotation != null)
            {
                inputFieldRotation.onValueChanged.AddListener(OnRotationValueChanged);
            }

            if (inputFieldAlpha != null)
            {
                inputFieldAlpha.onValueChanged.AddListener(OnAlphaValueChanged);
            }

            if (fontDropdown != null)
            {
                fontDropdown.onValueChanged.AddListener(OnFontDropdownValueChanged);
            }


            if (textEnableToggle_view != null)
            {
                textEnableToggle_view.OnPushToggleListner += RaiseTextEnableValueChanged;
            }

            if (openCloseButton_view != null)
            {
                openCloseButton_view.OnPushButtonListner += OnSwitchPanelState;
                openCloseButton_view.OnChangeValue(isOpenPanel);
                UpdatePanelState(isOpenPanel);
            }
        }

        void OnDestroy()
        {
            CancelLayoutRebuild();

            if (textEnableToggle_view != null)
            {
                textEnableToggle_view.OnPushToggleListner -= RaiseTextEnableValueChanged;
            }
        }

        public void SetInteractable(bool value)
        {
            if (inputFieldText != null) { inputFieldText.interactable = value; }
            if (inputFieldX != null) { inputFieldX.interactable = value; }
            if (inputFieldY != null) { inputFieldY.interactable = value; }
            if (inputFieldScale != null) { inputFieldScale.interactable = value; }
            if (inputFieldRotation != null) { inputFieldRotation.interactable = value; }
            if (inputFieldAlpha != null) { inputFieldAlpha.interactable = value; }
            if (fontDropdown != null) { fontDropdown.interactable = value; }
        }

        public void SetEnabledState(bool enabled)
        {
            if (textEnableToggle_view != null)
            {
                textEnableToggle_view.OnChangeModelValue(enabled);
            }
        }

        public void SetFontOptions(IReadOnlyList<string> fontNames)
        {
            if (fontDropdown == null) { return; }

            fontDropdown.ClearOptions();

            var options = fontNames != null ? new List<string>(fontNames) : new List<string>();
            if (options.Count == 0)
            {
                options.Add("No Fonts");
                fontDropdown.interactable = false;
            }
            else
            {
                fontDropdown.interactable = true;
            }

            fontDropdown.AddOptions(options);
            fontDropdown.SetValueWithoutNotify(0);
            fontDropdown.RefreshShownValue();
        }

        public void SetSelectedFontName(string fontName)
        {
            if (fontDropdown == null || string.IsNullOrWhiteSpace(fontName)) { return; }

            for (int i = 0; i < fontDropdown.options.Count; i++)
            {
                if (fontDropdown.options[i].text != fontName) { continue; }

                fontDropdown.SetValueWithoutNotify(i);
                fontDropdown.RefreshShownValue();
                return;
            }
        }

        public void SetData(string text, Vector3 localPosition, float scale, float rotation, float alpha, int decimalDigits)
        {
            var format = $"F{Mathf.Clamp(decimalDigits, 0, 9)}";

            if (inputFieldText != null)
            {
                inputFieldText.SetTextWithoutNotify(text ?? string.Empty);
            }

            if (inputFieldX != null)
            {
                inputFieldX.SetTextWithoutNotify(localPosition.x.ToString(format));
            }

            if (inputFieldY != null)
            {
                inputFieldY.SetTextWithoutNotify(localPosition.y.ToString(format));
            }

            if (inputFieldScale != null)
            {
                inputFieldScale.SetTextWithoutNotify(scale.ToString(format));
            }

            if (inputFieldRotation != null)
            {
                inputFieldRotation.SetTextWithoutNotify(rotation.ToString(format));
            }

            if (inputFieldAlpha != null)
            {
                inputFieldAlpha.SetTextWithoutNotify(alpha.ToString(format));
            }
        }

        public void Clear()
        {
            if (inputFieldText != null) { inputFieldText.SetTextWithoutNotify(string.Empty); }
            if (inputFieldX != null) { inputFieldX.SetTextWithoutNotify(string.Empty); }
            if (inputFieldY != null) { inputFieldY.SetTextWithoutNotify(string.Empty); }
            if (inputFieldScale != null) { inputFieldScale.SetTextWithoutNotify(string.Empty); }
            if (inputFieldRotation != null) { inputFieldRotation.SetTextWithoutNotify(string.Empty); }
            if (inputFieldAlpha != null) { inputFieldAlpha.SetTextWithoutNotify(string.Empty); }
        }


        void RaiseTextEnableValueChanged(bool enabled)
        {
            OnTextEnableValueChangedListener?.Invoke(enabled);
        }

        void OnTextValueChanged(string value)
        {
            OnTextValueChangedListener?.Invoke(value);
        }

        void OnXValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnXValueChangedListener?.Invoke(parsed);
        }

        void OnYValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnYValueChangedListener?.Invoke(parsed);
        }

        void OnScaleValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnScaleValueChangedListener?.Invoke(parsed);
        }

        void OnRotationValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnRotationValueChangedListener?.Invoke(parsed);
        }

        void OnAlphaValueChanged(string value)
        {
            if (!float.TryParse(value, out var parsed)) { return; }
            OnAlphaValueChangedListener?.Invoke(parsed);
        }

        void OnFontDropdownValueChanged(int index)
        {
            if (fontDropdown == null) { return; }
            if (index < 0 || index >= fontDropdown.options.Count) { return; }

            OnFontNameChangedListener?.Invoke(fontDropdown.options[index].text);
        }

        void OnSwitchPanelState()
        {
            isOpenPanel = !isOpenPanel;
            openCloseButton_view.OnChangeValue(isOpenPanel);
            UpdatePanelState(isOpenPanel);
        }

        void UpdatePanelState(bool isOpen)
        {
            foreach (var o in displayElements)
            {
                o.SetActive(isOpen);
            }

            ScheduleLayoutRebuild();
        }

        void ScheduleLayoutRebuild()
        {
            CancelLayoutRebuild();
            rebuildLayoutCts = DelayUtility.Run(0f, RebuildLayoutNextFrame);
        }

        void RebuildLayoutNextFrame()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }

            if (transform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            }
        }

        void CancelLayoutRebuild()
        {
            if (rebuildLayoutCts == null) { return; }

            rebuildLayoutCts.Cancel();
            rebuildLayoutCts.Dispose();
            rebuildLayoutCts = null;
        }
    }
}
