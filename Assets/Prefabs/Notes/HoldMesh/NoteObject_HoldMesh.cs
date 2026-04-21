using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using JudgementUtil;
using JudgementUtil.Hold;
using System.Linq;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObject_HoldMesh : NoteObject<NoteData_HoldMesh>
{
    [Header("meshのマテリアル(未判定時)")]
    [SerializeField] Material meshMaterialDefault;
    [Header("meshのマテリアル(タッチ時)")]
    [SerializeField] Material meshMaterialTouching;
    [Header("meshのマテリアル(非タッチ時)")]
    [SerializeField] Material meshMaterialUntouching;

    NoteData_HoldMesh noteData;
    List<MeshRenderer> meshRenderers;

    List<int> judgeRange = new List<int>();

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldMesh data)
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

    /// <summary>
    /// ホールドメッシュはアクティブ化の影響を受けない
    /// </summary>
    /// <param name="isVisible"></param>
    public override void SetActive(bool isVisible) { }

    private void Update()
    {
        if (noteData == null) { return; }
        if (noteData.Timer == null) { return; }
        if (noteData.Timer.Time < noteData.Timing) { return; }

        // 判定範囲の更新
        judgeRange = HoldJudgement.GetJudgeRange(noteData.TimeToRanges, noteData.Timer.Time);

        UpdateTouchStatus();
    }

    /// <summary>
    /// タッチ判定を更新する
    /// </summary>
    private void UpdateTouchStatus()
    {
        if (noteData.Timer == null) { return; }
        if (judgeRange == null) { return; }

        // 判定範囲内のスライダー入力を調べる
        if (!noteData.OptionGetter.IsAutoMode) { SetTouchStatus(GroundJudgement.IsTouchingSlider(noteData.SliderInput, judgeRange.ToArray())); }
        else { SetTouchStatus(true); }
        return;
    }

    /// <summary>
    /// タッチされているかどうかでマテリアルを変更する
    /// </summary>
    /// <param name="isTouching"></param>
    public void SetTouchStatus(bool isTouching)
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.material = isTouching ? meshMaterialTouching : meshMaterialUntouching;
        }
    }

    /// <summary>
    /// ノーツを機能停止する
    /// </summary>
    private void SetDisable()
    {
        this.gameObject.SetActive(false);
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
/// </summary>
public class NoteData_HoldMesh : INoteData
{
    public NoteType NoteType => NoteType.HoldMesh;

    public float Timing { get; set; }

    public List<TimeToRange> TimeToRanges { get; set; }

    public ISliderInputGetter SliderInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public INoteSpawnDataOptionGetter OptionGetter { get; set; }

    public float GroundEndZ { get; set; }
}

