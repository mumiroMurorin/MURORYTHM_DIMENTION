using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawner_DynamicLeftwardBack : InteractNoteEffectSpawner
{
    [SerializeField] GameObject backGroundEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.DynamicGroundLeftward;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        var pos = Vector3.zero;
        var obj = Instantiate(backGroundEffect, pos, Quaternion.identity, parent);

        SetDataForEffect(obj, judgementData.NoteData as NoteData_DynamicGroundLeftward);

        return obj;
    }

    /// <summary>
    /// エフェクトを初期化する
    /// </summary>
    /// <param name="effectObject"></param>
    /// <param name="noteData"></param>
    private void SetDataForEffect(GameObject effectObject, NoteData_DynamicGroundLeftward noteData)
    {
        if (!effectObject.TryGetComponent(out IInteractNoteEffectController<NoteData_DynamicGroundLeftward> effect)) { return; }
        effect.SetEffect(noteData);
        effect.Play();
    }
}

