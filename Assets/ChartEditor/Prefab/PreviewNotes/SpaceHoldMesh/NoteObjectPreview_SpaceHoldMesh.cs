using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_SpaceHoldMesh : NoteObject<NoteData_SpaceHoldMesh>
{
    [Header("meshのマテリアル")]
    [SerializeField] Material meshMaterialInside;
    [SerializeField] Material meshMaterialOutside;
    [SerializeField] Material meshMaterialOutlineInside;
    [SerializeField] Material meshMaterialOutlineOutside;
    [SerializeField] Material meshMaterialShadow;

    NoteData_SpaceHoldMesh noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldMesh data)
    {
        noteData = data;

        // マテリアルの設定
        noteData.MeshRendererAsset.SetMaterial(
            meshMaterialInside, meshMaterialOutside,
            meshMaterialOutlineInside, meshMaterialOutlineOutside,
            meshMaterialShadow);
    }

    private void OnDestroy()
    {
        // SetMaterial がホールドごとに複製したマテリアルを破棄する (本編の NoteObject_SpaceHoldMesh と同じ後片付け)
        // プレビューは編集のたびに作り直すので、破棄しないとメモリが増え続ける
        noteData?.MeshRendererAsset?.DestroyMaterialInstances();
    }
}
