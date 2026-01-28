using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawner_HoldRelayHidden : InteractNoteEffectSpawner
{
    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.HoldRelayHidden;
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

