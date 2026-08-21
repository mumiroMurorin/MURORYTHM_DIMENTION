using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

/// <summary>
/// スペースホールドメッシュにアタッチされるクラス
/// </summary>
public class NoteObject_SpaceHoldMesh : NoteObject<NoteData_SpaceHoldMesh>
{
    [SerializeField] float judgementMarginRadius = 0.25f;
    [SerializeField] float judgementMarginTime = 0.1f;
    [SerializeField] float firstMarginTime = 0.1f;
    [Header("meshのマテリアル(未判定時)")]
    [SerializeField] Material meshMaterialDefaultInside;
    [SerializeField] Material meshMaterialDefaultOutside;
    [SerializeField] Material meshMaterialDefaultOutlineInside;
    [SerializeField] Material meshMaterialDefaultOutlineOutside;
    [SerializeField] Material meshMaterialDefaultShadow;
    [Header("meshのマテリアル(ホールド時)")]
    [SerializeField] Material meshMaterialHoldingInside;
    [SerializeField] Material meshMaterialHoldingOutside;
    [SerializeField] Material meshMaterialHoldingOutlineInside;
    [SerializeField] Material meshMaterialHoldingOutlineOutside;
    [SerializeField] Material meshMaterialHoldingShadow;
    [Header("meshのマテリアル(非ホールド時)")]
    [SerializeField] Material meshMaterialUnholdingInside;
    [SerializeField] Material meshMaterialUnholdingOutside;
    [SerializeField] Material meshMaterialUnholdingOutlineInside;
    [SerializeField] Material meshMaterialUnholdingOutlineOutside;
    [SerializeField] Material meshMaterialUnholdingShadow;
    [Header("判定範囲表示")]
    [SerializeField] SpaceHoldJudgementRangeMeshView judgementRangeView;

    NoteData_SpaceHoldMesh noteData;

