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
        /// 2点間のベクトルの角度θを求める
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private float CalculateAngle(Vector3 a, Vector3 b)
        {
            Vector3 direction = b - a;

            // ベクトルの角度を計算 (xz平面またはxy平面に合わせる)
            float angleRad = Mathf.Atan2(direction.y, direction.x); 

            // ラジアンを度に変換
            float angleDeg = angleRad * Mathf.Rad2Deg;

            // 角度を0～360°の範囲に正規化
            if (angleDeg < 0)
            {
                angleDeg += 360f;
            }

            return angleDeg;
        }
    }

}
