using TMPro;
using UnityEngine;

public class WorldLoopingClippedTMPText : MonoBehaviour
{
    [SerializeField] TextMeshPro primaryText;
    [SerializeField] Transform clipRoot;
    [SerializeField] Vector2 clipCenterOffset;
    [SerializeField] Vector2 clipSize = new Vector2(8f, 2f);
    [SerializeField] Vector2 clipSoftness = new Vector2(0.05f, 0.05f);
    [SerializeField] bool loopIfOverflow = true;
    [SerializeField] float loopSpeed = 1f;
    [SerializeField] float loopGap = 2f;
    [SerializeField] bool drawClipGizmo = true;
    [SerializeField] Color clipGizmoColor = new Color(0f, 0.8f, 1f, 0.8f);

    TextMeshPro secondaryText;
    bool overrideRenderQueue;
    int renderQueue;
    MaterialPropertyBlock propertyBlock;
    Vector3 primaryInitialLocalPosition;
    bool hasPrimaryInitialLocalPosition;
    float textWidth;
    float loopDistance;
    bool isLooping;

    static readonly int ClipCenterId = Shader.PropertyToID("_ClipCenter");
    static readonly int ClipRightId = Shader.PropertyToID("_ClipRight");
    static readonly int ClipUpId = Shader.PropertyToID("_ClipUp");
    static readonly int ClipSizeId = Shader.PropertyToID("_ClipSize");
    static readonly int ClipSoftnessId = Shader.PropertyToID("_ClipSoftness");

    Transform ClipRoot => clipRoot != null ? clipRoot : transform;
    Vector3 ClipCenter => ClipRoot.position + ClipRoot.right * clipCenterOffset.x + ClipRoot.up * clipCenterOffset.y;

    public void SetText(string value)
    {
        if (primaryText == null)
        {
            Debug.LogWarning("[WorldLoopingClippedTMPText] Primary TextMeshPro is not set.");
            return;
        }

        CachePrimaryInitialLocalPosition();
        WarnIfClipRootMovesWithPrimaryText();

        primaryText.text = value;
        ApplyTextSettings(primaryText);
        primaryText.ForceMeshUpdate();

        textWidth = Mathf.Max(primaryText.textBounds.size.x, 0f);
        loopDistance = textWidth + loopGap;
        isLooping = loopIfOverflow && textWidth > clipSize.x && loopDistance > 0.001f;

        if (isLooping)
        {
            EnsureSecondaryText();
        }

        if (secondaryText != null)
        {
            secondaryText.text = value;
            ApplyTextSettings(secondaryText);
            secondaryText.ForceMeshUpdate();
            secondaryText.transform.localPosition = isLooping ? primaryInitialLocalPosition + new Vector3(loopDistance, 0f, 0f) : primaryInitialLocalPosition;
            secondaryText.gameObject.SetActive(isLooping);
        }

        primaryText.transform.localPosition = primaryInitialLocalPosition;

        ApplyClipProperties(primaryText);
        ApplyClipProperties(secondaryText);
    }

    public void SetRenderQueue(int value)
    {
        overrideRenderQueue = true;
        renderQueue = value;

        ApplyRenderQueue(primaryText);
        ApplyRenderQueue(secondaryText);
    }

    void Update()
    {
        if (!isLooping) { return; }

        float delta = loopSpeed * Time.deltaTime;
        MoveLoopText(primaryText, delta);
        MoveLoopText(secondaryText, delta);
    }

    void LateUpdate()
    {
        // 親オブジェクトが動いても、ワールド上の切り抜き範囲が追従するように毎フレーム更新する。
        ApplyClipProperties(primaryText);
        ApplyClipProperties(secondaryText);
    }

    void MoveLoopText(TextMeshPro target, float delta)
    {
        if (target == null) { return; }

        Vector3 localPosition = target.transform.localPosition;
        localPosition.x -= delta;
        if (localPosition.x <= -loopDistance)
        {
            localPosition.x += loopDistance * 2f;
        }

        target.transform.localPosition = localPosition;
    }

    void CachePrimaryInitialLocalPosition()
    {
        if (hasPrimaryInitialLocalPosition) { return; }

        primaryInitialLocalPosition = primaryText.transform.localPosition;
        hasPrimaryInitialLocalPosition = true;
    }

