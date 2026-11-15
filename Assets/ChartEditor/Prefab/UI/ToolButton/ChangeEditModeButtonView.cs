using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace ChartEditor
{
    public class ChangeEditModeButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        // ショートカットキーの表示 (実行時に作る)
        TextMeshProUGUI shortcutText;

        public Action OnClickedListner { get; set; }

        private void Start()
        {
            button.onClick.AddListener(OnClicked);
        }

        /// <summary>
        /// このボタンのショートカットキーを、ボタンの右下に小さく表示する (配置ノーツのボタンと同じ見た目)
        /// キーが決まっていないボタンには、空の文字を渡す (何も出さない)
        /// </summary>
        public void SetShortcutText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (shortcutText != null) { shortcutText.transform.parent.gameObject.SetActive(false); }
                return;
            }

            if (shortcutText == null)
            {
                // 文字の見た目(フォント)は、ボタンの名前の文字に合わせる
                shortcutText = ShortcutBadge.Create(button.transform, button.GetComponentInChildren<TextMeshProUGUI>(true));
            }
            if (shortcutText == null) { return; }

            shortcutText.transform.parent.gameObject.SetActive(true);
            shortcutText.text = text;
        }

        public void OnChangeEditMode(bool isInteracted)
        {
            button.interactable = !isInteracted;
        }

        public void OnClicked()
        {
            OnClickedListner?.Invoke();
        }

    }
}
