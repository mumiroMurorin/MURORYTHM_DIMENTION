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

        // ボタンに元からある、ショートカットキーの文字のオブジェクト名 (ボタンの下の端に「alt + S」のように書いてある)
        const string SHORTCUT_LABEL_NAME = "Text_Shortcut";

        public Action OnClickedListner { get; set; }

        private void Start()
        {
            button.onClick.AddListener(OnClicked);

            ReplaceShortcutLabel();
        }

        /// <summary>
        /// ショートカットキーの表示を、配置ノーツのボタンと同じ形式にする
        /// (ボタンの下の端の文字をやめて、右下の黒い四角に「Alt+S」のように書く。キーは元の文字から読み取る)
        /// </summary>
        private void ReplaceShortcutLabel()
        {
            TextMeshProUGUI original = null;
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.gameObject.name != SHORTCUT_LABEL_NAME) { continue; }

                original = text;
                break;
            }

            if (original == null) { return; }

            string shortcut = ShortcutBadge.Format(original.text);

            // 元の文字は隠す
            original.gameObject.SetActive(false);

            // ショートカットキーが決まっていないボタンには、何も出さない
            if (shortcut.Length == 0) { return; }

            TextMeshProUGUI badgeText = ShortcutBadge.Create(button.transform, original);
            if (badgeText != null) { badgeText.text = shortcut; }
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
