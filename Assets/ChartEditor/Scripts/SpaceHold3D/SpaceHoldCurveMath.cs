using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold を、ベジェ曲線で曲げるための計算
    ///
    /// 曲げるのは、隣り合った2つの節 (始点・中継点・終点。メッシュ中継点は飛ばす) の間 (区間)。
    /// 区間ごとに制御点が1つあり、2つの節の中心 (頂点の平均の位置) と制御点で、2次のベジェ曲線を作る。
    /// 曲線は3次元: 横 (x)・縦 (y) は断面の中の位置 (単位円の中の値)、奥 (z) は奥行き (SpaceHoldTimeline)。
    /// 制御点の奥行きは、2つの節の間から出さない (出すと、曲線が時間をさかのぼる)。
    ///
    /// 間の分線のうち、曲線に近い折れ線を作るのに必要なものにだけ、メッシュ中継点を置く。
    /// 置く中継点の形は、2つの節の形をゲームと同じやり方で補間したものを、曲線の位置まで平行に動かしたもの。
    /// 頂点の数は、ゲームがそろえる数と同じにしてある (帯の面の張り方が、補間とまったく同じになるように)。
    /// </summary>
    public static class SpaceHoldCurveMath
    {
        /// <summary>
        /// 制御点の奥行きを、区間のどこまで寄せられるか (両端からの割合)
        /// </summary>
        public const float CONTROL_DEPTH_MARGIN = 0.05f;

        /// <summary>
        /// 中継点を置かずに済ませてよい、曲線と折れ線のずれ (単位円の中の値。半径10のノーツ面で 0.04)
        /// </summary>
        const float TOLERANCE = 0.004f;

        /// <summary>
        /// 置いた中継点1つぶん
        /// </summary>
        public struct BakedSection
        {
            public int Line;
            public float Time;
            public float Depth;
            public Vector2[] Vertices;
        }

        /// <summary>
        /// 節の、曲線の空間での位置 (中心の x, y と、奥行き)
        /// </summary>
        public static Vector3 GetPoint(SpaceHoldSection section)
        {
            Vector2 center = section.Center;
            return new Vector3(center.x, center.y, section.Depth);
        }

        /// <summary>
        /// 曲げていない (まっすぐな) ときの制御点 = 2つの節のまんなか
        /// </summary>
        public static Vector3 GetStraightControl(SpaceHoldSection a, SpaceHoldSection b)
        {
            return (GetPoint(a) + GetPoint(b)) * 0.5f;
        }

        /// <summary>
        /// 制御点の奥行きを、区間の中に収める
        /// </summary>
        public static Vector3 ClampControl(SpaceHoldSection a, SpaceHoldSection b, Vector3 control)
        {
            float min = Mathf.Min(a.Depth, b.Depth);
            float max = Mathf.Max(a.Depth, b.Depth);
            float margin = (max - min) * CONTROL_DEPTH_MARGIN;
            control.z = max - min <= 1e-6f ? (min + max) * 0.5f : Mathf.Clamp(control.z, min + margin, max - margin);
            return control;
        }

        public static Vector3 Evaluate(Vector3 p0, Vector3 control, Vector3 p1, float s)
        {
            float u = 1f - s;
            return u * u * p0 + 2f * u * s * control + s * s * p1;
        }

        /// <summary>
        /// 曲線の奥行きが depth になる位置 (0〜1) を求める (制御点の奥行きが区間の中にあれば、奥行きは単調に増える)
        /// </summary>
        public static float SolveForDepth(Vector3 p0, Vector3 control, Vector3 p1, float depth)
        {
            if (Mathf.Abs(p1.z - p0.z) <= 1e-6f) { return 0.5f; }

            bool isIncreasing = p1.z > p0.z;
            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 40; i++)
            {
                float middle = (low + high) * 0.5f;
                float z = Evaluate(p0, control, p1, middle).z;
                if ((z < depth) == isIncreasing) { low = middle; }
                else { high = middle; }
            }

            return (low + high) * 0.5f;
        }

        /// <summary>
        /// 今ある中継点 (メッシュ中継点) の中心に、いちばん近くなる制御点を求める (制御点の奥行きは区間のまんなか)
        /// 制御点を覚えていない区間 (譜面を開き直したときなど) で、印を今の形に近い場所に出すのに使う。中継点が無ければ、まっすぐの位置
        /// </summary>
        public static Vector3 FitControl(SpaceHoldSection a, SpaceHoldSection b, IReadOnlyList<SpaceHoldSection> relays)
        {
            Vector3 straight = GetStraightControl(a, b);
            if (relays == null || relays.Count == 0) { return straight; }

            // 制御点の奥行きがまんなかのとき、曲線の奥行きは s に比例する (s = 奥行きの割合)。
            // 中心 c = (1-s)²A + 2s(1-s)P + s²B を、P について最小二乗で解く
            Vector2 centerA = a.Center;
            Vector2 centerB = b.Center;
            Vector2 numerator = Vector2.zero;
            float denominator = 0f;
            foreach (var relay in relays)
            {
                float s = SpaceHoldSections.GetFraction(a, b, relay.Time, relay.Depth);
                float weight = 2f * s * (1f - s);
                if (weight <= 1e-4f) { continue; }

                Vector2 rest = relay.Center - (1f - s) * (1f - s) * centerA - s * s * centerB;
                numerator += weight * rest;
                denominator += weight * weight;
            }

            if (denominator <= 1e-8f) { return straight; }

            Vector2 control = numerator / denominator;
            return new Vector3(control.x, control.y, straight.z);
        }

        /// <summary>
        /// 制御点の曲線のとおりに、2つの節の間に置く中継点を求める
        /// </summary>
        /// <param name="vertexCount">中継点の頂点の数 (ゲームがそろえる数。SpaceHoldSections.GetNormalizedVertexCount)</param>
        public static void Bake(SpaceHoldTimeline timeline, SpaceHoldSection a, SpaceHoldSection b, Vector3 control, int vertexCount, List<BakedSection> result)
        {
            result.Clear();
            if (timeline == null || b.Line - a.Line < 2) { return; }

            control = ClampControl(a, b, control);
            Vector3 p0 = GetPoint(a);
            Vector3 p1 = GetPoint(b);
            Vector2 centerA = a.Center;
            Vector2 centerB = b.Center;

            // 間の分線ごとに、まっすぐのときからのずれを求める (両端は、ずれ 0)
            int lineCount = b.Line - a.Line + 1;
            var fractions = new float[lineCount];
            var offsets = new Vector2[lineCount];
            fractions[0] = 0f;
            fractions[lineCount - 1] = 1f;
            for (int i = 1; i < lineCount - 1; i++)
            {
                int line = a.Line + i;
                float time = timeline.GetTime(line);
                float depth = timeline.GetDepth(line);
                float fraction = SpaceHoldSections.GetFraction(a, b, time, depth);
                float s = SolveForDepth(p0, control, p1, depth);
                Vector3 onCurve = Evaluate(p0, control, p1, s);

                fractions[i] = fraction;
                offsets[i] = new Vector2(onCurve.x, onCurve.y) - Vector2.Lerp(centerA, centerB, fraction);
            }

            // 曲線に近い折れ線を作るのに必要な分線を選ぶ
            var keep = new bool[lineCount];
            keep[0] = true;
            keep[lineCount - 1] = true;
            SelectLines(fractions, offsets, 0, lineCount - 1, keep);

            for (int i = 1; i < lineCount - 1; i++)
            {
                if (!keep[i]) { continue; }

                int line = a.Line + i;
                var points = SpaceHoldSections.InterpolateRaw(a.Vertices, b.Vertices, fractions[i], vertexCount);
                var vertices = new Vector2[points.Count];
                for (int j = 0; j < points.Count; j++)
                {
                    vertices[j] = (points[j] + offsets[i]).ClampToUnitCircle();
                }

                result.Add(new BakedSection
                {
                    Line = line,
                    Time = timeline.GetTime(line),
                    Depth = timeline.GetDepth(line),
                    Vertices = vertices,
                });
            }
        }

        /// <summary>
        /// first と last の間で、折れ線からいちばんずれている分線を残し、その両側を同じように調べる
        /// </summary>
        private static void SelectLines(float[] fractions, Vector2[] offsets, int first, int last, bool[] keep)
        {
            if (last - first < 2) { return; }

            float worst = 0f;
            int worstIndex = -1;
            float span = fractions[last] - fractions[first];
            for (int i = first + 1; i < last; i++)
            {
                float t = span > 1e-6f ? (fractions[i] - fractions[first]) / span : 0.5f;
                Vector2 onLine = Vector2.Lerp(offsets[first], offsets[last], t);
                float deviation = (offsets[i] - onLine).magnitude;
                if (deviation > worst)
                {
                    worst = deviation;
                    worstIndex = i;
                }
            }

            if (worstIndex < 0 || worst <= TOLERANCE) { return; }

            keep[worstIndex] = true;
            SelectLines(fractions, offsets, first, worstIndex, keep);
            SelectLines(fractions, offsets, worstIndex, last, keep);
        }
    }
}
