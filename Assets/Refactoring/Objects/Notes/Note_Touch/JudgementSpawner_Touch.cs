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
            Debug.Log(judgementData.PositionJudged + "," + CalculateAngle(judgementData.PositionJudged, Vector3.zero));
            Vector3 euler = new Vector3(0, 0, CalculateAngle(judgementData.PositionJudged, Vector3.zero));

            switch (judgementData.Judgement)
            {
                case Judgement.Perfect:
                    return Instantiate(perfectEffect, judgementData.PositionJudged, Quaternion.Euler(euler), parent);
                case Judgement.Great:
                    return Instantiate(greatEffect, judgementData.PositionJudged, Quaternion.Euler(euler), parent);
                case Judgement.Good:
                    return Instantiate(goodEffect, judgementData.PositionJudged, Quaternion.Euler(euler), parent);
                case Judgement.Miss:
                    return Instantiate(missEffect, judgementData.PositionJudged, Quaternion.Euler(euler), parent);
            }
            return null;
        }

        /// <summary>
        /// 2�_�Ԃ̃x�N�g���̊p�x�Ƃ����߂�
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float CalculateAngle(Vector3 a, Vector3 b)
        {
            Vector3 direction = b - a;

            // �x�N�g���̊p�x���v�Z (xz���ʂ܂���xy���ʂɍ��킹��)
            float angleRad = Mathf.Atan2(direction.y, direction.x); 

            // ���W�A����x�ɕϊ�
            float angleDeg = angleRad * Mathf.Rad2Deg;

            // �p�x��0�`360���͈̔͂ɐ��K��
            if (angleDeg < 0)
            {
                angleDeg += 360f;
            }

            return angleDeg;
        }
    }

}
