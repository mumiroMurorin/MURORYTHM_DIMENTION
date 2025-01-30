using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class InteractNoteEffectSpawner_Touch : InteractNoteEffectSpawner
    {
        [SerializeField] GameObject perfectEffect;
        [SerializeField] GameObject greatEffect;
        [SerializeField] GameObject goodEffect;

        public override bool ConditionChecker(NoteJudgementData judgementData)
        {
            return judgementData.NoteData.NoteType == NoteType.Touch;
        }

        public override GameObject Spawn(NoteJudgementData judgementData)
        {
            Vector3 pos = judgementData.PositionJudged;

            switch (judgementData.Judgement)
            {
                case Judgement.Perfect:
                    return Instantiate(perfectEffect, pos, Quaternion.identity, parent);
                case Judgement.Great:
                    return Instantiate(greatEffect, pos, Quaternion.identity, parent);
                case Judgement.Good:
                    return Instantiate(goodEffect, pos, Quaternion.identity, parent);
            }
            return null;
        }
    }

}
