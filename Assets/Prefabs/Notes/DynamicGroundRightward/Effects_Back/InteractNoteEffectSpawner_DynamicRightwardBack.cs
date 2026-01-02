using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawner_DynamicRightwardBack : InteractNoteEffectSpawner
{
    [SerializeField] GameObject backGroundEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.DynamicGroundRightward;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        if(judgementData.Judgement == Judgement.Miss) { return null; }
        if(judgementData.Judgement == Judgement.None) { return null; }

        var pos = Vector3.zero;
        var obj = Instantiate(backGroundEffect, pos, Quaternion.identity, parent);
        
        SetDataForEffect(obj, judgementData.NoteData as NoteData_DynamicGroundRightward);

        return obj;
    }

    /// <summary>
    /// エフェクトを初期化する
    /// </summary>
    /// <param name="effectObject"></param>
    /// <param name="noteData"></param>
    private void SetDataForEffect(GameObject effectObject, NoteData_DynamicGroundRightward noteData)
    {
        if (!effectObject.TryGetComponent(out IInteractNoteEffectController<NoteData_DynamicGroundRightward> effect)) { return; }
        effect.SetEffect(noteData);
        effect.Play();
    }
}

