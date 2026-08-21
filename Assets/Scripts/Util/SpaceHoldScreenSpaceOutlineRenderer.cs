using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SpaceHoldScreenSpaceOutlineRenderer : MonoBehaviour
{
    [SerializeField] bool enableOutline = true;
    [SerializeField] LayerMask targetLayerMask;
    [SerializeField] string fallbackTargetLayerName = "SpaceHoldScreenOutline";
    [SerializeField] Shader idRenderShader;
    [SerializeField] Shader allIdRenderShader;
    [SerializeField] Shader compositeShader;
    [SerializeField] Color outlineColor = Color.white;
    [SerializeField] bool enableOccludedOutline = false;
    [SerializeField] Color occludedOutlineColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField, Range(1, 4)] int outlineThickness = 1;
    [SerializeField, Range(1, 4)] int occludedOutlineThickness = 1;
    [SerializeField, Range(0.001f, 1f)] float allCoverageStep = 0.25f;
    [SerializeField] OutlineDebugView debugView = OutlineDebugView.Composite;
    [SerializeField, Range(1, 4)] int downSample = 1;

    Camera mainCamera;
    Camera outlineCamera;
    RenderTexture visibleOutlineTexture;
    RenderTexture allOutlineTexture;
    Material compositeMaterial;

    void OnEnable()
    {
        mainCamera = GetComponent<Camera>();
        CreateResources();
    }

    void OnDisable()
    {
        ReleaseResources();
    }

    void OnDestroy()
    {
        ReleaseResources();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!enableOutline || !CreateResources())
        {
            Graphics.Blit(source, destination);
            return;
        }

        EnsureRenderTexture(source.width, source.height);
        RenderOutlineIdTexture();

        compositeMaterial.SetTexture("_VisibleOutlineTex", visibleOutlineTexture);
        compositeMaterial.SetTexture("_AllOutlineTex", allOutlineTexture);
        compositeMaterial.SetColor("_OutlineColor", outlineColor);
        compositeMaterial.SetColor("_OccludedOutlineColor", enableOccludedOutline ? occludedOutlineColor : Color.clear);
        compositeMaterial.SetFloat("_OutlineThickness", outlineThickness);
        compositeMaterial.SetFloat("_OccludedOutlineThickness", occludedOutlineThickness);
        compositeMaterial.SetFloat("_DebugViewMode", (int)debugView);
        compositeMaterial.SetVector("_OutlineTexelSize", new Vector4(
            1f / visibleOutlineTexture.width,
            1f / visibleOutlineTexture.height,
            visibleOutlineTexture.width,
            visibleOutlineTexture.height));

        Graphics.Blit(source, destination, compositeMaterial);
    }

    bool CreateResources()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        if (idRenderShader == null)
        {
            idRenderShader = Shader.Find("Hidden/SpaceHold/ScreenSpaceOutlineId");
        }

        if (allIdRenderShader == null)
        {
            allIdRenderShader = Shader.Find("Hidden/SpaceHold/ScreenSpaceOutlineCoverageAlways");
        }

        if (compositeShader == null)
        {
            compositeShader = Shader.Find("Hidden/SpaceHold/ScreenSpaceOutlineComposite");
        }

        if (idRenderShader == null || allIdRenderShader == null || compositeShader == null)
        {
            return false;
        }

        if (targetLayerMask.value == 0)
        {
            int layer = LayerMask.NameToLayer(fallbackTargetLayerName);
            if (0 <= layer && layer <= 31)
            {
                targetLayerMask = 1 << layer;
            }
        }

        if (compositeMaterial == null)
        {
            compositeMaterial = new Material(compositeShader);
        }

        if (outlineCamera == null)
        {
            GameObject cameraObject = new GameObject("SpaceHoldScreenSpaceOutlineCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            outlineCamera = cameraObject.AddComponent<Camera>();
            outlineCamera.enabled = false;
        }

        return mainCamera != null && outlineCamera != null && compositeMaterial != null;
    }

    void EnsureRenderTexture(int sourceWidth, int sourceHeight)
    {
        int scale = Mathf.Max(1, downSample);
        int width = Mathf.Max(1, sourceWidth / scale);
        int height = Mathf.Max(1, sourceHeight / scale);

        if (visibleOutlineTexture != null &&
            allOutlineTexture != null &&
            visibleOutlineTexture.width == width &&
            visibleOutlineTexture.height == height &&
            allOutlineTexture.width == width &&
            allOutlineTexture.height == height)
        {
            return;
        }

        ReleaseRenderTexture();

        visibleOutlineTexture = CreateOutlineRenderTexture(width, height, "SpaceHoldScreenSpaceVisibleOutlineId", 24);
        allOutlineTexture = CreateOutlineRenderTexture(width, height, "SpaceHoldScreenSpaceAllOutlineCoverage", 0, RenderTextureFormat.ARGBHalf);
    }

    RenderTexture CreateOutlineRenderTexture(int width, int height, string textureName, int depth)
    {
        return CreateOutlineRenderTexture(width, height, textureName, depth, RenderTextureFormat.ARGB32);
    }

    RenderTexture CreateOutlineRenderTexture(int width, int height, string textureName, int depth, RenderTextureFormat format)
    {
        RenderTexture texture = new RenderTexture(width, height, depth, format);
        texture.name = textureName;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Create();
        return texture;
    }

    void RenderOutlineIdTexture()
    {
        RenderOutlineIdTexture(visibleOutlineTexture, idRenderShader);
        Shader.SetGlobalFloat("_CoverageStep", allCoverageStep);
        RenderOutlineIdTexture(allOutlineTexture, allIdRenderShader);
    }

    void RenderOutlineIdTexture(RenderTexture targetTexture, Shader shader)
    {
        if (targetTexture == null || shader == null) { return; }

        outlineCamera.CopyFrom(mainCamera);
        outlineCamera.enabled = false;
        outlineCamera.clearFlags = CameraClearFlags.SolidColor;
        outlineCamera.backgroundColor = Color.clear;
        outlineCamera.cullingMask = targetLayerMask;
        outlineCamera.targetTexture = targetTexture;

        outlineCamera.RenderWithShader(shader, string.Empty);
        outlineCamera.targetTexture = null;
    }

    void ReleaseResources()
    {
        ReleaseRenderTexture();

        if (compositeMaterial != null)
        {
            DestroyObject(compositeMaterial);
            compositeMaterial = null;
        }

        if (outlineCamera != null)
        {
            DestroyObject(outlineCamera.gameObject);
            outlineCamera = null;
        }
    }

    void ReleaseRenderTexture()
    {
        ReleaseRenderTexture(ref visibleOutlineTexture);
        ReleaseRenderTexture(ref allOutlineTexture);
    }

    void ReleaseRenderTexture(ref RenderTexture texture)
    {
        if (texture == null) { return; }

        texture.Release();
        DestroyObject(texture);
        texture = null;
    }

    void DestroyObject(Object target)
    {
        if (target == null) { return; }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    enum OutlineDebugView
    {
        Composite = 0,
        VisibleEdge = 1,
        AllEdge = 2,
        OccludedEdge = 3,
        AllCoverageTexture = 4
    }
}
