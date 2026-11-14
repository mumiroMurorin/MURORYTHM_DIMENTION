using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// プレビュー上でノーツを掴んで動かすための計算
    /// </summary>
    public static class PreviewPickMath
    {
        /// <summary>
        /// 並んでいる分線の中から、指定した距離にいちばん近いものの番号を返す
        ///
        /// ふつうの譜面では距離は先頭から順に増えていくので、いちばん近い分線は1つに決まる。
        /// スピード倍率がマイナスの区間がある譜面では流れが戻るため、同じ距離を何度も通る。
        /// その場合は「その距離を通過する場所」が複数あるので、直前に指していた分線に近いものを選ぶ
        /// (選び直すたびに遠くの小節へ飛んでしまうのを防ぐ)。
        /// </summary>
        /// <param name="distances">分線の距離 (譜面の先頭から順)</param>
        /// <param name="distance">探す距離</param>
        /// <param name="preferredIndex">直前に指していた分線の番号。無ければ負の数</param>
        /// <returns>分線の番号。分線が1本も無ければ -1</returns>
        public static int FindNearestIndex(IReadOnlyList<float> distances, float distance, int preferredIndex)
        {
            int count = distances.Count;
            if (count == 0) { return -1; }

            // 全体でいちばん近い分線
            int nearest = 0;
            float nearestError = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float error = Mathf.Abs(distances[i] - distance);
                if (error < nearestError)
                {
                    nearest = i;
                    nearestError = error;
                }
            }

            if (preferredIndex < 0) { return nearest; }

            // 隣り合う2本の間に探す距離がある場所ごとに、近い方の分線を候補にする
            // 候補のうち、直前に指していた分線にいちばん近いものを選ぶ
            int best = -1;
            int bestGap = int.MaxValue;
            for (int i = 0; i + 1 < count; i++)
            {
                float a = distances[i] - distance;
                float b = distances[i + 1] - distance;
                if ((a > 0f && b > 0f) || (a < 0f && b < 0f)) { continue; }

                int candidate = Mathf.Abs(a) <= Mathf.Abs(b) ? i : i + 1;
                int gap = Mathf.Abs(candidate - preferredIndex);
                if (gap < bestGap)
                {
                    best = candidate;
                    bestGap = gap;
                }
            }

            // 探す距離が譜面の範囲の外にあるときは、通過する場所が無い
            return best >= 0 ? best : nearest;
        }

        /// <summary>
        /// 円形の軌道の上での距離の差を、半周ぶん前後の範囲 (-一周の半分 〜 +一周の半分) に直す
        /// (位置から求めた距離は一周ぶんの範囲に丸まっているので、基準の位置にいちばん近い周回での差に直すために使う)
        /// </summary>
        /// <param name="delta">距離の差</param>
        /// <param name="circumference">軌道の一周の長さ</param>
        public static float WrapDistance(float delta, float circumference)
        {
            if (!(circumference > 0f)) { return delta; }

            return Mathf.Repeat(delta + circumference * 0.5f, circumference) - circumference * 0.5f;
        }

        /// <summary>
        /// 軌道の上の位置から求めた距離を、譜面の先頭からの距離に直す
        ///
        /// 軌道は円なので、位置から求めた距離は一周ぶんの範囲 (-半周 〜 +半周) に丸まっている。
        /// 譜面の先頭からの距離は、曲が進むと半周を超えるので、そのままでは比べられない
        /// (比べると、譜面の後ろの方で、カーソルの指す分線を取り違える)。
        /// 基準の位置(今の再生位置)にいちばん近い周回での距離に直してから比べる。
        /// </summary>
        /// <param name="sectionDistance">位置から求めた距離 (PreviewTrackSurface が返す値)</param>
        /// <param name="referenceDistance">基準の位置の、譜面の先頭からの距離 (今の再生位置)</param>
        /// <param name="curveRadius">軌道の半径</param>
        public static float UnwrapDistance(float sectionDistance, float referenceDistance, float curveRadius)
        {
            return referenceDistance + WrapDistance(sectionDistance - referenceDistance, 2f * Mathf.PI * curveRadius);
        }

        // ノーツの「端」として扱う幅 (レーン数)。ノーツの幅に対する割合で決めて、狭すぎ・広すぎにならないよう範囲を限る
        // (幅1レーンのノーツでも、真ん中を掴めば動かせて、端を掴めば幅を変えられるように)
        const float NOTE_EDGE_RATIO = 0.2f;
        const float NOTE_EDGE_MIN_LANES = 0.3f;
        const float NOTE_EDGE_MAX_LANES = 0.6f;

        /// <summary>
        /// レーン番号(実数)が、グラウンドのノーツの左右どちらの端を指しているかを返す
        /// (端を掴むと幅の変更、真ん中を掴むと移動になる)
        /// </summary>
        /// <param name="lane">調べるレーン番号 (実数。レーンの中心で整数になる)</param>
        /// <param name="firstLane">ノーツの左端のレーン番号</param>
        /// <param name="lastLane">ノーツの右端のレーン番号</param>
        /// <returns>左の端 = -1、右の端 = +1、端ではない = 0</returns>
        public static int GetNoteEdge(float lane, float firstLane, float lastLane)
        {
            // ノーツが占めている範囲 (レーンの中心が整数なので、両端のレーンの外側へ半レーンずつ広がっている)
            float left = firstLane - 0.5f;
            float right = lastLane + 0.5f;
            float edgeWidth = Mathf.Clamp((right - left) * NOTE_EDGE_RATIO, NOTE_EDGE_MIN_LANES, NOTE_EDGE_MAX_LANES);

            if (lane <= left + edgeWidth) { return -1; }
            if (lane >= right - edgeWidth) { return 1; }

            return 0;
        }

        /// <summary>
        /// ノーツの片方の端を動かしたときの、新しい左端と右端のレーン番号を求める
        /// 動かす端は、反対の端より内側へは行かず (幅は1レーン以上)、レーンの外へも出ない
        /// </summary>
        /// <param name="originFirstLane">動かす前の左端のレーン番号</param>
        /// <param name="originLastLane">動かす前の右端のレーン番号</param>
        /// <param name="laneDelta">端を動かすレーン数 (右へ動かすとき正)</param>
        /// <param name="isRightEdge">右の端を動かすとき true、左の端を動かすとき false</param>
        /// <param name="maxLane">レーン番号の最大値</param>
        public static void MoveNoteEdge(int originFirstLane, int originLastLane, int laneDelta, bool isRightEdge, int maxLane, out int firstLane, out int lastLane)
        {
            firstLane = originFirstLane;
            lastLane = originLastLane;

            if (isRightEdge) { lastLane = Mathf.Clamp(originLastLane + laneDelta, originFirstLane, maxLane); }
            else { firstLane = Mathf.Clamp(originFirstLane + laneDelta, 0, originLastLane); }
        }

        /// <summary>
        /// 光線が三角形の集まり(メッシュ)に当たる場所のうち、いちばん手前までの長さを求める
        /// </summary>
        /// <param name="vertices">頂点</param>
        /// <param name="triangles">三角形ごとの頂点番号 (3つで1組)</param>
        /// <param name="origin">光線の始点 (頂点と同じ座標系)</param>
        /// <param name="direction">光線の向き (頂点と同じ座標系。長さは1でなくてよい)</param>
        /// <param name="hitLength">当たった場所までの長さ (direction の何倍か)</param>
        public static bool TryRaycastTriangles(Vector3[] vertices, int[] triangles, Vector3 origin, Vector3 direction, out float hitLength)
        {
            return TryRaycastTriangles(vertices, triangles, origin, direction, null, out hitLength);
        }

        /// <summary>
        /// 光線が三角形の集まり(メッシュ)に当たる場所のうち、条件に合うものの中で、いちばん手前までの長さを求める
        /// </summary>
        /// <param name="acceptsHit">当たった場所を対象にするか決める処理 (引数は、その場所までの長さ)。null のときはすべて対象にする</param>
        public static bool TryRaycastTriangles(
            Vector3[] vertices,
            int[] triangles,
            Vector3 origin,
            Vector3 direction,
            System.Predicate<float> acceptsHit,
            out float hitLength)
        {
            hitLength = float.MaxValue;
            if (vertices == null || triangles == null) { return false; }

            const float EPSILON = 1e-7f;

            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];
                if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length) { continue; }

                Vector3 v0 = vertices[i0];
                Vector3 edge1 = vertices[i1] - v0;
                Vector3 edge2 = vertices[i2] - v0;

                // 光線と三角形の交点を求める (面の表裏は問わない)
                Vector3 p = Vector3.Cross(direction, edge2);
                float determinant = Vector3.Dot(edge1, p);
                if (determinant > -EPSILON && determinant < EPSILON) { continue; }

                float inverse = 1f / determinant;
                Vector3 toOrigin = origin - v0;
                float u = Vector3.Dot(toOrigin, p) * inverse;
                if (!(u >= 0f && u <= 1f)) { continue; }

                Vector3 q = Vector3.Cross(toOrigin, edge1);
                float v = Vector3.Dot(direction, q) * inverse;
                if (!(v >= 0f && u + v <= 1f)) { continue; }

                float length = Vector3.Dot(edge2, q) * inverse;
                if (!(length > 0f && length < hitLength)) { continue; }
                if (acceptsHit != null && !acceptsHit(length)) { continue; }

                hitLength = length;
            }

            return hitLength < float.MaxValue;
        }
    }
}
