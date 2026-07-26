using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

public class NoteObjectPreview_SpaceBreak : NoteObject<NoteData_SpaceBreak>
{
    [Header("Shadow Material")]
    [SerializeField] Material shadowMaterialDefault;

    NoteData_SpaceBreak noteData;

    /// <summary>
    /// Initialize.
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceBreak data)
    {
        noteData = data;
        SetShadowMaterial(shadowMaterialDefault);
    }

    public bool IsFragmentPrebuildEnabled => false;

    private void SetShadowMaterial(Material material)
    {
        noteData?.MeshRendererAsset?.SetShadowMaterial(material);
    }
}