    void EnsureSecondaryText()
    {
        if (secondaryText != null) { return; }

        // ループに必要な2枚目のテキストは、Primaryの見た目をそのまま複製して内部生成する。
        secondaryText = Instantiate(primaryText, primaryText.transform.parent);
        secondaryText.gameObject.name = $"{primaryText.gameObject.name}_LoopCopy";
        secondaryText.transform.localRotation = primaryText.transform.localRotation;
        secondaryText.transform.localScale = primaryText.transform.localScale;

        WorldLoopingClippedTMPText copiedController = secondaryText.GetComponent<WorldLoopingClippedTMPText>();
        if (copiedController != null)
        {
            Destroy(copiedController);
        }
    }

    void ApplyTextSettings(TextMeshPro target)
    {
        if (target == null) { return; }

        target.enableWordWrapping = false;
        target.overflowMode = TextOverflowModes.Overflow;

        Material material = CreateTextMaterial(target);
        if (material != null)
        {
            ApplyRenderQueue(material);
            target.fontMaterial = material;
            target.UpdateMeshPadding();
        }
    }

    Material CreateTextMaterial(TextMeshPro target)
    {
        Shader shader = Shader.Find("Notes/Stages/WorldTMPHorizontalClip");
        if (shader == null)
        {
            Debug.LogWarning("[WorldLoopingClippedTMPText] Clip shader is not found: Notes/Stages/WorldTMPHorizontalClip");
            return null;
        }

        Material sourceMaterial = target.fontSharedMaterial;
        if (sourceMaterial == null)
        {
            Debug.LogWarning("[WorldLoopingClippedTMPText] TextMeshPro material is not set.");
            return null;
        }

        // TMPに設定済みのUnderlayやBevelなどを保ったまま、クリップ対応Shaderへ差し替える。
        Material material = new Material(sourceMaterial);
        if (material.shader == shader)
        {
            return material;
        }

        material.shader = shader;
        if (target.font != null && target.font.atlasTexture != null)
        {
            material.SetTexture("_MainTex", target.font.atlasTexture);
        }

        // Shader変更後にTMP固有の比率を再計算し、UnderlayやOutlineの幅を標準Shaderと揃える。
        ShaderUtilities.UpdateShaderRatios(material);

        return material;
    }

    void ApplyRenderQueue(TextMeshPro target)
    {
        if (!overrideRenderQueue || target == null || target.fontMaterial == null) { return; }

        target.fontMaterial.renderQueue = renderQueue;
    }

    void ApplyRenderQueue(Material material)
    {
        if (!overrideRenderQueue || material == null) { return; }

        material.renderQueue = renderQueue;
    }

    void ApplyClipProperties(TextMeshPro target)
    {
        if (target == null) { return; }

        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer == null) { return; }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        Transform root = ClipRoot;
        propertyBlock.SetVector(ClipCenterId, ClipCenter);
        propertyBlock.SetVector(ClipRightId, root.right);
        propertyBlock.SetVector(ClipUpId, root.up);
        propertyBlock.SetVector(ClipSizeId, clipSize);
        propertyBlock.SetVector(ClipSoftnessId, clipSoftness);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    void WarnIfClipRootMovesWithPrimaryText()
    {
        if (clipRoot != null || primaryText == null || primaryText.transform != transform) { return; }

        Debug.LogWarning("[WorldLoopingClippedTMPText] ClipRoot is not set and this component is attached to PrimaryText. Set a fixed parent Transform to ClipRoot.");
    }

    void OnDrawGizmos()
    {
        if (!drawClipGizmo) { return; }

        Transform root = ClipRoot;
        Vector3 center = root.position + root.right * clipCenterOffset.x + root.up * clipCenterOffset.y;
        Vector3 right = root.right * clipSize.x * 0.5f;
        Vector3 up = root.up * clipSize.y * 0.5f;

        Vector3 leftTop = center - right + up;
        Vector3 rightTop = center + right + up;
        Vector3 rightBottom = center + right - up;
        Vector3 leftBottom = center - right - up;

        Gizmos.color = clipGizmoColor;
        Gizmos.DrawLine(leftTop, rightTop);
        Gizmos.DrawLine(rightTop, rightBottom);
        Gizmos.DrawLine(rightBottom, leftBottom);
        Gizmos.DrawLine(leftBottom, leftTop);
    }
}
