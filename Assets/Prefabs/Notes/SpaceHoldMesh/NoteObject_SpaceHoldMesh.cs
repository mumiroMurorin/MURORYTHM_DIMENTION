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
    [Header("meshのマテリアル(未判定時)")]
    [SerializeField] Material meshMaterialDefault;
    [Header("meshのマテリアル(ホールド時)")]
    [SerializeField] Material meshMaterialHolding;
    [Header("meshのマテリアル(非ホールド時)")]
    [SerializeField] Material meshMaterialUnholding;

    NoteData_SpaceHoldMesh noteData;
    List<MeshRenderer> meshRenderers;

    Vector2[] judgeRange;
    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldMesh data)
    {
        noteData = data;

        // マテリアルの設定
        meshRenderers = new List<MeshRenderer>();
        foreach (Transform child in this.gameObject.transform)
        {
            if (child.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderers.Add(meshRenderer);
                meshRenderer.material = meshMaterialDefault;
            }
        }
    }

    private void Update()
    {
        if (noteData == null) { return; }
        if (noteData.Timer == null) { return; }
        if (noteData.Timer.Time < noteData.Timing) { return; }

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
        if (!noteData.OptionGetter.IsAutoMode) { SetHoldStatus(IsInSpaceRange()); }
        else { SetHoldStatus(true); }
        return;
    }

    /// <summary>
    /// ノーツ範囲内に手があるか判定
    /// </summary>
    /// <returns></returns>
    private bool IsInSpaceRange()
    {
        if (noteData.SpaceInput == null) { return false; }
        if (noteData.Timer == null) { return false; }

        // 右手の判定
        int rightCount = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand).Count;
        if (rightCount < 2) { return false; }

        var rightPos1 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand)[rightCount - 1].Pos;
        var rightPos2 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand)[rightCount - 2].Pos;
        bool isRightIn = IsSegmentIntersectingOrInsidePolygon(rightPos1, rightPos2, judgeRange);

        // 左手の判定
        int leftCount = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand).Count;
        if (leftCount < 2) { return false; }

        var leftPos1 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.LeftHand)[leftCount - 1].Pos;
        var leftPos2 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.LeftHand)[leftCount - 2].Pos;
        bool isLeftIn = IsSegmentIntersectingOrInsidePolygon(leftPos1, leftPos2, judgeRange);

        return isRightIn || isLeftIn;
    }

    /// <summary>
    /// ホールドされているかどうかでマテリアルを変更する
    /// </summary>
    /// <param name="isTouching"></param>
    public void SetHoldStatus(bool isHolding)
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.material = isHolding ? meshMaterialHolding : meshMaterialUnholding;
        }
    }

    public override void SetVisible(bool isVisible)
    {

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

    public ITimeGetter Timer { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public INoteSpawnDataOptionHolder OptionGetter { get; set; }
}

