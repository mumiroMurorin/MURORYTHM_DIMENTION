using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class JudgementSpawner_HoldStart : JudgementEffectSpawner
    {
        [Header("判定出現半径")]
        [SerializeField] float radius = 8f;
        [SerializeField] GameObject perfectEffect;
        [SerializeField] GameObject greatEffect;
        [SerializeField] GameObject goodEffect;
        [SerializeField] GameObject missEffect;

        public override bool ConditionChecker(NoteJudgementData judgementData)
        {
            return judgementData.NoteData.NoteType == NoteType.HoldStart;
        }

        public override GameObject Spawn(NoteJudgementData judgementData)
        {
            NoteData_HoldStart noteData = judgementData.NoteData as NoteData_HoldStart;

            float centerIndex = noteData.Range[0] + noteData.Range.Length / 2f;
            float radian = (11.25f * centerIndex - 180f) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(radius * Mathf.Cos(radian), radius * Mathf.Sin(radian), 0);
            Vector3 rot = GetEularAngle(pos, Vector3.zero) - new Vector3(0, 0, 90f);

            switch (judgementData.Judgement)
            {
                case Judgement.Perfect:
                    return Instantiate(perfectEffect, pos, Quaternion.Euler(rot), parent);
                case Judgement.Great:
                    return Instantiate(greatEffect, pos, Quaternion.Euler(rot), parent);
                case Judgement.Good:
                    return Instantiate(goodEffect, pos, Quaternion.Euler(rot), parent);
                case Judgement.Miss:
                    return Instantiate(missEffect, pos, Quaternion.Euler(rot), parent);
            }
            return null;
        }

        private Vector3 GetEularAngle(Vector3 pos, Vector3 target)
        {
            // 点Aへの方向を計算
            Vector2 direction = target - pos;

            // Atan2で角度を計算 (ラジアン)
            float angleRad = Mathf.Atan2(direction.y, direction.x);

            // ラジアンを度数法に変換
            float angleDeg = angleRad * Mathf.Rad2Deg;

            return new Vector3(0, 0, angleDeg);
        }
    }

}
