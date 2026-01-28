using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawner_HoldStart : InteractNoteEffectSpawner
{
    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.HoldStart;
    }

    protected override Vector3 CalcSpawnPos(NoteJudgementData judgementData)
    {
        return Vector3.zero;
    }

    protected override Quaternion CalcSpawnRotate(NoteJudgementData judgementData)
    {
        return Quaternion.identity;
    }
}

