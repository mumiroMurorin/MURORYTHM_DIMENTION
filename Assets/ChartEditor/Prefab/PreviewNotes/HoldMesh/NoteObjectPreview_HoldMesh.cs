using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_HoldMesh : NoteObject<NoteData_HoldMesh>
{
    [Header("meshのマテリアル")]
    [SerializeField] Material meshMaterialDefault;

    NoteData_HoldMesh noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldMesh data)
    {
        noteData = data;

        // マテリアルの設定
        foreach (Transform child in this.gameObject.transform)
        {
            if (child.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.material = meshMaterialDefault;
            }
        }
    }
}
