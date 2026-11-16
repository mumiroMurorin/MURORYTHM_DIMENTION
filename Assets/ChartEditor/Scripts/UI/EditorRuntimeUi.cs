using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 実行時に作る画面部品 (フィルムストリップ、SpaceHold の3D編集) の、作り方をまとめたもの
    ///
    /// シーンには置かず、起動したときにコードで作る。色や大きさを変えたいときは、ここと、それぞれの画面の定数を直す。
    /// 見た目は、メニューバー (EditorMenuItem) の色に合わせてある。文字のフォントは、シーンにある文字と同じものを使う。
    /// </summary>
    public static class EditorRuntimeUi
    {
        public static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.14f, 0.94f);
        public static readonly Color PanelBorderColor = new Color(0.32f, 0.32f, 0.36f, 1f);
        public static readonly Color TextColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        public static readonly Color SubTextColor = new Color(0.72f, 0.72f, 0.76f, 1f);

        static TMP_FontAsset font;
        static Material fontMaterial;
        static Sprite circleSprite;

        /// <summary>
        /// 画面に直接重ねるキャンバスを作る (ほかのキャンバスと同じく、1920×1080 を基準に、横幅に合わせて拡大縮小する)
        /// </summary>
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = LayerMask.NameToLayer("UI");

            var canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            obj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            if (parent != null) { obj.layer = parent.gameObject.layer; }

            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>
        /// 位置と大きさを決める (anchor と pivot は同じ点)
        /// </summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>
        /// 親いっぱいに広げる (内側の余白を付けられる)
        /// </summary>
        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget = false)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>
        /// 枠線の付いた下地
        /// </summary>
        public static Image CreatePanel(string name, Transform parent, bool raycastTarget = true)
        {
            var border = CreateImage(name, parent, PanelBorderColor, raycastTarget);
            var inner = CreateImage("Inner", border.transform, PanelColor);
            Stretch(inner.rectTransform, 1f, 1f, 1f, 1f);
            return border;
        }

        public static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyFont(label);
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color ?? TextColor;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// シーンにある文字と同じフォントにする
        /// </summary>
        public static void ApplyFont(TextMeshProUGUI label)
        {
            if (font == null)
            {
                // メニューバーなどの文字 (日本語が出るフォント) を探す
                foreach (var existing in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true))
                {
                    if (existing == null || existing.font == null || existing == label) { continue; }

                    font = existing.font;
                    fontMaterial = existing.fontSharedMaterial;
                    break;
                }

                if (font == null) { font = TMP_Settings.defaultFontAsset; }
            }

            if (font != null) { label.font = font; }
            if (fontMaterial != null) { label.fontSharedMaterial = fontMaterial; }
        }

        public static EditorUiButton CreateButton(string name, Transform parent, string text, Vector2 size, Action onClick, float fontSize = 15f)
        {
            var rect = CreateRect(name, parent);
            rect.sizeDelta = size;

            var background = rect.gameObject.AddComponent<Image>();
            background.raycastTarget = true;

            var label = CreateLabel("Label", rect, text, fontSize, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 4f, 0f, 4f, 0f);

            var button = rect.gameObject.AddComponent<EditorUiButton>();
            button.Initialize(background, label, onClick);
            return button;
        }

        /// <summary>
        /// 丸い点の絵 (つまみに使う。縁をなめらかにしてある)
        /// </summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null) { return circleSprite; }

                const int SIZE = 64;
                var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave,
                };

                float radius = SIZE * 0.5f - 1f;
                var pixels = new Color32[SIZE * SIZE];
                for (int y = 0; y < SIZE; y++)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(SIZE * 0.5f, SIZE * 0.5f));
                        float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                        pixels[y * SIZE + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply();

                circleSprite = Sprite.Create(texture, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f), 100f);
                circleSprite.hideFlags = HideFlags.HideAndDontSave;
                return circleSprite;
            }
        }

        /// <summary>
        /// 画面上の位置 (ピクセル) を、キャンバスの部品の中の位置に直す
        /// </summary>
        public static bool TryGetLocalPoint(RectTransform rect, Vector2 screenPosition, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out localPoint);
        }
    }

    /// <summary>
    /// 実行時に作る画面部品の、押せるボタン
    ///
    /// メニューの項目 (EditorMenuItem) と同じく、Unity の Button は使わない
    /// (押した後も選択中のまま残り、Space キーでもう一度押されてしまうため)。
    /// </summary>
    public class EditorUiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        static readonly Color normalColor = new Color(0.30f, 0.30f, 0.34f, 1f);
        static readonly Color hoverColor = new Color(0.42f, 0.42f, 0.48f, 1f);
        static readonly Color selectedColor = new Color(0.22f, 0.46f, 0.85f, 1f);
        static readonly Color selectedHoverColor = new Color(0.32f, 0.56f, 0.93f, 1f);
        static readonly Color disabledColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        static readonly Color disabledTextColor = new Color(0.50f, 0.50f, 0.53f, 1f);

        Image background;
        TextMeshProUGUI label;
        Action onClick;
        bool isHovered;
        bool isSelected;
        bool isInteractable = true;

        public RectTransform RectTransform => (RectTransform)transform;
        public TextMeshProUGUI Label => label;

        public void Initialize(Image background, TextMeshProUGUI label, Action onClick)
        {
            this.background = background;
            this.label = label;
            this.onClick = onClick;
            Refresh();
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

            if (label != null) { label.color = isInteractable ? EditorRuntimeUi.TextColor : disabledTextColor; }
        }
    }
}
