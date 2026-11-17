using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold を、ベジェ曲線で曲げるための計算
    ///
    /// 曲げるのは、隣り合った2つの節 (始点・中継点・終点・ロックしたメッシュ中継点。ほかのメッシュ中継点は飛ばす) の間 (区間)。
    /// 区間ごとに制御点が1つあり、2つの節の中心 (形の重心。SpaceHoldSections.GetCenter) と制御点で、2次のベジェ曲線を作る。
    /// 曲線は3次元: 横 (x)・縦 (y) は断面の中の位置 (単位円の中の値)、奥 (z) は奥行き (SpaceHoldTimeline)。
    /// 制御点の奥行きは、2つの節の間から出さない (出すと、曲線が時間をさかのぼる)。
    ///
    /// 制御点を動かすと、間のメッシュ中継点を、形を保ったまま、曲線が動いた分だけ平行に動かす (BakeKeepingShapes)。
    /// 曲線に近い折れ線を作るのに、ほかの分線にも中継点が要るときは、そこの今の帯の形 (前後の節を補間したもの) を置く。
    /// 新しく置く中継点の頂点の数は、ゲームがそろえる数と同じにしてある (帯の面の張り方が、補間とまったく同じになるように)。
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
        /// 制御点を old から next へ動かしたときの、2つの節の間の中継点を求める。今の帯の形は、そのまま残す (録音 20261117_194644)
        /// ・今ある中継点は、残す (形はそのまま。曲線が動いた分だけ、平行に動かす)。回したり、大きさを変えたりした形が消えないように
        /// ・新しい曲線に近い折れ線を作るのに、ほかの分線にも中継点が要るときは、そこの今の帯の形 (前後の節を補間したもの) を、同じように動かして置く
        /// (前は、2つの節の形から作り直していたので、1つおきに回した形などが、元に戻っていた)
        /// </summary>
        /// <param name="span">2つの節と、その間の今の節 (並びの順。最初と最後が、2つの節)</param>
        /// <param name="vertexCount">新しく置く中継点の頂点の数 (ゲームがそろえる数)</param>
        public static void BakeKeepingShapes(SpaceHoldTimeline timeline, IReadOnlyList<SpaceHoldSection> span, Vector3 oldControl, Vector3 nextControl,
                                             int vertexCount, List<BakedSection> result)
        {
            result.Clear();
            if (timeline == null || span == null || span.Count < 2) { return; }

            SpaceHoldSection a = span[0];
            SpaceHoldSection b = span[span.Count - 1];
            if (b.Line - a.Line < 2) { return; }

            oldControl = ClampControl(a, b, oldControl);
            nextControl = ClampControl(a, b, nextControl);
            Vector3 p0 = GetPoint(a);
            Vector3 p1 = GetPoint(b);

            // 分線ごとの、曲線の動いた分 (両端は 0)
            int lineCount = b.Line - a.Line + 1;
            var fractions = new float[lineCount];
            var moves = new Vector2[lineCount];
            fractions[lineCount - 1] = 1f;
            for (int i = 1; i < lineCount - 1; i++)
            {
                int line = a.Line + i;
                float time = timeline.GetTime(line);
                float depth = timeline.GetDepth(line);
                fractions[i] = SpaceHoldSections.GetFraction(a, b, time, depth);
                Vector3 before = Evaluate(p0, oldControl, p1, SolveForDepth(p0, oldControl, p1, depth));
                Vector3 after = Evaluate(p0, nextControl, p1, SolveForDepth(p0, nextControl, p1, depth));
                moves[i] = new Vector2(after.x - before.x, after.y - before.y);
            }

            // 残す分線: 今ある中継点の分線と、そのあいだで、動いた分が折れ線から離れるところ
            var keep = new bool[lineCount];
            keep[0] = true;
            keep[lineCount - 1] = true;
            for (int s = 1; s < span.Count - 1; s++)
            {
                int index = span[s].Line - a.Line;
                if (index > 0 && index < lineCount - 1) { keep[index] = true; }
            }
            int previousKept = 0;
            for (int i = 1; i < lineCount; i++)
            {
                if (!keep[i]) { continue; }
                SelectLines(fractions, moves, previousKept, i, keep);
                previousKept = i;
            }

            for (int i = 1; i < lineCount - 1; i++)
            {
                if (!keep[i]) { continue; }

                int line = a.Line + i;
                Vector2[] shape = GetCurrentShape(span, line, timeline, vertexCount);
                if (shape == null || shape.Length == 0) { continue; }

                var vertices = new Vector2[shape.Length];
                for (int j = 0; j < shape.Length; j++) { vertices[j] = (shape[j] + moves[i]).ClampToUnitCircle(); }

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
        /// 今の帯の、その分線での形 (その分線に節があれば、その形。無ければ、前後の節をゲームと同じやり方で補間した形)
        /// </summary>
        private static Vector2[] GetCurrentShape(IReadOnlyList<SpaceHoldSection> span, int line, SpaceHoldTimeline timeline, int vertexCount)
        {
            for (int s = 0; s < span.Count; s++)
            {
                if (span[s].Line == line) { return span[s].Vertices; }
            }

            for (int s = 0; s < span.Count - 1; s++)
            {
                SpaceHoldSection before = span[s];
                SpaceHoldSection after = span[s + 1];
                if (before.Line >= line || after.Line <= line) { continue; }

                float fraction = SpaceHoldSections.GetFraction(before, after, timeline.GetTime(line), timeline.GetDepth(line));
                return SpaceHoldSections.InterpolateRaw(before.Vertices, after.Vertices, fraction, vertexCount).ToArray();
            }
            return null;
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
