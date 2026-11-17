using System.Collections.Generic;
using MeshGenerate;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 譜面の分線ごとの「時刻 (秒)」と「奥行き」
    ///
    /// 奥行きは、ゲームでノーツが流れてくる距離 (スピード倍率を掛けた時間)。ノートスピードは掛けていない (比べるだけなら要らない)。
    /// ゲームの SpaceHold の帯は、節と節の間を、この奥行きでまっすぐ補間して作られる (SpaceHoldMeshGenerator)。
    /// 時間の式はゲームと同じ (CalcTimingClass: 1分線 = 60 / BPM × 4 / 拍の単位 / 分割数)。
    /// オフセットは、差しか使わないので入れない。スピード倍率 0 は、ゲームと同じく 1 として扱う。
    ///
    /// 作り直すのは、1フレームに1回まで (BPM やスピード倍率を変えても、次に使うときには合っている)。
    /// 分線には、譜面の先頭から順に通し番号 (線番号) を振ってある。
    /// </summary>
    public class SpaceHoldTimeline
    {
        static SpaceHoldTimeline cached;
        static ChartData cachedChart;
        static int cachedFrame = -1;

        // 小節ごとの、最初の分線の線番号
        readonly List<int> barFirstLine = new List<int>();
        // 線番号ごとの値
        readonly List<int> lineBar = new List<int>();
        readonly List<int> lineSub = new List<int>();
        readonly List<float> lineTime = new List<float>();
        readonly List<float> lineDepth = new List<float>();

        /// <summary>
        /// 譜面の分線の時刻と奥行き (このフレームの譜面の状態)。譜面が無いときは null
        /// </summary>
        public static SpaceHoldTimeline Get(ChartData chartData)
        {
            if (chartData == null) { return null; }

            if (cached != null && ReferenceEquals(cachedChart, chartData) && cachedFrame == Time.frameCount) { return cached; }

            cached ??= new SpaceHoldTimeline();
            cached.Build(chartData);
            cachedChart = chartData;
            cachedFrame = Time.frameCount;
            return cached;
        }

        /// <summary>
        /// 分線の数
        /// </summary>
        public int LineCount => lineBar.Count;

        public int GetBar(int line) => lineBar[line];
        public int GetSub(int line) => lineSub[line];
        public float GetTime(int line) => lineTime[line];
        public float GetDepth(int line) => lineDepth[line];

        /// <summary>
        /// 小節番号と分線番号から、線番号を求める (無い場所なら -1)
        /// </summary>
        public int GetLine(int bar, int sub)
        {
            if (bar < 0 || bar >= barFirstLine.Count) { return -1; }

            int first = barFirstLine[bar];
            int next = bar + 1 < barFirstLine.Count ? barFirstLine[bar + 1] : lineBar.Count;
            if (sub < 0 || first + sub >= next) { return -1; }

            return first + sub;
        }

        /// <summary>
        /// ノーツの場所の、線番号 (無い場所なら -1)
        /// </summary>
        public int GetLine(IReadOnlyAddressWithinRange address)
        {
            return address == null ? -1 : GetLine(address.BarIndex, address.SubDivisionIndex);
        }

        /// <summary>
        /// ノーツの場所の、時刻と奥行き
        /// </summary>
        public bool TryGet(IReadOnlyAddressWithinRange address, out float time, out float depth)
        {
            int line = GetLine(address);
            if (line < 0)
            {
                time = 0f;
                depth = 0f;
                return false;
            }

            time = lineTime[line];
            depth = lineDepth[line];
            return true;
        }

        /// <summary>
        /// 線番号 first から last までのうち、奥行きが depth にいちばん近い分線の線番号
        /// </summary>
        public int FindNearestLineByDepth(float depth, int first, int last)
        {
            first = Mathf.Max(0, first);
            last = Mathf.Min(lineBar.Count - 1, last);
            if (first > last) { return -1; }

            int best = first;
            float bestDelta = float.MaxValue;
            for (int line = first; line <= last; line++)
            {
                float delta = Mathf.Abs(lineDepth[line] - depth);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = line;
                }
            }

            return best;
        }

        /// <summary>
        /// 線番号の場所を、スペースのノーツの場所にする
        /// </summary>
        public AddressWithinRange CreateSpaceAddress(int line)
        {
            return new AddressWithinRange(lineBar[line], lineSub[line], new List<float> { SpaceHoldSections.SPACE_LANE_INDEX });
        }

        private void Build(ChartData chartData)
        {
            barFirstLine.Clear();
            lineBar.Clear();
            lineSub.Clear();
            lineTime.Clear();
            lineDepth.Clear();

            float time = 0f;
            float depth = 0f;
            var bars = chartData.BarDatas;
            for (int bar = 0; bar < bars.Count; bar++)
            {
                barFirstLine.Add(lineBar.Count);

                var barData = bars[bar];
                BarConfig config = barData.BarConfig;
                float beatUnit = config.BeatUnit > 0f ? config.BeatUnit : 4f;
                int divisionNum = Mathf.Max(1, config.DivisionNum);

                var subs = barData.SubDivisionDatas;
                for (int sub = 0; sub < subs.Count; sub++)
                {
                    lineBar.Add(bar);
                    lineSub.Add(sub);
                    lineTime.Add(time);
                    lineDepth.Add(depth);

                    float bpm = Mathf.Max(subs[sub].Bpm.Value, 0.0001f);
                    float speedRatio = subs[sub].SpeedRatio.Value;
                    if (speedRatio == 0f) { speedRatio = 1f; }

                    float seconds = (60f / bpm) * (4f / beatUnit) / divisionNum;
                    time += seconds;
                    depth += seconds * speedRatio;
                }
            }
        }
    }

    /// <summary>
    /// SpaceHold の節 (始点・中継点・メッシュ中継点・終点) 1つぶんの、形と位置
    /// </summary>
    public struct SpaceHoldSection
    {
        public NoteData_SpaceHold Note;
        /// <summary>頂点 (単位円の中の値)</summary>
        public Vector2[] Vertices;
        /// <summary>線番号 (SpaceHoldTimeline)</summary>
        public int Line;
        public float Time;
        public float Depth;

        /// <summary>
        /// 断面の中心 = 形の重心 (SpaceHoldSections.GetCenter。頂点の平均ではない)
        /// </summary>
        public Vector2 Center => SpaceHoldSections.GetCenter(Vertices);
    }

    /// <summary>
    /// SpaceHold のつながりと、断面の計算
    /// </summary>
    public static class SpaceHoldSections
    {
        /// <summary>スペースのノーツの、レーン番号の代わりに入っている値 (スペースの配置場所)</summary>
        public const float SPACE_LANE_INDEX = 100f;

        /// <summary>
        /// ゲームが、断面の頂点の数をそろえるときの最小の数 (NoteConverters_SpaceHold の MESH_DIVISION_NUM と同じ)
        /// </summary>
        public const int MESH_DIVISION_NUM = 10;

        /// <summary>
        /// ノーツがある SpaceHold のつながりを、時間の順に並べて返す (つながっていないノーツなら、そのノーツだけ)
        /// </summary>
        public static void GetChain(INotesDataGetter notesGetter, NoteData_SpaceHold note, List<NoteData_SpaceHold> result)
        {
            result.Clear();
            if (note == null) { return; }

            int index = note.ChainIndex.Value;
            var list = index >= 0 && notesGetter != null ? notesGetter.GetChainNoteList(index)?.ChainNoteList : null;
            if (list != null)
            {
                foreach (var chained in list)
                {
                    if (chained is NoteData_SpaceHold spaceHold) { result.Add(spaceHold); }
                }
            }

            if (!result.Contains(note))
            {
                result.Clear();
                result.Add(note);
            }
        }

        /// <summary>
        /// 始点と終点がそろった、完成した SpaceHold か
        /// </summary>
        public static bool IsComplete(IReadOnlyList<NoteData_SpaceHold> chain)
        {
            return chain != null && chain.Count >= 2 &&
                   chain[0].NoteType == DeploymentNoteType.SpaceHoldStart &&
                   chain[chain.Count - 1].NoteType == DeploymentNoteType.SpaceHoldEnd;
        }

        /// <summary>
        /// 節の形と位置を集める (場所が分からないノーツがあるときは false)
        /// </summary>
        public static bool TryBuildSections(SpaceHoldTimeline timeline, IReadOnlyList<NoteData_SpaceHold> chain, List<SpaceHoldSection> result)
        {
            result.Clear();
            if (timeline == null || chain == null) { return false; }

            foreach (var note in chain)
            {
                if (note == null) { return false; }

                int line = timeline.GetLine(note.Address);
                if (line < 0) { return false; }

                result.Add(new SpaceHoldSection
                {
                    Note = note,
                    Vertices = GetVertices(note.SpaceVertices),
                    Line = line,
                    Time = timeline.GetTime(line),
                    Depth = timeline.GetDepth(line),
                });
            }

            return true;
        }

        public static Vector2[] GetVertices(SpaceVertices vertices)
        {
            var source = vertices.Vertices;
            var result = new Vector2[source.Count];
            for (int i = 0; i < source.Count; i++) { result[i] = source[i].Position.Value; }
            return result;
        }

        /// <summary>
        /// 断面の中心 = 形の重心 (面積の中心)
        ///
        /// 頂点の平均ではない: ゲームと同じやり方で頂点の数をそろえた形 (辺の途中に点を足したもの) は、点の打ち方が辺ごとに
        /// ばらつくので、頂点の平均が片側へずれる。同じ形でも、始点・終点 (頂点が少ない) と、間のメッシュ中継点 (そろえた形) で
        /// 中心がずれ、軌跡が両端だけ斜めになっていた (録音 20261117_163212)。重心は、辺の途中の点で変わらない。
        /// 面積がほとんど無い形 (点が2つ以下、一直線など) は、頂点の平均にする。
        /// 8の字のように辺が交わっていて、面積が打ち消し合う形も、重心が形から遠くへ飛ぶので、頂点の平均にする
        /// (打ち消したあとの面積が、打ち消す前の 1/4 より小さいとき。重心が、頂点の収まる範囲の外に出たとき)。
        /// </summary>
        public static Vector2 GetCenter(Vector2[] vertices)
        {
            if (vertices == null || vertices.Length == 0) { return Vector2.zero; }

            Vector2 sum = Vector2.zero;
            Vector2 min = vertices[0];
            Vector2 max = vertices[0];
            foreach (var vertex in vertices)
            {
                sum += vertex;
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
            }
            Vector2 average = sum / vertices.Length;
            if (vertices.Length < 3) { return average; }

            // (頂点の平均を原点にして計算する: 桁落ちを防ぐ)
            double area2 = 0;
            double absoluteArea2 = 0;
            double cx = 0;
            double cy = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 a = vertices[i] - average;
                Vector2 b = vertices[(i + 1) % vertices.Length] - average;
                double cross = (double)a.x * b.y - (double)b.x * a.y;
                area2 += cross;
                absoluteArea2 += System.Math.Abs(cross);
                cx += (a.x + b.x) * cross;
                cy += (a.y + b.y) * cross;
            }
            if (System.Math.Abs(area2) < 1e-7 || System.Math.Abs(area2) < absoluteArea2 * 0.25) { return average; }

            Vector2 center = average + new Vector2((float)(cx / (3.0 * area2)), (float)(cy / (3.0 * area2)));
            const float MARGIN = 1e-4f;
            if (center.x < min.x - MARGIN || center.x > max.x + MARGIN || center.y < min.y - MARGIN || center.y > max.y + MARGIN) { return average; }

            return center;
        }

        /// <summary>
        /// ゲームが頂点の数をそろえるときの数 (最小の数と、つながりの中でいちばん多い頂点の数の、大きい方)
        /// </summary>
        public static int GetNormalizedVertexCount(IReadOnlyList<SpaceHoldSection> sections)
        {
            int count = MESH_DIVISION_NUM;
            foreach (var section in sections)
            {
                if (section.Vertices != null && section.Vertices.Length > count) { count = section.Vertices.Length; }
            }
            return count;
        }

        /// <summary>
        /// 断面の頂点を、ゲームと同じやり方で count 個にそろえる (外周に沿って、足りない分の点を等間隔に打つ)
        /// </summary>
        public static Vector2[] Resample(Vector2[] vertices, int count)
        {
            if (vertices == null || vertices.Length == 0) { return new Vector2[0]; }

            var list = new List<Vector2>(vertices);
            return SpaceHoldMeshGenerator.InterpolatePoints(list, list, 0f, count).ToArray();
        }

        /// <summary>
        /// 2つの節の間の、奥行き (同じなら時刻) の割合 (0 で a、1 で b)
        /// </summary>
        public static float GetFraction(SpaceHoldSection a, SpaceHoldSection b, float time, float depth)
        {
            float depthDelta = b.Depth - a.Depth;
            if (Mathf.Abs(depthDelta) > 1e-6f) { return Mathf.Clamp01((depth - a.Depth) / depthDelta); }

            float timeDelta = b.Time - a.Time;
            if (Mathf.Abs(timeDelta) > 1e-6f) { return Mathf.Clamp01((time - a.Time) / timeDelta); }

            return 0f;
        }

        /// <summary>
        /// 2つの断面を、ゲームと同じやり方で補間する (頂点の数は count 個にそろう)
        /// </summary>
        public static List<Vector2> InterpolateRaw(Vector2[] a, Vector2[] b, float fraction, int count)
        {
            return SpaceHoldMeshGenerator.InterpolatePoints(new List<Vector2>(a), new List<Vector2>(b), fraction, count);
        }

        /// <summary>
        /// 完成した SpaceHold の、その時刻・奥行きの断面の形 (前後の節を、ゲームと同じやり方で補間したもの)
        ///
        /// 補間した形は、頂点の数がそろえられていて、同じ場所の点や、辺の途中の点を含む。
        /// 編集しやすいよう、それらを取り除く (形の外周は変わらない)。
        /// </summary>
        /// <returns>SpaceHold の時間の範囲の外のときは false</returns>
        public static bool TryInterpolate(IReadOnlyList<SpaceHoldSection> sections, float time, float depth, out Vector2[] vertices)
        {
            vertices = null;
            if (sections == null || sections.Count == 0) { return false; }

            const float EPSILON = 1e-5f;
            if (time < sections[0].Time - EPSILON || time > sections[sections.Count - 1].Time + EPSILON) { return false; }

            foreach (var section in sections)
            {
                if (section.Vertices == null || section.Vertices.Length == 0) { return false; }
            }

            int count = GetNormalizedVertexCount(sections);

            // ちょうど節のある時刻なら、その節の形 (同じ時刻に節が2つあるときは、後ろの方)
            for (int i = sections.Count - 1; i >= 0; i--)
            {
                if (Mathf.Abs(sections[i].Time - time) <= EPSILON)
                {
                    vertices = (Vector2[])sections[i].Vertices.Clone();
                    return true;
                }
            }

            for (int i = 0; i < sections.Count - 1; i++)
            {
                SpaceHoldSection a = sections[i];
                SpaceHoldSection b = sections[i + 1];
                if (time > b.Time) { continue; }

                float fraction = GetFraction(a, b, time, depth);
                var points = InterpolateRaw(a.Vertices, b.Vertices, fraction, count);
                var simplified = Simplify(points);
                vertices = (simplified.Count >= 3 ? simplified : points).ToArray();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 多角形から、同じ場所に重なった点と、まっすぐな辺の途中にある点を取り除く (外周の形は変わらない)
        /// 0番の点は残す (頂点の並びの始まりを変えないため。ねじれの向きが変わらないように)
        /// </summary>
        public static List<Vector2> Simplify(List<Vector2> points)
        {
            const float SAME_POINT = 1e-4f;
            const float STRAIGHT = 1e-3f;

            var result = new List<Vector2>(points.Count);
            foreach (var point in points)
            {
                if (result.Count == 0 || (point - result[result.Count - 1]).sqrMagnitude > SAME_POINT * SAME_POINT) { result.Add(point); }
            }
            while (result.Count > 1 && (result[0] - result[result.Count - 1]).sqrMagnitude <= SAME_POINT * SAME_POINT)
            {
                result.RemoveAt(result.Count - 1);
            }

            bool isChanged = true;
            while (isChanged && result.Count > 3)
            {
                isChanged = false;
                for (int i = 1; i < result.Count && result.Count > 3; i++)
                {
                    int count = result.Count;
                    Vector2 previous = result[i - 1];
                    Vector2 current = result[i];
                    Vector2 next = result[(i + 1) % count];

                    Vector2 toCurrent = current - previous;
                    Vector2 toNext = next - current;
                    float scale = toCurrent.magnitude * toNext.magnitude;
                    float cross = toCurrent.x * toNext.y - toCurrent.y * toNext.x;

                    bool isOnStraightEdge = scale > 1e-12f && Mathf.Abs(cross) <= scale * STRAIGHT && Vector2.Dot(toCurrent, toNext) > 0f;
                    if (scale <= 1e-12f || isOnStraightEdge)
                    {
                        result.RemoveAt(i);
                        i--;
                        isChanged = true;
                    }
                }
            }

            return result;
        }
    }
}
