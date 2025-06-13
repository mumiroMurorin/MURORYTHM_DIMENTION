using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObject_SpaceHoldMesh : NoteObject<NoteData_SpaceHoldMesh>
{
    [Header("meshのマテリアル(未判定時)")]
    [SerializeField] Material meshMaterialDefault;
    [Header("meshのマテリアル(タッチ時)")]
    [SerializeField] Material meshMaterialTouching;
    [Header("meshのマテリアル(非タッチ時)")]
    [SerializeField] Material meshMaterialUntouching;

    NoteData_SpaceHoldMesh noteData;
    List<MeshRenderer> meshRenderers;

    List<int> judgeRange = new List<int>();
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

    public ISliderInputGetter SliderInput { get; set; }

    public ITimeGetter Timer { get; set; }
}

