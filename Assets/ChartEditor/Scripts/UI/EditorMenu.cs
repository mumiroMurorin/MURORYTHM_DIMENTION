using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 画面上のメニューバーに並ぶ見出し1つぶん (「ファイル」「編集」など) と、その下に開く一覧
    ///
    /// 見出しを押したこと・カーソルが乗ったことを知らせるだけで、どの一覧を開くかは EditorMenuBar が決める。
    /// 一覧(dropDown)は、シーンに最初から置いてあり、閉じている間は非表示にしてある。
    /// </summary>
    public class EditorMenu : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("見出しを押すと開く一覧")]
        [SerializeField] GameObject dropDown;
        [SerializeField] Image background;
        [SerializeField] TextMeshProUGUI label;

        static readonly Color normalColor = new Color(0f, 0f, 0f, 0f);
        static readonly Color hoverColor = new Color(0.32f, 0.32f, 0.37f, 1f);
        static readonly Color openColor = new Color(0.22f, 0.42f, 0.78f, 1f);
        static readonly Color textColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        static readonly Color textDisabledColor = new Color(0.50f, 0.50f, 0.53f, 1f);

        bool isOpen;
        bool isHovered;
        bool isInteractable = true;

        /// <summary>
        /// 見出しを押されたとき
        /// </summary>
        public Action<EditorMenu> OnPressedListener { get; set; }

        /// <summary>
        /// 見出しにカーソルが乗ったとき (ほかの一覧を開いている間は、乗っただけで開く一覧を切り替える)
        /// </summary>
        public Action<EditorMenu> OnEnteredListener { get; set; }

        public bool IsOpen => isOpen;

        /// <summary>
        /// 一覧を開く / 閉じる
        /// </summary>
        public void SetOpen(bool isOpen)
        {
            this.isOpen = isOpen;
            if (dropDown != null && dropDown.activeSelf != isOpen) { dropDown.SetActive(isOpen); }
            Refresh();
        }

        /// <summary>
        /// 見出しを押せるかどうかを切り替える (押せないときは、文字を薄くする)
        /// </summary>
        public void SetInteractable(bool isInteractable)
        {
            if (this.isInteractable == isInteractable) { return; }

            this.isInteractable = isInteractable;
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            isHovered = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            // 左ボタンだけ
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            if (!isInteractable) { return; }

            OnPressedListener?.Invoke(this);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            Refresh();

            if (isInteractable) { OnEnteredListener?.Invoke(this); }
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            Refresh();
        }

        private void Refresh()
        {
            if (background != null)
            {
                background.color = isOpen ? openColor : (isHovered && isInteractable ? hoverColor : normalColor);
            }
            if (label != null) { label.color = isInteractable ? textColor : textDisabledColor; }
        }
    }
}
