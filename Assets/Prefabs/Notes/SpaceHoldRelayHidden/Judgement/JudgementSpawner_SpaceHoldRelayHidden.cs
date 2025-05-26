using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgementSpawner_SpaceHoldRelayHidden : JudgementEffectSpawner
{
    [SerializeField] float radius = 10f;
    [SerializeField] float addHeight = 1f;
    [SerializeField] GameObject perfectEffect;
    [SerializeField] GameObject greatEffect;
    [SerializeField] GameObject goodEffect;
    [SerializeField] GameObject missEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.SpaceHoldRelayHidden;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        NoteData_SpaceHoldRelayHidden noteData = judgementData.NoteData as NoteData_SpaceHoldRelayHidden;

        Vector2 ave = noteData.Vertices.Average();
        Vector3 pos = new Vector3(ave.x, ave.y, 0);

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

