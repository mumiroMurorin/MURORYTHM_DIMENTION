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
    [SerializeField] Material meshMaterialDefault;

    NoteData_SpaceHoldMesh noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldMesh data)
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

    public override void SetActive(bool isVisible) { }

}
