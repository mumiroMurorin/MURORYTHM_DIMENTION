using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
}