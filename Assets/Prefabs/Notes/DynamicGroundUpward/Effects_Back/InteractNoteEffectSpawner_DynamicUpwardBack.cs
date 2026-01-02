using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawner_DynamicUpwardBack : InteractNoteEffectSpawner
{
    [SerializeField] GameObject backGroundEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.DynamicGroundUpward;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        if(judgementData.Judgement == Judgement.Miss) { return null; }
        if(judgementData.Judgement == Judgement.None) { return null; }

        var pos = Vector3.zero;
        var obj = Instantiate(backGroundEffect, pos, Quaternion.identity, parent);
        
        SetDataForEffect(obj, judgementData.NoteData as NoteData_DynamicGroundUpward);

        return obj;
    }

    /// <summary>
    /// エフェクトを初期化する
    /// </summary>
    /// <param name="effectObject"></param>
    /// <param name="noteData"></param>
    private void SetDataForEffect(GameObject effectObject, NoteData_DynamicGroundUpward noteData)
    {
        if (!effectObject.TryGetComponent(out IInteractNoteEffectController<NoteData_DynamicGroundUpward> effect)) { return; }
        effect.SetEffect(noteData);
        effect.Play();
    }
}

