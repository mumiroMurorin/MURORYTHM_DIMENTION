using UnityEngine;
using TMPro;

namespace ChartEditor
{
    public class TextGuideObject : MonoBehaviour
    {
        const string TmpObjectName = "TextGuideTMP";

        [SerializeField] int sortingOrder = -90;
        [SerializeField, Range(0f, 1f)] float alpha = 0.6f;
        [SerializeField] Vector2 textAreaSize = new Vector2(10f, 3f);

        TextMeshPro tmpText;
        TMP_FontAsset fontAsset;
        TextMesh legacyTextMesh;
        MeshRenderer legacyMeshRenderer;
        bool isEnabled = true;

        public bool IsSettingFont => tmpText != null && tmpText.font != null;
        public Vector3 LocalPosition => transform.localPosition;
        public float Scale => transform.localScale.x;
        public float RotationZ => transform.localEulerAngles.z;
        public float Alpha => alpha;
        public string Text => tmpText != null ? tmpText.text : string.Empty;
        public string FontName => tmpText != null && tmpText.font != null ? tmpText.font.name : string.Empty;
        public bool IsEnabled => tmpText != null ? tmpText.enabled : isEnabled;

        public void Initialize()
        {
            EnsureText();
            SetAlpha(alpha);
            SetEnabled(true);
        }

        public void SetFont(Font font, int fontSize)
        {
            EnsureText();
            if (font == null || tmpText == null) { return; }

            DestroyCurrentFontAsset();

            fontAsset = TMP_FontAsset.CreateFontAsset(font);
            if (fontAsset == null)
            {
                Debug.LogWarning($"\u3010TextGuideObject\u3011Could not create TMP font asset from '{font.name}'.");
                return;
            }

            fontAsset.name = font.name;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            tmpText.font = fontAsset;
            tmpText.fontSize = Mathf.Max(1, fontSize);
            tmpText.ForceMeshUpdate(true, true);

            SetAlpha(alpha);
        }

        public void SetText(string value)
        {
            EnsureText();
            if (tmpText == null) { return; }

            tmpText.text = value ?? string.Empty;
            tmpText.ForceMeshUpdate(true, true);
        }

        public void SetLocalPosition(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetScale(float scale)
        {
            float clamped = Mathf.Max(0.001f, scale);
            transform.localScale = Vector3.one * clamped;
        }

        public void SetRotation(float rotationZ)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        public void SetAlpha(float value)
        {
            alpha = Mathf.Clamp01(value);

            EnsureText();
            ApplyAlpha();
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            EnsureText();
            DisableLegacyTextMeshRendering();

            if (tmpText != null)
            {
                tmpText.enabled = enabled;
            }
        }

        void EnsureText()
        {
            DisableLegacyTextMeshRendering();

            if (tmpText != null)
            {
                ApplyTextDefaults();
                ApplyAlpha();
                tmpText.enabled = isEnabled;
                return;
            }

            DisableOwnedCanvas();
            tmpText = GetOwnedTmpText();
            if (tmpText == null)
            {
                var textObject = new GameObject(TmpObjectName, typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro));
                textObject.transform.SetParent(transform, false);
                textObject.transform.localPosition = Vector3.zero;
                textObject.transform.localRotation = Quaternion.identity;
                textObject.transform.localScale = Vector3.one;

                tmpText = textObject.GetComponent<TextMeshPro>();

                var renderer = textObject.GetComponent<MeshRenderer>();
                renderer.sortingOrder = sortingOrder;
            }

            ApplyTextDefaults();
            ApplyAlpha();
            tmpText.enabled = isEnabled;
        }

        TextMeshPro GetOwnedTmpText()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name != TmpObjectName) { continue; }

                return child.GetComponent<TextMeshPro>();
            }

            return null;
        }

        void DisableOwnedCanvas()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var canvas = transform.GetChild(i).GetComponent<Canvas>();
                if (canvas == null) { continue; }

                canvas.enabled = false;
            }
        }

        void ApplyTextDefaults()
        {
            if (tmpText == null) { return; }

            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.enableWordWrapping = false;
            tmpText.richText = false;
            tmpText.rectTransform.sizeDelta = textAreaSize;

            if (tmpText.font == null && TMP_Settings.defaultFontAsset != null)
            {
                tmpText.font = TMP_Settings.defaultFontAsset;
            }

            var renderer = tmpText.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }

        void ApplyAlpha()
        {
            if (tmpText == null) { return; }

            var color = tmpText.color;
            color.a = alpha;
            tmpText.color = color;
        }

        void OnDestroy()
        {
            DestroyCurrentFontAsset();
        }

        void DestroyCurrentFontAsset()
        {
            if (fontAsset == null) { return; }

            if (tmpText != null && tmpText.font == fontAsset)
            {
                tmpText.font = null;
            }

            if (Application.isPlaying)
            {
                Destroy(fontAsset);
            }
            else
            {
                DestroyImmediate(fontAsset);
            }

            fontAsset = null;
        }

        void DisableLegacyTextMeshRendering()
        {
            if (legacyTextMesh == null)
            {
                legacyTextMesh = GetComponent<TextMesh>();
            }

            if (legacyMeshRenderer == null)
            {
                legacyMeshRenderer = GetComponent<MeshRenderer>();
            }

            if (legacyTextMesh != null)
            {
                legacyTextMesh.text = string.Empty;
            }

            if (legacyMeshRenderer != null)
            {
                legacyMeshRenderer.enabled = false;
            }
        }
    }
}
