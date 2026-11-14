using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace ChartEditor
{
    public class ChangeDeploymentNoteButtonView : MonoBehaviour
    {
        [SerializeField] Button button;

        // ショートカットキーの表示 (実行時に作る)
        TextMeshProUGUI shortcutText;

        public Action OnClickedListner { get; set; }

        public void OnChangeDeploymentNote(bool isInteracted)
        {
            button.interactable = !isInteracted;
        }

        public void OnChangeEditNoteType(bool isActive)
        {
            button.gameObject.SetActive(isActive);
        }

        public void OnClicked()
        {
            OnClickedListner?.Invoke();
        }

        /// <summary>
        /// このボタンのショートカットキーを、ボタンの右下に小さく表示する (ツールのボタンと同じ見た目)
        /// </summary>
        public void SetShortcutText(string text)
        {
            if (shortcutText == null) { shortcutText = CreateShortcutLabel(); }
            if (shortcutText == null) { return; }

            shortcutText.text = text;
        }

        private TextMeshProUGUI CreateShortcutLabel()
        {
            if (button == null) { return null; }

            // 文字の見た目(フォント)は、ボタンの名前の文字に合わせる
            var template = button.GetComponentInChildren<TextMeshProUGUI>(true);

            return ShortcutBadge.Create(button.transform, template);
        }
    }

}
