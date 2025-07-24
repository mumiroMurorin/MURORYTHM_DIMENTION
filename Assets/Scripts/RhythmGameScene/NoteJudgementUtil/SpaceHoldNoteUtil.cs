using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace JudgementUtil.SpacaHold
{
    static public class SpaceHoldJudgement
    {
        /// <summary>
        /// 2次元頂点リストから構成されるMeshの中に点があるか判定
        /// </summary>
        /// <param name="point"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            int crossings = 0;
            int count = polygon.Length;

            for (int i = 0; i < count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % count];

                // 射線がエッジを横切るかチェック
                if (((a.y <= point.y) && (b.y > point.y)) || ((a.y > point.y) && (b.y <= point.y)))
                {
                    float t = (point.y - a.y) / (b.y - a.y);
                    float x = a.x + t * (b.x - a.x);

                    if (x > point.x)
                    {
                        crossings++;
                    }
                }
            }

            // 奇数回交差すれば中にある
            return (crossings % 2) == 1;
        }

        /// <summary>
        /// 線分がポリゴンと交差またはポリゴンに含まれるか判定する
        /// </summary>
        /// <param name="lineStart">線分の始点</param>
        /// <param name="lineEnd">線分の終点</param>
        /// <param name="polygon">ポリゴンの頂点配列（閉じた図形を想定）</param>
        /// <returns>true:交差または内包, false:非交差</returns>
        public static bool IsSegmentIntersectingOrInsidePolygon(Vector2 lineStart, Vector2 lineEnd, Vector2[] polygon)
        {
            int count = polygon.Length;

            // 線分の始点または終点がポリゴン内にあるか
            if (IsPointInPolygon(lineStart, polygon) || IsPointInPolygon(lineEnd, polygon))
            {
                return true;
            }

            // 線分がポリゴンのいずれかの辺と交差しているか
            for (int i = 0; i < count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % count];

                if (DoLineSegmentsIntersect(lineStart, lineEnd, a, b))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 線分同士が交差するかを判定する
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        private static bool DoLineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float d1 = Cross(p2 - p1, q1 - p1);
            float d2 = Cross(p2 - p1, q2 - p1);
            float d3 = Cross(q2 - q1, p1 - q1);
            float d4 = Cross(q2 - q1, p2 - q1);

            // 交差判定（符号が異なる）
            return d1 * d2 < 0 && d3 * d4 < 0;
        }

        public static Vector2[] InterpolatePoints(List<TimeToVertices> timeToVertices, float timing)
        {
            var timeList = timeToVertices.Select(x => x.Timing).ToList();
            int index = LowerBound(timeList, timing) + 1;
            float ratio = (timeToVertices[index].Timing - timeToVertices[index - 1].Timing) / (timing - timeToVertices[index - 1].Timing);

            return InterpolatePoints(timeToVertices[index - 1].Vertices, timeToVertices[index].Vertices, ratio);
        }

        /// <summary>
        /// 中間点を出力
        /// </summary>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        private static Vector2[] InterpolatePoints(Vector2[] listA, Vector2[] listB, float ratio)
        {
            if (listA.Length != listB.Length) 
            {
                Debug.LogError("listAとlistBの長さが一致していません");
                return null;
            }

            Vector2[] result = new Vector2[listA.Length];

            for (int i = 0; i < listA.Length; i++)
            {
                Vector2 pointA = listA[i];
                Vector2 pointB = listB[i];

                // 線分ABの中の比率 ratio の点を計算（線形補間）
                Vector2 interpolated = Vector2.Lerp(pointA, pointB, ratio);
                result[i] = interpolated;
            }

            return result;
        }

        private static float GetTotalLength(List<Vector2> points)
        {
            float length = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                length += Vector2.Distance(points[i], points[i + 1]);
            }
            return length;
        }

        /// <summary>
        /// 外積
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        /// <summary>
        /// 二分探索
        /// </summary>
        /// <param name="list"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int LowerBound(List<float> list, float target)
        {
            int left = 0, right = list.Count;
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (list[mid] < target)
                    left = mid + 1;
                else
                    right = mid;
            }
            return left;
        }
    }
}