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
    [Header("meshのマテリアル(ホールド時)")]
    [SerializeField] Material meshMaterialHoldingInside;
    [SerializeField] Material meshMaterialHoldingOutside;
    [Header("meshのマテリアル(非ホールド時)")]
    [SerializeField] Material meshMaterialUnholdingInside;
    [SerializeField] Material meshMaterialUnholdingOutside;

    NoteData_SpaceHoldMesh noteData;

    Vector2[] judgeRange;
    float holdingMarginCount;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldMesh data)
    {
        noteData = data;
        holdingMarginCount = firstMarginTime;

        // マテリアルの設定
        data.MeshRendererAsset.SetMaterial(meshMaterialDefaultInside, meshMaterialDefaultOutside);
    }

    private void Update()
    {
        if (noteData == null) { return; }
        if (noteData.Timer == null) { return; }
        if (noteData.Timer.Time < noteData.Timing) { return; }

        // マージンタイムの更新
        if (holdingMarginCount > 0f) { holdingMarginCount -= Time.deltaTime; }
        else { holdingMarginCount = 0; }

        // 判定範囲の更新
        judgeRange = InterpolatePoints(noteData.TimeToVertices, noteData.Timer.Time);

        UpdateHoldStatus();
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
            noteData.MeshRendererAsset.SetMaterial(meshMaterialHoldingInside, meshMaterialHoldingOutside);
        }
        else
        {
            noteData.MeshRendererAsset.SetMaterial(meshMaterialUnholdingInside, meshMaterialUnholdingOutside);
        }
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
/// </summary>
public class NoteData_SpaceHoldMesh : INoteData
{
    public NoteType NoteType => NoteType.SpaceHoldMesh;

    public float Timing { get; set; }

    public List<TimeToVertices> TimeToVertices { get; set; }

    public HoldMeshRendererAsset MeshRendererAsset { get; set; }

    public ITimeGetter Timer { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public INoteSpawnDataOptionGetter OptionGetter { get; set; }
}

public class HoldMeshRendererAsset
{
    public HoldMeshRendererAsset(MeshRenderer inside, MeshRenderer outside)
    {
        InsideRenderer = inside;
        OutsideRenderer = outside;
    }

    public MeshRenderer InsideRenderer { get; set; }

    public MeshRenderer OutsideRenderer { get; set; }

    public void SetMaterial(Material inside, Material outside)
    {
        InsideRenderer.material = inside;
        OutsideRenderer.material = outside;
    }
}
