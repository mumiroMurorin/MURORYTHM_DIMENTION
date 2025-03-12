using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        static public Vector3 CalcJudgementThresHold(float radius, int index, Vector3 rotationVector, Vector3? center = null)
        {
            // 円上から中心に向けてのベクトルを算出
            Vector3 directionToCenter = GetDirectionToCenter(radius, index, center);

            // 回転角を算出
            float rad = GetRadianXY(rotationVector);

            return RotateVector(directionToCenter, rad);
        }

        public static float GetRadianXY(Vector2 direction)
        {
            // Mathf.Atan2(y, x) を使ってラジアンを取得
            float angle = Mathf.Atan2(direction.y, direction.x);

            return angle;
        }

        /// <summary>
        /// 半径 r と角度 theta から中心に向かうベクトルを取得
        /// </summary>
        /// <param name="r"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static Vector3 GetDirectionToCenter(float r, int index, Vector3? center = null)
        {
            center = center ?? Vector3.zero;

            float radian = (180f + (index + 0.5f) * 11.25f) * Mathf.Deg2Rad;
            float x = r * Mathf.Cos(radian) + center.Value.x;
            float y = r * Mathf.Sin(radian) + center.Value.y;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// ベクトルを rotationAngle 分だけ回転させる
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="rotationAngle"></param>
        /// <returns></returns>
        public static Vector3 RotateVector(Vector3 vector, float rotationAngle)
        {
            float radian = rotationAngle * Mathf.Deg2Rad;

            // 2D 回転行列を適用 (Z軸を中心に回転)
            float rotatedX = vector.x * Mathf.Cos(radian) - vector.y * Mathf.Sin(radian);
            float rotatedY = vector.x * Mathf.Sin(radian) + vector.y * Mathf.Cos(radian);

            return new Vector3(rotatedX, rotatedY, vector.z); // Z軸はそのまま
        }

        /// <summary>
        /// 2つのTimeToPositionからベクトルを求める
        /// </summary>
        /// <param name="timeToPosition1"></param>
        /// <param name="timeToPosition2"></param>
        /// <returns></returns>
        public static Vector3 CalculateVelocity((float, Vector3)timeToPosition1, (float, Vector3) timeToPosition2)
        {
            float t1 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition1.Item1 : timeToPosition2.Item1;
            float t2 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition2.Item1 : timeToPosition1.Item1;
            Vector3 p1 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition1.Item2 : timeToPosition2.Item2;
            Vector3 p2 = timeToPosition1.Item1 < timeToPosition2.Item1 ? timeToPosition2.Item2 : timeToPosition1.Item2;

            return (p2 - p1) / Mathf.Abs(t2 - t1);
        }
    }
}