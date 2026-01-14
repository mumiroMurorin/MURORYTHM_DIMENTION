using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgementSpawner_SpaceHoldRelay : JudgementEffectSpawner
{
    [SerializeField] float addY = 1.5f;

    [SerializeField] GameObject perfectEffect;
    [SerializeField] GameObject greatEffect;
    [SerializeField] GameObject goodEffect;
    [SerializeField] GameObject missEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.SpaceHoldRelay;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        NoteData_SpaceHoldRelay noteData = judgementData.NoteData as NoteData_SpaceHoldRelay;

        Vector2 ave = noteData.Vertices.Average();
        Vector3 pos = new Vector3(ave.x * 10f, ave.y * 10f + addY, 0);

        switch (judgementData.Judgement)
        {
            case Judgement.Perfect:
                return Instantiate(perfectEffect, pos, Quaternion.identity, parent);
            case Judgement.Great:
                return Instantiate(greatEffect, pos, Quaternion.identity, parent);
            case Judgement.Good:
                return Instantiate(goodEffect, pos, Quaternion.identity, parent);
            case Judgement.Miss:
                return Instantiate(missEffect, pos, Quaternion.identity, parent);
        }
        return null;
    }
}

