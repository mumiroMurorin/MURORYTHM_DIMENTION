using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 3D編集の視点のウィンドウ・フィルムストリップの、押せるボタン (シーンに置いてある。押したときの処理は、コードで渡す)
    ///
    /// メニューの項目 (EditorMenuItem) と同じく、Unity の Button は使わない
    /// (押した後も選択中のまま残り、Space キーでもう一度押されてしまうため)。
    /// 色は、カーソルを乗せたとき・選んでいるとき・押せないときで変わる (メニューバーの色に合わせてある)。
    /// </summary>
    public class EditorUiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        /// <summary>ボタンなどの文字の色</summary>
        public static readonly Color TextColor = new Color(0.96f, 0.96f, 0.96f, 1f);

        static readonly Color normalColor = new Color(0.30f, 0.30f, 0.34f, 1f);
        static readonly Color hoverColor = new Color(0.42f, 0.42f, 0.48f, 1f);
        static readonly Color selectedColor = new Color(0.22f, 0.46f, 0.85f, 1f);
        static readonly Color selectedHoverColor = new Color(0.32f, 0.56f, 0.93f, 1f);
        static readonly Color disabledColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        static readonly Color disabledTextColor = new Color(0.50f, 0.50f, 0.53f, 1f);

        [Tooltip("ボタンの背景 (色が変わる)")]
        [SerializeField] Image background;
        [Tooltip("ボタンの文字")]
        [SerializeField] TextMeshProUGUI label;

        Action onClick;
        bool isHovered;
        bool isSelected;
        bool isInteractable = true;

        public RectTransform RectTransform => (RectTransform)transform;
        public TextMeshProUGUI Label => label;

        private void Awake()
        {
            Refresh();
        }

        /// <summary>
        /// 押されたときの処理を決める (前の処理と入れ替える)
        /// </summary>
        public void SetOnClick(Action onClick)
        {
            this.onClick = onClick;
        }

        public void SetSelected(bool isSelected)
        {
            if (this.isSelected == isSelected) { return; }

            this.isSelected = isSelected;
            Refresh();
        }

        public void SetInteractable(bool isInteractable)
        {
            if (this.isInteractable == isInteractable) { return; }

            this.isInteractable = isInteractable;
            Refresh();
        }

        public void SetText(string text)
        {
            if (label != null && label.text != text) { label.text = text; }
        }

        private void OnDisable()
        {
            isHovered = false;
            Refresh();
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            Refresh();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            Refresh();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            if (!isInteractable) { return; }

            onClick?.Invoke();
        }

        private void Refresh()
        {
            if (background != null)
            {
                bool isHighlighted = isHovered && isInteractable;
                background.color = !isInteractable ? disabledColor
                    : isSelected ? (isHighlighted ? selectedHoverColor : selectedColor)
                    : (isHighlighted ? hoverColor : normalColor);
            }

            if (label != null) { label.color = isInteractable ? TextColor : disabledTextColor; }
        }
    }
}
