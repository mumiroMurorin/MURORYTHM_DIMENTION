using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JudgementUtil.Dynamic
{
    static public class DynamicJudgement
    {
        // レーン0の左端の角度 (度)。レーンはここから反時計回りに並ぶ (コントローラーの半円の下側)
        const float LANE_START_ANGLE = 180f;
        // 1レーンあたりの角度 (度)
        const float LANE_ANGLE_WIDTH = 11.25f;

        /// <summary>
        /// ノーツの見た目上の向きを、入力空間 (コントローラーの半円と同じ座標) での単位ベクトルに変換する。
        /// <para>
        /// localDirection はノーツのタイルに描かれた向き。タイルはレーンごとに回転していて、
        /// 上 (0,1) = 半円の中心へ向かう向き、右 (1,0) = レーン番号が増える向き (半円に沿う向き)。
        /// 一番下のレーンでは、上下左右がそのまま画面の上下左右になる。
        /// </para>
        /// </summary>
        public static Vector2 GetLaneJudgeDirection(int laneIndex, Vector2 localDirection)
        {
            float radian = (LANE_START_ANGLE + (laneIndex + 0.5f) * LANE_ANGLE_WIDTH) * Mathf.Deg2Rad;

            // レーンの位置から半円の中心へ向かう向き (タイルの上)
            Vector2 towardCenter = new Vector2(-Mathf.Cos(radian), -Mathf.Sin(radian));
            // レーン番号が増える向き (タイルの右)
            Vector2 alongLanes = new Vector2(-Mathf.Sin(radian), Mathf.Cos(radian));

            Vector2 direction = alongLanes * localDirection.x + towardCenter * localDirection.y;
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        }

        /// <summary>
        /// 手の速度のうち判定の向きの成分が、magnitude 以上あるか
        /// (向きと直角や逆向きの動きは成功にならない)
        /// </summary>
        public static bool JudgeDirection(Vector2 judgeDirection, Vector3 velocity, float magnitude)
        {
            if (judgeDirection == Vector2.zero) { return false; }

            float speedAlongDirection = Vector2.Dot(new Vector2(velocity.x, velocity.y), judgeDirection);
            return speedAlongDirection > 0f && speedAlongDirection >= magnitude;
        }

        /// <summary>
        /// 2つのTimeToPositionからベクトルを求める
        /// </summary>
        /// <param name="timeToPosition1"></param>
        /// <param name="timeToPosition2"></param>
        /// <returns></returns>
        public static Vector3 CalculateVelocity(TimeToPos timeToPosition1, TimeToPos timeToPosition2)
        {
            float t1 = timeToPosition1.Time;
            float t2 = timeToPosition2.Time;
            Vector3 p1 = timeToPosition1.Time < timeToPosition2.Time ? timeToPosition1.Pos : timeToPosition2.Pos;
            Vector3 p2 = timeToPosition1.Time < timeToPosition2.Time ? timeToPosition2.Pos : timeToPosition1.Pos;

            return (p2 - p1) / Mathf.Abs(t2 - t1);
        }
    }

}