using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 頂点詳細設定の、ガイドテキスト (頂点を置くときの下書きの文字) の欄
    ///
    /// ・数値は、入力を終えたとき (Enter、ほかの場所をクリック) に反映する (NumberInputField)
    /// ・文字は、打つたびに反映する (下書きを見ながら打てるように)。打っている間は、欄の文字を外から書き直さない
    ///   (書き直すと、日本語入力の変換の途中で、文字が崩れる)
    /// </summary>
    // ほかの場所のクリックで入力を終えたとき、そのクリックで頂点の選択などが変わる前に、打った値を反映するため、先に動かす
    [DefaultExecutionOrder(-1000)]
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

        NumberInputField inputX;
        NumberInputField inputY;
        NumberInputField inputScale;
        NumberInputField inputRotation;
        NumberInputField inputAlpha;
        bool isOpenPanel;
        CancellationTokenSource rebuildLayoutCts;

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
            if (inputFieldText != null)
            {
                inputFieldText.onValueChanged.AddListener(OnTextValueChanged);
                // Enter で終えたときに、欄が「選択中」のまま残らないようにする (Space キーで、もう一度入力状態になってしまう)
                inputFieldText.onEndEdit.AddListener(_ => InputFieldFocus.Release(inputFieldText));
            }

            // (欄の名前を左右にドラッグしても、値を動かせる: 録音 20261118_162040)
            inputX = new NumberInputField(inputFieldX, value => OnXValueChangedListener?.Invoke(value))
                .EnableScrub(NumberScrubHandle.Settings.Float(0.01f));
            inputY = new NumberInputField(inputFieldY, value => OnYValueChangedListener?.Invoke(value))
                .EnableScrub(NumberScrubHandle.Settings.Float(0.01f));
            inputScale = new NumberInputField(inputFieldScale, value => OnScaleValueChangedListener?.Invoke(value))
                .EnableScrub(NumberScrubHandle.Settings.Float(0.01f, 0.01f));
            inputRotation = new NumberInputField(inputFieldRotation, value => OnRotationValueChangedListener?.Invoke(value))
                .EnableScrub(NumberScrubHandle.Settings.Float(0.5f));
            inputAlpha = new NumberInputField(inputFieldAlpha, value => OnAlphaValueChangedListener?.Invoke(value))
                .EnableScrub(NumberScrubHandle.Settings.Float(0.005f, 0f, 1f));

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

            // 打っている最中の文字は書き直さない (もう同じ文字が入っている)
            if (inputFieldText != null && !inputFieldText.isFocused)
            {
                inputFieldText.SetTextWithoutNotify(text ?? string.Empty);
            }

            inputX?.SetText(localPosition.x.ToString(format));
            inputY?.SetText(localPosition.y.ToString(format));
            inputScale?.SetText(scale.ToString(format));
            inputRotation?.SetText(rotation.ToString(format));
            inputAlpha?.SetText(alpha.ToString(format));
        }

        public void Clear()
        {
            if (inputFieldText != null) { inputFieldText.SetTextWithoutNotify(string.Empty); }
            inputX?.SetText(string.Empty);
            inputY?.SetText(string.Empty);
            inputScale?.SetText(string.Empty);
            inputRotation?.SetText(string.Empty);
            inputAlpha?.SetText(string.Empty);
        }


        void RaiseTextEnableValueChanged(bool enabled)
        {
            OnTextEnableValueChangedListener?.Invoke(enabled);
        }

        void OnTextValueChanged(string value)
        {
            OnTextValueChangedListener?.Invoke(value);
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