    Vector2[] judgeRange;
    float holdingMarginCount;
    bool isVisibleByController = true;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldMesh data)
    {
        noteData = data;
        holdingMarginCount = firstMarginTime;
        SetJudgementRangeVisible(false);

        // マテリアルの設定
        data.MeshRendererAsset.SetMaterial(
            meshMaterialDefaultInside, meshMaterialDefaultOutside,
            meshMaterialDefaultOutlineInside, meshMaterialDefaultOutlineOutside,
            meshMaterialDefaultShadow);
        data.MeshRendererAsset.SetStencilId(data.StencilId);

        if (judgementRangeView != null)
        {
            judgementRangeView.SetShowJudgementRange(noteData.EnableJudgementRangeLine);
            judgementRangeView.Initialize(noteData.JudgementRangeLineParent);
        }
    }

    private void Update()
    {
        if (noteData == null) { SetJudgementRangeVisible(false); return; }
        if (noteData.Timer == null) { SetJudgementRangeVisible(false); return; }
        if (noteData.Timer.Time < noteData.Timing) { SetJudgementRangeVisible(false); return; }

        // マージンタイムの更新
        if (holdingMarginCount > 0f) { holdingMarginCount -= Time.deltaTime; }
        else { holdingMarginCount = 0; }

        // 判定範囲の更新
        judgeRange = InterpolatePointsByDepth(noteData.DepthToVertices, GetCurrentDepth());

        bool isInJudgementLineTiming = IsInJudgementLineTiming();
        UpdateJudgementRangeView(judgeRange, isInJudgementLineTiming);
        UpdateHoldStatus();
    }

    public override void SetActive(bool isVisible)
    {
        base.SetActive(isVisible);
        isVisibleByController = isVisible;

        if (!isVisible)
        {
            SetJudgementRangeVisible(false);
        }
    }

    public override bool ShouldLockVisibility(float currentDistance)
    {
        return currentDistance > EndChartDistance;
    }

    /// <summary>
    /// 範囲内判定を更新する
    /// </summary>
    private void UpdateHoldStatus()
    {
        if (noteData.Timer == null) { return; }
        if (judgeRange == null) { return; }

        // 判定範囲内のスライダー入力を調べる
        // プレイ時
        if (!noteData.OptionGetter.IsAutoMode) 
        {
            bool isInRange = noteData.SpaceInput.IsInSpaceRange(judgeRange, judgementMarginRadius);

            // マージンタイムの更新
            if (isInRange) { holdingMarginCount = judgementMarginTime; }

            // 範囲内 または マージンタイム中はホールド中にする
            SetHoldStatus(isInRange || holdingMarginCount > 0f);
        }
        // オートモード時
        else 
        { 
            SetHoldStatus(true);
        }
    }

    /// <summary>
    /// ホールドされているかどうかでマテリアルを変更する
    /// </summary>
    /// <param name="isTouching"></param>
    public void SetHoldStatus(bool isHolding)
    {
        if (isHolding)
        {
            noteData.MeshRendererAsset.SetMaterial(
                meshMaterialHoldingInside, meshMaterialHoldingOutside,
                meshMaterialHoldingOutlineInside, meshMaterialHoldingOutlineOutside,
                meshMaterialHoldingShadow);
        }
        else
        {
            noteData.MeshRendererAsset.SetMaterial(
                meshMaterialUnholdingInside, meshMaterialUnholdingOutside,
                meshMaterialUnholdingOutlineInside, meshMaterialUnholdingOutlineOutside,
                meshMaterialUnholdingShadow);
        }
    }

    private void UpdateJudgementRangeView(Vector2[] points, bool isInJudgementLineTiming)
    {
        bool isVisible = isVisibleByController && isInJudgementLineTiming;
        judgementRangeView?.UpdateRange(points, isVisible);
    }

    private bool IsInJudgementLineTiming()
    {
        if (noteData == null) { return false; }
        if (noteData.Timer == null) { return false; }
        if (noteData.TimeToVertices == null || noteData.TimeToVertices.Count == 0) { return false; }

        float currentTime = noteData.Timer.Time;
        float startTime = noteData.TimeToVertices[0].Timing;
        float endTime = noteData.TimeToVertices[^1].Timing;

        return startTime <= currentTime && currentTime <= endTime;
    }

    private float GetCurrentDepth()
    {
        if (noteData == null) { return 0f; }
        if (noteData.Timer == null) { return 0f; }
        if (noteData.PositionCalculator == null) { return 0f; }

        return noteData.PositionCalculator.GetPosition(noteData.Timer.Time) * noteData.NoteSpeed;
    }

    private void OnDisable()
    {
        SetJudgementRangeVisible(false);
    }

    private void OnDestroy()
    {
        noteData?.MeshRendererAsset?.DestroyMaterialInstances();
    }

    private void SetJudgementRangeVisible(bool isVisible)
    {
        judgementRangeView?.SetVisible(isVisible);
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
/// </summary>
public class NoteData_SpaceHoldMesh : INoteData
{
    public NoteType NoteType => NoteType.SpaceHoldMesh;

    public int HoldNumber { get; set; }

    public float Timing { get; set; }

    public List<TimeToVertices> TimeToVertices { get; set; }

    public List<DepthToVertices> DepthToVertices { get; set; }

    public INotePositionCalculator PositionCalculator { get; set; }

    public float NoteSpeed { get; set; }

    public HoldMeshRendererAsset MeshRendererAsset { get; set; }

    public ITimeGetter Timer { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public INoteSpawnDataOptionGetter OptionGetter { get; set; }

    public Transform JudgementRangeLineParent { get; set; }

    public bool EnableJudgementRangeLine { get; set; } = true;

    public int StencilId { get; set; }
}

public class HoldMeshRendererAsset
{
    static readonly int StencilRefPropertyId = Shader.PropertyToID("_StencilRef");
    static readonly int ScreenOutlineIdColorPropertyId = Shader.PropertyToID("_ScreenOutlineIdColor");
    const string ScreenOutlineMaskShaderName = "Hidden/SpaceHold/ScreenSpaceOutlineMask";

    public HoldMeshRendererAsset(
        MeshRenderer inside,
        MeshRenderer outside,
        MeshRenderer insideOutline,
        MeshRenderer outsideOutline,
        MeshRenderer shadow,
        MeshRenderer screenOutlineInsideMask,
        MeshRenderer screenOutlineOutsideMask)
    {
        InsideRenderer = inside;
        OutsideRenderer = outside;
        InsideOutlineRenderer = insideOutline;
        OutsideOutlineRenderer = outsideOutline;
        ShadowRenderer = shadow;
        ScreenOutlineInsideMaskRenderer = screenOutlineInsideMask;
        ScreenOutlineOutsideMaskRenderer = screenOutlineOutsideMask;
    }

    int stencilId = -1;
    readonly Dictionary<Material, Material> stencilMaterialCache = new Dictionary<Material, Material>();
    readonly Dictionary<Material, Material> screenOutlineMaskMaterialCache = new Dictionary<Material, Material>();
    readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    public MeshRenderer InsideRenderer { get; set; }

    public MeshRenderer OutsideRenderer { get; set; }

    public MeshRenderer InsideOutlineRenderer { get; set; }

    public MeshRenderer OutsideOutlineRenderer { get; set; }

    public MeshRenderer ShadowRenderer { get; set; }

    public MeshRenderer ScreenOutlineInsideMaskRenderer { get; set; }

    public MeshRenderer ScreenOutlineOutsideMaskRenderer { get; set; }

    public void SetMaterial(Material inside, Material outside, Material outlineInside, Material outlineOutside, Material shadow)
    {
        SetMaterialIfExists(InsideRenderer, inside);
        SetMaterialIfExists(OutsideRenderer, outside);
        SetMaterialIfExists(InsideOutlineRenderer, outlineInside);
        SetMaterialIfExists(OutsideOutlineRenderer, outlineOutside);
        SetMaterialIfExists(ShadowRenderer, shadow);
        SetScreenOutlineMaskMaterialIfExists(ScreenOutlineInsideMaskRenderer, inside);
        SetScreenOutlineMaskMaterialIfExists(ScreenOutlineOutsideMaskRenderer, outside);
    }

    public void SetStencilId(int id)
    {
        stencilId = Mathf.Clamp(id, 0, 255);
        ClearStencilMaterialCache();
        ApplyStencilIdIfExists(InsideRenderer);
        ApplyStencilIdIfExists(OutsideRenderer);
        ApplyStencilIdIfExists(InsideOutlineRenderer);
        ApplyStencilIdIfExists(OutsideOutlineRenderer);
    }

    public void SetScreenSpaceOutlineTarget(Color idColor, int layer)
    {
        ApplyScreenSpaceOutlineTargetIfExists(ScreenOutlineInsideMaskRenderer, idColor, layer);
        ApplyScreenSpaceOutlineTargetIfExists(ScreenOutlineOutsideMaskRenderer, idColor, layer);
    }

    private void SetMaterialIfExists(MeshRenderer meshRenderer, Material material)
    {
        if (meshRenderer == null) { return; }
        meshRenderer.sharedMaterial = GetStencilMaterial(material);
    }

    private void SetScreenOutlineMaskMaterialIfExists(MeshRenderer meshRenderer, Material material)
    {
        if (meshRenderer == null) { return; }
        meshRenderer.sharedMaterial = GetScreenOutlineMaskMaterial(material);
    }

    private void ApplyStencilIdIfExists(MeshRenderer meshRenderer)
    {
        if (meshRenderer == null) { return; }
        meshRenderer.sharedMaterial = GetStencilMaterial(meshRenderer.sharedMaterial);
    }

    private void ApplyScreenSpaceOutlineTargetIfExists(MeshRenderer meshRenderer, Color idColor, int layer)
    {
        if (meshRenderer == null) { return; }

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ScreenOutlineIdColorPropertyId, idColor);
        meshRenderer.SetPropertyBlock(propertyBlock);

        if (0 <= layer && layer <= 31)
        {
            meshRenderer.gameObject.layer = layer;
        }
    }

    private Material GetStencilMaterial(Material source)
    {
        if (source == null) { return null; }
        if (stencilId < 0) { return source; }
        if (!source.HasProperty(StencilRefPropertyId)) { return source; }

        if (stencilMaterialCache.TryGetValue(source, out Material cachedMaterial))
        {
            return cachedMaterial;
        }

        Material material = new Material(source);
        material.SetInt(StencilRefPropertyId, stencilId);
        stencilMaterialCache.Add(source, material);

        return material;
    }

    private Material GetScreenOutlineMaskMaterial(Material source)
    {
        if (source == null) { return null; }

        if (screenOutlineMaskMaterialCache.TryGetValue(source, out Material cachedMaterial))
        {
            return cachedMaterial;
        }

        Material material = new Material(source);
        Shader maskShader = Shader.Find(ScreenOutlineMaskShaderName);
        if (maskShader != null)
        {
            material.shader = maskShader;
        }

        screenOutlineMaskMaterialCache.Add(source, material);

        return material;
    }

    public void DestroyMaterialInstances()
    {
        ClearStencilMaterialCache();
        ClearScreenOutlineMaskMaterialCache();
    }

    private void ClearStencilMaterialCache()
    {
        foreach (Material material in stencilMaterialCache.Values)
        {
            if (material == null) { continue; }
            Object.Destroy(material);
        }

        stencilMaterialCache.Clear();
    }

    private void ClearScreenOutlineMaskMaterialCache()
    {
        foreach (Material material in screenOutlineMaskMaterialCache.Values)
        {
            if (material == null) { continue; }
            Object.Destroy(material);
        }

        screenOutlineMaskMaterialCache.Clear();
    }
}
