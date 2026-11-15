using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 頂点詳細設定の、頂点の座標 (X・Y) の欄
    /// 座標は、入力を終えたとき (Enter、ほかの場所をクリック) に反映する (NumberInputField)
    /// </summary>
    // ほかの場所のクリックで入力を終えたとき、そのクリックで頂点の選択などが変わる前に、打った値を反映するため、先に動かす
    [DefaultExecutionOrder(-1000)]
    public class VertexPositionPanelView : MonoBehaviour
    {
        [SerializeField] GameObject[] displayElements;
        [SerializeField] TMP_InputField inputFieldX;
        [SerializeField] TMP_InputField inputFieldY;
        [SerializeField] TextChangableButtonView openCloseButton_view;

        public event Action OnBeginEditListener;
        public event Action OnEndEditListener;
        public event Action<float> OnXValueChangedListener;
        public event Action<float> OnYValueChangedListener;

        NumberInputField inputX;
        NumberInputField inputY;
        bool isOpenPanel;

        void Update()
        {
            // ほかの場所がクリックされたら、そのクリックが処理される前に、打っている値を反映する
            inputX?.CommitIfClickedElsewhere();
            inputY?.CommitIfClickedElsewhere();
        }

        void Awake()
        {
            // 値を知らせる処理を先に登録する (その後で、入力の終わりを知らせる。元に戻す操作の記録は、値が入った後に行うため)
            inputX = new NumberInputField(inputFieldX, value => OnXValueChangedListener?.Invoke(value));
            inputY = new NumberInputField(inputFieldY, value => OnYValueChangedListener?.Invoke(value));

            if (inputFieldX != null)
            {
                inputFieldX.onSelect.AddListener(_ => OnBeginEditListener?.Invoke());
                inputFieldX.onEndEdit.AddListener(_ => OnEndEditListener?.Invoke());
            }

            if (inputFieldY != null)
            {
                inputFieldY.onSelect.AddListener(_ => OnBeginEditListener?.Invoke());
                inputFieldY.onEndEdit.AddListener(_ => OnEndEditListener?.Invoke());
            }

            if (openCloseButton_view != null)
            {
                openCloseButton_view.OnPushButtonListner += OnSwitchPanelState;
                openCloseButton_view.OnChangeValue(isOpenPanel);
                UpdatePanelState(isOpenPanel);
            }
        }

        public void SetInteractable(bool value)
        {
            if (inputFieldX != null) { inputFieldX.interactable = value; }
            if (inputFieldY != null) { inputFieldY.interactable = value; }
        }

        public void SetPosition(Vector2 position, int decimalDigits)
        {
            var format = $"F{Mathf.Clamp(decimalDigits, 0, 9)}";

            inputX?.SetText(position.x.ToString(format));
            inputY?.SetText(position.y.ToString(format));
        }

        public void Clear()
        {
            inputX?.SetText(string.Empty);
            inputY?.SetText(string.Empty);
        }

        void OnSwitchPanelState()
        {
            isOpenPanel = !isOpenPanel;
            openCloseButton_view.OnChangeValue(isOpenPanel);
            UpdatePanelState(isOpenPanel);
        }

        void UpdatePanelState(bool isOpen)
        {
            foreach(var o in displayElements)
            {
                o.SetActive(isOpen);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
