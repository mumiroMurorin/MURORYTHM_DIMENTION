using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class ColoredImageOutline : BaseMeshEffect
{
    const string ShaderName = "UI/Colored Image Outline";
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    static readonly int BlurWidthId = Shader.PropertyToID("_BlurWidth");
    static readonly int BlurSamplesId = Shader.PropertyToID("_BlurSamples");
    static readonly int AlphaThresholdId = Shader.PropertyToID("_AlphaThreshold");
    static readonly int SpriteUVRectId = Shader.PropertyToID("_SpriteUVRect");

    [SerializeField] Shader outlineShader;
    [SerializeField] Color outlineColor = Color.white;
    [SerializeField, Min(0f)] float outlineWidth = 2f;
    [SerializeField, Min(0f)] float blurWidth;
    [SerializeField, Range(1, 4)] int blurSamples = 2;
    [SerializeField, Range(0f, 1f)] float alphaThreshold = 0.01f;

    Material runtimeMaterial;
    Material originalMaterial;

    void Reset()
    {
        CacheShader();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheShader();
        ApplyMaterial();
    }

    protected override void OnDisable()
    {
        RestoreMaterial();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        RestoreMaterial();
        base.OnDestroy();
    }

    void OnValidate()
    {
        CacheShader();

        if (!isActiveAndEnabled) { return; }

        ApplyMaterial();
        UpdateMaterialProperties();
        graphic?.SetVerticesDirty();
    }

    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        UpdateMaterialProperties();
    }

    public void SetOutlineWidth(float width)
    {
        outlineWidth = Mathf.Max(0f, width);
        UpdateMaterialProperties();
        graphic?.SetVerticesDirty();
    }

    public void SetBlurWidth(float width)
    {
        blurWidth = Mathf.Max(0f, width);
        UpdateMaterialProperties();
        graphic?.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        float effectWidth = GetEffectWidth();
        if (!IsActive() || effectWidth <= 0f || vertexHelper.currentVertCount == 0) { return; }

        Rect rect = graphic.rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f) { return; }

        UIVertex vertex = default;
        Texture texture = graphic.mainTexture;
        float uvExpandX = texture != null && texture.width > 0 ? effectWidth / texture.width : 0f;
        float uvExpandY = texture != null && texture.height > 0 ? effectWidth / texture.height : 0f;

        for (int i = 0; i < vertexHelper.currentVertCount; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);

            Vector3 position = vertex.position;
            Vector2 uv = vertex.uv0;

            if (Mathf.Approximately(position.x, rect.xMin))
            {
                position.x -= effectWidth;
                uv.x -= uvExpandX;
            }
            else if (Mathf.Approximately(position.x, rect.xMax))
            {
                position.x += effectWidth;
                uv.x += uvExpandX;
            }

            if (Mathf.Approximately(position.y, rect.yMin))
            {
                position.y -= effectWidth;
                uv.y -= uvExpandY;
            }
            else if (Mathf.Approximately(position.y, rect.yMax))
            {
                position.y += effectWidth;
                uv.y += uvExpandY;
            }

            vertex.position = position;
            vertex.uv0 = uv;
            vertexHelper.SetUIVertex(vertex, i);
        }
    }

    void ApplyMaterial()
    {
        if (graphic == null) { return; }

        if (runtimeMaterial == null)
        {
            if (outlineShader == null)
            {
                Debug.LogWarning($"[{nameof(ColoredImageOutline)}] Shader was not found: {ShaderName}", this);
                return;
            }

            originalMaterial = graphic.material;
            runtimeMaterial = new Material(outlineShader)
            {
                name = $"{ShaderName} ({gameObject.name})",
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        UpdateMaterialProperties();

        if (graphic.material != runtimeMaterial)
        {
            graphic.material = runtimeMaterial;
            graphic.SetMaterialDirty();
        }
    }

    void CacheShader()
    {
        if (outlineShader != null) { return; }

        outlineShader = Shader.Find(ShaderName);
    }

    void UpdateMaterialProperties()
    {
        if (runtimeMaterial == null) { return; }

        runtimeMaterial.SetColor(OutlineColorId, outlineColor);
        runtimeMaterial.SetFloat(OutlineWidthId, outlineWidth);
        runtimeMaterial.SetFloat(BlurWidthId, blurWidth);
        runtimeMaterial.SetFloat(BlurSamplesId, blurSamples);
        runtimeMaterial.SetFloat(AlphaThresholdId, alphaThreshold);
        runtimeMaterial.SetVector(SpriteUVRectId, GetSpriteUVRect());

        if (graphic != null)
        {
            graphic.SetMaterialDirty();
        }
    }

    Vector4 GetSpriteUVRect()
    {
        if (graphic is Image image && image.overrideSprite != null && image.overrideSprite.texture != null)
        {
            Sprite sprite = image.overrideSprite;
            Texture texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            return new Vector4(
                textureRect.xMin / texture.width,
                textureRect.yMin / texture.height,
                textureRect.xMax / texture.width,
                textureRect.yMax / texture.height);
        }

        return new Vector4(0f, 0f, 1f, 1f);
    }

    float GetEffectWidth()
    {
        return outlineWidth + blurWidth;
    }

    void RestoreMaterial()
    {
        if (graphic != null && graphic.material == runtimeMaterial)
        {
            graphic.material = originalMaterial;
            graphic.SetMaterialDirty();
        }

        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }

            runtimeMaterial = null;
        }

        originalMaterial = null;
    }
}
