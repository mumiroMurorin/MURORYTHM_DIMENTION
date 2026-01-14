using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgementSpawner_SpaceBreak : JudgementEffectSpawner
{
    [SerializeField] float addY = 1.5f;

    [SerializeField] GameObject perfectEffect;
    [SerializeField] GameObject greatEffect;
    [SerializeField] GameObject greatEffect_late;
    [SerializeField] GameObject greatEffect_fast;
    [SerializeField] GameObject goodEffect;
    [SerializeField] GameObject goodEffect_late;
    [SerializeField] GameObject goodEffect_fast;
    [SerializeField] GameObject missEffect;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.SpaceBreak;
    }

    public override GameObject Spawn(NoteJudgementData judgementData)
    {
        NoteData_SpaceBreak noteData = judgementData.NoteData as NoteData_SpaceBreak;

        Vector2 ave = noteData.Vertices.Average();
        Vector3 pos = new Vector3(ave.x * 10f, ave.y * 10f + addY, 0);

        switch (judgementData.Judgement)
        {
            case Judgement.Perfect:
                return Instantiate(perfectEffect, pos, Quaternion.identity, parent);
            case Judgement.Great:
                // FastLate•\Ž¦‚È‚µ
                if (!judgementData.IsEnabledFastLate)
                { return Instantiate(greatEffect, pos, Quaternion.identity, parent); }

                // FastŽž
                else if (judgementData.TimingError < 0f)
                { return Instantiate(greatEffect_fast, pos, Quaternion.identity, parent); }

                // LateŽž
                else
                { return Instantiate(greatEffect_late, pos, Quaternion.identity, parent); }

            case Judgement.Good:
                // FastLate•\Ž¦‚È‚µ
                if (!judgementData.IsEnabledFastLate)
                { return Instantiate(goodEffect, pos, Quaternion.identity, parent); }

                // FastŽž
                else if (judgementData.TimingError < 0f)
                { return Instantiate(goodEffect_fast, pos, Quaternion.identity, parent); }

                // LateŽž
                else
                { return Instantiate(goodEffect_late, pos, Quaternion.identity, parent); }

            case Judgement.Miss:
                return Instantiate(missEffect, pos, Quaternion.identity, parent);
        }
        return null;
    }
}

