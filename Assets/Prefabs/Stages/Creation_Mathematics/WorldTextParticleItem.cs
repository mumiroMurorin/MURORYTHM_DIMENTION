using System;
using TMPro;
using UnityEngine;

public class WorldTextParticleItem : MonoBehaviour
{
    Transform cachedTransform;
    [SerializeField] Transform spriteRoot;
    [SerializeField] Transform textRoot;
    SpriteRenderer spriteRenderer;
    TextMeshPro text;
    Material defaultSpriteMaterial;
    Action<WorldTextParticleItem> returnToPool;

    Vector3 startLocalPosition;
    Vector3 velocity;
    Color startColor;
    float lifeTime;
    float fadeInDuration;
    float fadeOutDuration;
    float elapsedTime;
    bool useUnscaledTime;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        EnsureComponents();
    }

    public void EnsureComponents()
    {
        cachedTransform = transform;
        spriteRenderer = spriteRoot != null ? spriteRoot.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
        {
            GameObject spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(transform, false);
            spriteRoot = spriteObject.transform;
            spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        }
        else if (spriteRoot == null)
        {
            spriteRoot = spriteRenderer.transform;
        }

        if (spriteRenderer != null && defaultSpriteMaterial == null)
        {
            defaultSpriteMaterial = spriteRenderer.sharedMaterial;
        }

        text = textRoot != null ? textRoot.GetComponent<TextMeshPro>() : GetComponentInChildren<TextMeshPro>(true);
        if (text == null)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(transform, false);
            textRoot = textObject.transform;
            text = textObject.AddComponent<TextMeshPro>();
        }
        else if (textRoot == null)
        {
            textRoot = text.transform;
        }
    }

    public void Initialize(
        Sprite sprite,
        string message,
        bool useText,
        Vector3 localPosition,
        Vector3 moveDirection,
        float moveSpeed,
        float lifeTime,
        float fadeInDuration,
        float fadeOutDuration,
        Color color,
        TMP_FontAsset font,
        float fontSize,
        float localScale,
        Material spriteMaterial,
        int renderQueue,
        string sortingLayerName,
        int sortingOrder,
        bool useUnscaledTime,
        Action<WorldTextParticleItem> returnToPool)
    {
        EnsureComponents();

        this.returnToPool = returnToPool;
        this.lifeTime = Mathf.Max(0.01f, lifeTime);
        this.fadeInDuration = Mathf.Max(0f, fadeInDuration);
        this.fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        this.useUnscaledTime = useUnscaledTime;

        elapsedTime = 0f;
        startLocalPosition = localPosition;
        velocity = moveDirection.sqrMagnitude <= Mathf.Epsilon ? Vector3.zero : moveDirection.normalized * moveSpeed;
        startColor = color;

        ApplyContent(sprite, message, useText, font, fontSize, spriteMaterial);
        ApplyColor(0f);
        ApplySorting(sortingLayerName, sortingOrder);
        ApplyRenderQueue(renderQueue);

        cachedTransform.localPosition = startLocalPosition;
        cachedTransform.localRotation = Quaternion.identity;
        cachedTransform.localScale = Vector3.one * Mathf.Max(0.01f, localScale);

        IsActive = true;
        gameObject.SetActive(true);
        if (useText)
        {
            text.ForceMeshUpdate();
        }
    }

    private void Update()
    {
        if (!IsActive) { return; }

        elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        cachedTransform.localPosition = startLocalPosition + velocity * elapsedTime;
        ApplyColor(CalculateAlpha());

        if (elapsedTime >= lifeTime)
        {
            ReturnToPool();
        }
    }

    public void StopForPool()
    {
        if (!IsActive) { return; }

        IsActive = false;
        gameObject.SetActive(false);
    }

    float CalculateAlpha()
    {
        float alpha = startColor.a;

        if (fadeInDuration > 0f)
        {
            alpha *= Mathf.Clamp01(elapsedTime / fadeInDuration);
        }

        if (fadeOutDuration > 0f)
        {
            alpha *= Mathf.Clamp01((lifeTime - elapsedTime) / fadeOutDuration);
        }

        return alpha;
    }

    void ApplyContent(Sprite sprite, string message, bool useText, TMP_FontAsset font, float fontSize, Material spriteMaterial)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = !useText;
            spriteRenderer.sprite = useText ? null : sprite;
            spriteRenderer.gameObject.SetActive(!useText);
            spriteRenderer.sharedMaterial = spriteMaterial != null ? spriteMaterial : defaultSpriteMaterial;
        }

        if (text != null)
        {
            text.enabled = useText;
            text.gameObject.SetActive(useText);
            text.text = useText ? message : string.Empty;
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = Mathf.Max(0.01f, fontSize);
            text.richText = true;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
        }
    }

    void ApplyColor(float alpha)
    {
        Color color = WithAlpha(startColor, alpha);

        if (spriteRenderer != null && spriteRenderer.enabled)
        {
            spriteRenderer.color = color;
        }

        if (text != null && text.enabled)
        {
            text.color = color;
        }
    }

    void ApplySorting(string sortingLayerName, int sortingOrder)
    {
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
        }

        spriteRenderer.sortingOrder = sortingOrder;

        Renderer textRenderer = text != null ? text.GetComponent<Renderer>() : null;
        if (textRenderer == null) { return; }

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            textRenderer.sortingLayerName = sortingLayerName;
        }

        textRenderer.sortingOrder = sortingOrder;
    }

    void ApplyRenderQueue(int renderQueue)
    {
        if (renderQueue < 0) { return; }

        if (spriteRenderer != null && spriteRenderer.material != null)
        {
            spriteRenderer.material.renderQueue = renderQueue;
        }

        if (text != null && text.fontMaterial != null)
        {
            text.fontMaterial.renderQueue = renderQueue;
        }
    }

    Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    void ReturnToPool()
    {
        if (!IsActive) { return; }

        IsActive = false;
        gameObject.SetActive(false);
        returnToPool?.Invoke(this);
    }
}
