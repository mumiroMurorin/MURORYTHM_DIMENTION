using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 頂点詳細設定の、ガイド画像 (頂点を置くときの下絵) の欄
    /// 数値は、入力を終えたとき (Enter、ほかの場所をクリック) に反映する (NumberInputField)
    /// </summary>
    // ほかの場所のクリックで入力を終えたとき、そのクリックで頂点の選択などが変わる前に、打った値を反映するため、先に動かす
    [DefaultExecutionOrder(-1000)]
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

        NumberInputField inputX;
        NumberInputField inputY;
        NumberInputField inputScale;
        NumberInputField inputRotation;
        NumberInputField inputAlpha;
        bool isOpenPanel;

        void Update()
        {
            // ほかの場所がクリックされたら、そのクリックが処理される前に、打っている値を反映する
            inputX?.CommitIfClickedElsewhere();
            inputY?.CommitIfClickedElsewhere();
            inputScale?.CommitIfClickedElsewhere();
            inputRotation?.CommitIfClickedElsewhere();
            inputAlpha?.CommitIfClickedElsewhere();
        }

        void Awake()
        {
            inputX = new NumberInputField(inputFieldX, value => OnXValueChangedListener?.Invoke(value));
            inputY = new NumberInputField(inputFieldY, value => OnYValueChangedListener?.Invoke(value));
            inputScale = new NumberInputField(inputFieldScale, value => OnScaleValueChangedListener?.Invoke(value));
            inputRotation = new NumberInputField(inputFieldRotation, value => OnRotationValueChangedListener?.Invoke(value));
            inputAlpha = new NumberInputField(inputFieldAlpha, value => OnAlphaValueChangedListener?.Invoke(value));

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

            inputX?.SetText(localPosition.x.ToString(format));
            inputY?.SetText(localPosition.y.ToString(format));
            inputScale?.SetText(scale.ToString(format));
            inputRotation?.SetText(rotation.ToString(format));
            inputAlpha?.SetText(alpha.ToString(format));
        }

        public void Clear()
        {
            inputX?.SetText(string.Empty);
            inputY?.SetText(string.Empty);
            inputScale?.SetText(string.Empty);
            inputRotation?.SetText(string.Empty);
            inputAlpha?.SetText(string.Empty);
        }

        void RaiseSelectImageButtonClicked()
        {
            OnSelectImageButtonClickedListener?.Invoke();
        }

        void RaiseDeleteImageButtonClicked(bool isEnable)
        {
            OnImageEnableValueChangedListener?.Invoke(isEnable);
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
