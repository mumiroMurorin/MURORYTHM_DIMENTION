using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ボタンの右下に出す、ショートカットキーの表示 (黒い四角に白い文字で「Alt+1」「Alt+S」のように書く)
    ///
    /// 配置ノーツのボタンとツールのボタンで同じ見た目にするため、作り方をここにまとめている。
    /// 表示は実行時に作る (ボタンの絵の上に重なっても読めるよう、暗い下地を敷く)。
    /// </summary>
    public static class ShortcutBadge
    {
        // 表示の大きさと位置 (ボタンの右下の角からの距離)
        static readonly Vector2 size = new Vector2(44f, 15f);
        static readonly Vector2 offset = new Vector2(-3f, 3f);
        // 文字の大きさ。長い表示は、四角に収まるよう下限まで自動で縮める
        const float FONT_SIZE = 12f;
        const float FONT_SIZE_MIN = 8f;
        static readonly Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        /// <summary>
        /// ボタンの右下に、ショートカットキーの表示を作る
        /// </summary>
        /// <param name="button">表示を付けるボタン</param>
        /// <param name="template">文字の見た目(フォント)を合わせる相手</param>
        /// <returns>表示の文字。作れなかったときは null</returns>
        public static TextMeshProUGUI Create(Transform button, TextMeshProUGUI template)
        {
            if (button == null || template == null) { return null; }

            // 下地
            var badge = new GameObject("Shortcut", typeof(RectTransform));
            badge.layer = button.gameObject.layer;

            var badgeRect = (RectTransform)badge.transform;
            badgeRect.SetParent(button, false);
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(1f, 0f);
            badgeRect.anchoredPosition = offset;
            badgeRect.sizeDelta = size;

            var background = badge.AddComponent<Image>();
            background.color = backgroundColor;
            // ボタンへのクリックを遮らない
            background.raycastTarget = false;

            // 文字
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.layer = badge.layer;

            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(badgeRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
            label.fontSize = FONT_SIZE;
            label.fontSizeMax = FONT_SIZE;
            label.fontSizeMin = FONT_SIZE_MIN;
            label.enableAutoSizing = true;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return label;
        }

        /// <summary>
        /// ショートカットキーの書き方を「Alt+S」の形式にそろえる
        /// ("alt + S" や "ALT+s" のように、空白や大文字・小文字がまちまちでも同じ書き方になる)
        /// </summary>
        /// <returns>そろえた文字。キーが書かれていなければ空の文字</returns>
        public static string Format(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) { return string.Empty; }

            string[] parts = text.Split('+');
            var builder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0) { continue; }

                if (builder.Length > 0) { builder.Append('+'); }

                if (i < parts.Length - 1)
                {
                    // 一緒に押すキー (Alt、Ctrl、Shift): 先頭だけ大文字
                    builder.Append(char.ToUpperInvariant(part[0]));
                    builder.Append(part.Substring(1).ToLowerInvariant());
                }
                else
                {
                    // 押すキー: 1文字のキーは大文字、それ以外(Space など)は書かれているとおり
                    builder.Append(part.Length == 1 ? part.ToUpperInvariant() : part);
                }
            }

            return builder.ToString();
        }
    }
}
