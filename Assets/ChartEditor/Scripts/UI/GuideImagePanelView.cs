using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    public class GuideImagePanelView : MonoBehaviour
    {
        [SerializeField] GameObject[] displayElements;
        [SerializeField] TMP_InputField inputFieldX;
        [SerializeField] TMP_InputField inputFieldY;
        [SerializeField] TMP_InputField inputFieldScale;
        [SerializeField] TMP_InputField inputFieldRotation;
        [SerializeField] TMP_InputField inputFieldAlpha;
        [SerializeField] ButtonView selectImageButton_view;
        [SerializeField] ToggleView imageEnableToggle_view;
        [SerializeField] TextChangableButtonView openCloseButton_view;

        public event Action OnSelectImageButtonClickedListener;
        public event Action<bool> OnImageEnableValueChangedListener;
        public event Action<float> OnXValueChangedListener;
        public event Action<float> OnYValueChangedListener;
        public event Action<float> OnScaleValueChangedListener;
        public event Action<float> OnRotationValueChangedListener;
        public event Action<float> OnAlphaValueChangedListener;

        bool isOpenPanel;

        void Awake()
        {
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

            if (selectImageButton_view != null)
            {
                selectImageButton_view.OnPushButtonListner += RaiseSelectImageButtonClicked;
            }

            if (imageEnableToggle_view != null)
            {
                imageEnableToggle_view.OnPushToggleListner += RaiseDeleteImageButtonClicked;
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
            if (selectImageButton_view != null)
            {
                selectImageButton_view.OnPushButtonListner -= RaiseSelectImageButtonClicked;
            }

            if (imageEnableToggle_view != null)
            {
                imageEnableToggle_view.OnPushToggleListner -= RaiseDeleteImageButtonClicked;
            }
        }

        public void SetInteractable(bool value)
        {
            if (inputFieldX != null) { inputFieldX.interactable = value; }
            if (inputFieldY != null) { inputFieldY.interactable = value; }
            if (inputFieldScale != null) { inputFieldScale.interactable = value; }
            if (inputFieldRotation != null) { inputFieldRotation.interactable = value; }
            if (inputFieldAlpha != null) { inputFieldAlpha.interactable = value; }
        }

        public void SetData(Vector3 localPosition, float scale, float rotation, float alpha, int decimalDigits)
        {
            var format = $"F{Mathf.Clamp(decimalDigits, 0, 9)}";

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
            if (inputFieldX != null) { inputFieldX.SetTextWithoutNotify(string.Empty); }
            if (inputFieldY != null) { inputFieldY.SetTextWithoutNotify(string.Empty); }
            if (inputFieldScale != null) { inputFieldScale.SetTextWithoutNotify(string.Empty); }
            if (inputFieldRotation != null) { inputFieldRotation.SetTextWithoutNotify(string.Empty); }
            if (inputFieldAlpha != null) { inputFieldAlpha.SetTextWithoutNotify(string.Empty); }
        }

        void RaiseSelectImageButtonClicked()
        {
            OnSelectImageButtonClickedListener?.Invoke();
        }

        void RaiseDeleteImageButtonClicked(bool isEnable)
        {
            OnImageEnableValueChangedListener?.Invoke(isEnable);
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

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
