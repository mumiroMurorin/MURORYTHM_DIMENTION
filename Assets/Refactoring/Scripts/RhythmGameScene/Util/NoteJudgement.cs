using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoteJudgement
{
    static public class DynamicNote
    {
        /// <summary>
        /// 入力されたVectorが判定閾値を超えているか返す
        /// </summary>
        /// <param name="thresHold"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool JudgeThreshold(Vector3 thresHold, Vector3 input)
        {
            // XYZどれかで閾値を超えていればtrue
            return JudgeThresholdValue(thresHold.x, input.x) ||
                JudgeThresholdValue(thresHold.y, input.y) ||
                JudgeThresholdValue(thresHold.z, input.z);
        }

        /// <summary>
        /// 要素の値で判定
        /// </summary>
        /// <param name="valueThreshold"></param>
        /// <param name="valueInput"></param>
        /// <returns></returns>
        private static bool JudgeThresholdValue(float valueThreshold, float valueInput)
        {
            if (valueThreshold == 0f) { return false; }
            if (valueThreshold > 0 && valueInput < valueThreshold) { return false; }
            if (valueThreshold < 0 && valueInput > valueThreshold) { return false; }

            return true;
        }

        /// <summary>
        /// ↑を(0,1)とするベクトルを基に判定の閾値を算出する
        /// Vectorの各要素の最大値が閾値になる
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        static public Vector3 CalcJudgementThresHold(Vector3 vector,int[] range)
        {
            Vector3 maxVector = 

            float rad = GetRadianXY(vector);
            return new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), vector.z);
        }

        public static float GetRadianXY(Vector2 direction)
        {
            // Mathf.Atan2(y, x) を使ってラジアンを取得
            float angle = Mathf.Atan2(direction.y, direction.x);

            return angle;
        }
    }
}