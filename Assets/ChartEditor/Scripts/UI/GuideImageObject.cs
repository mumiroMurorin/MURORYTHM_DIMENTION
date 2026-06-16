using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GuideImageObject : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] int sortingOrder = -100;
        [SerializeField, Range(0f, 1f)] float alpha = 0.6f;

        public bool IsSettingImage => spriteRenderer.sprite != null;
        public Vector3 LocalPosition => transform.localPosition;
        public float Scale => transform.localScale.x;
        public float RotationZ => transform.localEulerAngles.z;
        public float Alpha => alpha;

        public void Initialize()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingOrder = sortingOrder;

            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer == null) { Initialize(); }
            spriteRenderer.sprite = sprite;
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

            if (spriteRenderer == null) { Initialize(); }

            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        public void SetEnabled(bool enabled)
        {
            spriteRenderer.enabled = enabled;
        }
    }
}
