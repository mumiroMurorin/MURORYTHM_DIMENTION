using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgementSpawner_SpaceHoldRelayHidden : JudgementEffectSpawner
{
    [SerializeField] float addY = 1.5f;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.SpaceHoldRelayHidden;
    }

    protected override Vector3 CalcSpawnPos(NoteJudgementData judgementData)
    {
        if (judgementData.NoteData is not NoteData_SpaceHoldRelayHidden noteData) { return Vector3.zero; }

        Vector2 ave = noteData.Vertices.Average();
        Vector3 pos = new Vector3(ave.x * 10f, ave.y * 10f + addY, 0);

        return pos;
    }

    protected override Quaternion CalcSpawnRotate(NoteJudgementData judgementData)
    {
        return Quaternion.identity;
    }
}

