using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawner_DivineTouch : InteractNoteEffectSpawner
{
    [SerializeField] GameObject perfectEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.DivineTouch;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        Vector3 pos = Vector3.zero;
        GameObject obj;

        switch (judgementData.Judgement)
        {
            case Judgement.Perfect:
                obj = Instantiate(perfectEffect, pos, Quaternion.identity, parent);
                break;
            default:
                return null;
        }

        SetDataForEffect(obj, judgementData.NoteData as NoteData_DivineTouch);
        return obj;
    }

    /// <summary>
    /// エフェクトを初期化する
    /// </summary>
    /// <param name="effectObject"></param>
    /// <param name="noteData"></param>
    private void SetDataForEffect(GameObject effectObject, NoteData_DivineTouch noteData)
    {
        if (!effectObject.TryGetComponent(out IInteractNoteEffectController<NoteData_DivineTouch> effect)) { return; }
        effect.SetEffect(noteData);
        effect.Play();

    }
}

