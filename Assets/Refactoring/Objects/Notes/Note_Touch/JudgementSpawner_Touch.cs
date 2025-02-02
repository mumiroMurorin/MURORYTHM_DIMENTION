using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class JudgementSpawner_Touch : JudgementEffectSpawner
    {
        [SerializeField] GameObject perfectEffect;
        [SerializeField] GameObject greatEffect;
        [SerializeField] GameObject goodEffect;
        [SerializeField] GameObject missEffect;

        public override bool ConditionChecker(NoteJudgementData judgementData)
        {
            return judgementData.NoteData.NoteType == NoteType.Touch;
        }

        public override GameObject Spawn(NoteJudgementData judgementData)
        {
            //Debug.Log(judgementData.PositionJudged + "," + GetEularAngle(judgementData.PositionJudged, Vector3.zero));
            Vector3 eularAngle = GetEularAngle(judgementData.PositionJudged, Vector3.zero) - new Vector3(0, 0, 90);

            switch (judgementData.Judgement)
            {
                case Judgement.Perfect:
                    return Instantiate(perfectEffect, judgementData.PositionJudged, Quaternion.Euler(eularAngle), parent);
                case Judgement.Great:
                    return Instantiate(greatEffect, judgementData.PositionJudged, Quaternion.Euler(eularAngle), parent);
                case Judgement.Good:
                    return Instantiate(goodEffect, judgementData.PositionJudged, Quaternion.Euler(eularAngle), parent);
                case Judgement.Miss:
                    return Instantiate(missEffect, judgementData.PositionJudged, Quaternion.Euler(eularAngle), parent);
            }
            return null;
        }

        private Vector3 GetEularAngle(Vector3 pos,Vector3 target)
        { 
            // �_A�ւ̕������v�Z
            Vector2 direction = target - pos;

            // Atan2�Ŋp�x���v�Z (���W�A��)
            float angleRad = Mathf.Atan2(direction.y, direction.x);

            // ���W�A����x���@�ɕϊ�
            float angleDeg = angleRad * Mathf.Rad2Deg;

            return new Vector3(0, 0, angleDeg);
        }
    }

}
