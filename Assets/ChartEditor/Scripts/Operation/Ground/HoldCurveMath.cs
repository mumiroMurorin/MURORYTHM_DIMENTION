using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// ホールドを曲線で曲げるときの計算 (制御点の位置 → 間に置く中継点の場所とレーン)
    ///
    /// 隣り合った2つの節の間に、制御点を1つ置く。ホールドは、始まりの節から終わりの節へ、
    /// 制御点の方へ引っ張られた曲線 (2次ベジェ曲線) でつながる。曲線は、制御点そのものは通らない。
    /// ・制御点は (横, 時間方向) の2つの数で表す
    ///     横      : 2つの節をまっすぐ結んだ線から、レーンいくつぶん、横へ離れているか (プラスが右)
    ///     時間方向: 始まりの節から終わりの節までの長さの、何割のところにあるか (0〜1)
    ///   時間方向を 0〜1 に収めているので、曲線が時間をさかのぼることはない
    ///   (0〜1 の外に出すと、曲線が節の手前や先へはみ出して戻ってくる形になり、ホールドでは表せない)
    /// ・横が 0 のとき (動かしていない制御点 null も同じ) は、まっすぐなホールドになる
    /// ・ホールドの左の縁と右の縁を、それぞれ1本の曲線でつなぐ。どちらの縁も、
    ///   その縁の両端をまっすぐ結んだ線から、制御点と同じだけ横へ離れた点へ引っ張る
    ///   (節の左の端だけを広げても、右の縁の形は変わらない。幅は、時間に沿って一定の割合で変わる)
    /// ・制御点の位置を「まっすぐ結んだ線からの離れ具合」で持っているので、ホールド全体を横へ動かしても、
    ///   左右に反転しても、別の場所へ複製しても、曲がり方は変わらない
    ///
    /// ノーツは、16本のレーンと分線の上にしか置けない。
    /// 中継点は、すべての分線に置くのではなく、曲線に近い折れ線を作るのに必要な分線にだけ置く (SelectRelays)。
    /// 中継点と中継点の間は、まっすぐな斜めの線でつながるので、少ない中継点の方が、なめらかな曲線に見える
    /// (すべての分線に置くと、レーン1本ぶんの段差が並んだ、階段のような形になる)。
    /// </summary>
    public static class HoldCurveMath
    {
        // レーンの数
        public const int LANE_COUNT = 16;
        // 動かしていない制御点の、時間方向の位置 (2つの節の真ん中)
        public const float DEFAULT_CONTROL_TIME = 0.5f;

        // 曲線上の位置を探すときの、繰り返しの回数 (1回ごとに誤差が半分になる)
        const int SEARCH_COUNT = 30;
        // レーンのちょうど真ん中 (0.5) とみなす、計算の誤差の幅
        const float ROUND_TOLERANCE = 0.0005f;

        // 中継点を選ぶときに、ホールドの縁が曲線から離れてよい、いちばん大きな幅 (レーンの数)
        // (中継点はレーンの境目にしか置けないので、丸めの誤差 (最大でレーン半分) より少し大きくしてある)
        const float MAX_DISTANCE = 0.51f;
        // 中継点を1つ増やすことの重さ。中継点を増やすと曲線に近づくが、増やしすぎると段差が並んでガタガタに見える。
        // 「縁と曲線の離れ具合 (レーンの数) の2乗を、すべての分線で足した数」が、これより大きく減るときだけ、中継点を増やす
        const float RELAY_COST = 0.15f;
        // 分線がこれより多い区間では、1つの中継点から LOOK_AHEAD_COUNT 本より先の分線へは、まっすぐ結ばない
        // (調べる量が、分線の数の2乗で増えるため。ふつうの長さのホールドでは、すべての分線を調べる)
        const int FULL_SEARCH_COUNT = 512;
        const int LOOK_AHEAD_COUNT = 256;
        // 曲線が、2つの節をまっすぐ結んだ線から、これより離れなければ (レーンの数)、まっすぐなホールドとして扱う
        const float STRAIGHT_DISTANCE = 0.02f;
        // 傾きを比べるときの、計算の誤差の幅
        const float SLOPE_TOLERANCE = 0.0001f;
        // 時間が進んでいないとみなす幅 (2つの節の間の長さに対する割合)
        const float MIN_TIME_STEP = 0.000001f;

        // 作業用 (呼ぶたびに確保しないよう使い回す。分線ごとの値が入る)
        static readonly List<float> ratios = new List<float>();
        static readonly List<float> leftTargets = new List<float>();
        static readonly List<float> rightTargets = new List<float>();
        static readonly List<Lanes> allLanes = new List<Lanes>();
        // 中継点を選ぶときの作業用
        static readonly List<EdgeSums> leftSums = new List<EdgeSums>();
        static readonly List<EdgeSums> rightSums = new List<EdgeSums>();
        static readonly List<double> costs = new List<double>();
        static readonly List<int> previous = new List<int>();

        /// <summary>
        /// 縁の位置の、分線をまたいだ合計 (まっすぐ結んだ線と曲線の離れ具合を、分線を1本ずつ調べ直さずに求めるためのもの)
        /// </summary>
        private struct EdgeSums
        {
            public double Time, TimeTime, Edge, EdgeEdge, TimeEdge;

            public EdgeSums Add(double time, double edge)
            {
                return new EdgeSums { Time = Time + time, TimeTime = TimeTime + time * time, Edge = Edge + edge, EdgeEdge = EdgeEdge + edge * edge, TimeEdge = TimeEdge + time * edge };
            }

            /// <summary>
            /// 「時刻 fromTime で fromEdge を通る、傾き slope の線」と、曲線との離れ具合の2乗の合計
            /// (この合計から before の合計を引いた範囲にある、count 本の分線について)
            /// </summary>
            public double SquaredDistance(EdgeSums before, int count, double fromTime, double fromEdge, double slope)
            {
                double offset = fromEdge - slope * fromTime;
                double time = Time - before.Time;
                double timeTime = TimeTime - before.TimeTime;
                double edge = Edge - before.Edge;
                double edgeEdge = EdgeEdge - before.EdgeEdge;
                double timeEdge = TimeEdge - before.TimeEdge;

                double sum = count * offset * offset + slope * slope * timeTime + edgeEdge + 2.0 * offset * slope * time - 2.0 * offset * edge - 2.0 * slope * timeEdge;
                return sum > 0.0 ? sum : 0.0;
            }
        }

        /// <summary>
        /// ホールドの節
        /// </summary>
        public struct Node
        {
            /// <summary>時間方向の位置 (時間とともに、増え続けるか、減り続ける数であれば、どんな単位でもよい)</summary>
            public float Position;
            /// <summary>左端のレーン番号 (0〜15)</summary>
            public int Left;
            /// <summary>幅 (レーンの数)</summary>
            public int Width;

            /// <summary>左の縁・右の縁・中心の位置 (レーン番号。左端のレーンの左の縁が 0、右端のレーンの右の縁が 16)</summary>
            public float LeftEdge => Left;
            public float RightEdge => Left + Width;
            public float Center => Left + Width * 0.5f;
        }

        /// <summary>
        /// ノーツが占めるレーン
        /// </summary>
        public struct Lanes
        {
            /// <summary>左端のレーン番号 (0〜15)</summary>
            public int Left;
            /// <summary>幅 (レーンの数)</summary>
            public int Width;
        }

        /// <summary>
        /// 動かしていない制御点 (2つの節をまっすぐ結んだ線の、真ん中)
        /// </summary>
        public static Vector2 StraightControl => new Vector2(0f, DEFAULT_CONTROL_TIME);

        /// <summary>
        /// 制御点の、時間方向の位置 (始まりの節を 0、終わりの節を 1 とした割合)
        /// </summary>
        public static float GetControlTime(Vector2? control)
        {
            return control.HasValue ? Clamp01(control.Value.y) : DEFAULT_CONTROL_TIME;
        }

        /// <summary>
        /// 制御点の、レーンの方向の位置 (レーン番号。左端のレーンの左の縁が 0)
        /// </summary>
        /// <param name="startCenter">始まりの節の中心</param>
        /// <param name="endCenter">終わりの節の中心</param>
        public static float GetControlCenter(float startCenter, float endCenter, Vector2? control)
        {
            float side = control.HasValue ? Finite(control.Value.x) : 0f;
            return startCenter + (endCenter - startCenter) * GetControlTime(control) + side;
        }

        /// <summary>
        /// 2つの節の間にある分線のうち、中継点を置く分線と、その中継点が占めるレーンを求める
        ///
        /// 中継点と中継点の間は、まっすぐな線でつながる。その折れ線が、曲線にいちばん近くなる置き方を選ぶ。
        /// ・折れ線は、どの分線でも、曲線から MAX_DISTANCE 以内を通ること
        /// ・その中で、「曲線との離れ具合の2乗の合計 + 中継点の数 × RELAY_COST」がいちばん小さいもの
        /// まっすぐなホールドには、中継点は1つも置かれない。
        /// </summary>
        /// <param name="start">始まりの節 (時間が早い方)</param>
        /// <param name="end">終わりの節</param>
        /// <param name="control">2つの節の間の制御点 (動かしていなければ null)</param>
        /// <param name="positions">間にある分線の、時間方向の位置 (start.Position と end.Position の間。時間の順)</param>
        /// <param name="indices">結果: 中継点を置く分線 (positions の中の番号。小さい順)</param>
        /// <param name="results">結果: その中継点が占めるレーン (indices と同じ順番で入る)</param>
        public static void SelectRelays(Node start, Node end, Vector2? control, IList<float> positions, List<int> indices, List<Lanes> results)
        {
            SelectRelays(start, end, control, positions, indices, results, RELAY_COST);
        }

        /// <param name="relayCost">中継点を1つ増やすことの重さ (RELAY_COST の説明を参照)</param>
        public static void SelectRelays(Node start, Node end, Vector2? control, IList<float> positions, List<int> indices, List<Lanes> results, float relayCost)
        {
            indices.Clear();
            results.Clear();

            Interpolate(start, end, control, positions, allLanes);
            int count = allLanes.Count;
            if (count == 0) { return; }

            // 2つの節が同じ位置にあって、時間が進まないときは、選びようがないので、すべての分線に置く
            if (Mathf.Approximately(end.Position - start.Position, 0f))
            {
                for (int i = 0; i < count; i++)
                {
                    indices.Add(i);
                    results.Add(allLanes[i]);
                }
                return;
            }

            // まっすぐなホールドには、中継点は要らない
            bool isStraight = true;
            for (int i = 0; i < count && isStraight; i++)
            {
                float leftLine = start.LeftEdge + (end.LeftEdge - start.LeftEdge) * ratios[i];
                float rightLine = start.RightEdge + (end.RightEdge - start.RightEdge) * ratios[i];
                isStraight = Mathf.Abs(leftLine - leftTargets[i]) <= STRAIGHT_DISTANCE && Mathf.Abs(rightLine - rightTargets[i]) <= STRAIGHT_DISTANCE;
            }
            if (isStraight) { return; }

            // 分線をまたいだ合計 (sums[i] = i 番目より前の分線の合計)
            leftSums.Clear();
            rightSums.Clear();
            leftSums.Add(new EdgeSums());
            rightSums.Add(new EdgeSums());
            for (int i = 0; i < count; i++)
            {
                leftSums.Add(leftSums[i].Add(ratios[i], leftTargets[i]));
                rightSums.Add(rightSums[i].Add(ratios[i], rightTargets[i]));
            }

            // 場所の番号: 0 = 始まりの節、1〜count = 分線 (positions の i 番目が i + 1)、count + 1 = 終わりの節
            // costs[k] = 始まりの節から k までを、いちばんうまくつないだときの重さ。previous[k] = そのとき、k の1つ前に通る場所
            int last = count + 1;
            int lookAhead = count > FULL_SEARCH_COUNT ? LOOK_AHEAD_COUNT : last;
            costs.Clear();
            previous.Clear();
            for (int k = 0; k <= last; k++)
            {
                costs.Add(double.PositiveInfinity);
                previous.Add(k - 1);
            }
            costs[0] = 0.0;

            for (int from = 0; from < last; from++)
            {
                // どこからも届かなかった場所 (ふつうは起きない): すぐ前の場所から、そのままつなぐ
                if (double.IsPositiveInfinity(costs[from]))
                {
                    costs[from] = costs[from - 1] + relayCost + 1.0;
                    previous[from] = from - 1;
                }

                double fromTime = from == 0 ? 0.0 : ratios[from - 1];
                float fromLeft = from == 0 ? start.LeftEdge : allLanes[from - 1].Left;
                float fromRight = from == 0 ? start.RightEdge : allLanes[from - 1].Left + allLanes[from - 1].Width;

                // from から出る線の傾きの、許される範囲 (間の分線を1本通りすぎるたびに、狭くなる)
                double lowLeft = double.NegativeInfinity;
                double highLeft = double.PositiveInfinity;
                double lowRight = double.NegativeInfinity;
                double highRight = double.PositiveInfinity;

                int farthest = System.Math.Min(last, from + lookAhead);
                for (int to = from + 1; to <= farthest; to++)
                {
                    bool isEnd = to == last;
                    double step = (isEnd ? 1.0 : ratios[to - 1]) - fromTime;
                    // 時間が進んでいない (ふつうは起きない)。傾きを決められないので、ここから先へは結ばない
                    if (step <= MIN_TIME_STEP) { break; }

                    float left = isEnd ? end.LeftEdge : allLanes[to - 1].Left;
                    float right = isEnd ? end.RightEdge : allLanes[to - 1].Left + allLanes[to - 1].Width;

                    // ここに中継点を置いたときの、縁の位置と、曲線との離れ具合 (終わりの節は、曲線の上にある)
                    double leftDistance = isEnd ? 0.0 : left - leftTargets[to - 1];
                    double rightDistance = isEnd ? 0.0 : right - rightTargets[to - 1];

                    if (System.Math.Abs(leftDistance) <= MAX_DISTANCE && System.Math.Abs(rightDistance) <= MAX_DISTANCE)
                    {
                        double slopeLeft = (left - fromLeft) / step;
                        double slopeRight = (right - fromRight) / step;
                        if (slopeLeft >= lowLeft - SLOPE_TOLERANCE && slopeLeft <= highLeft + SLOPE_TOLERANCE &&
                            slopeRight >= lowRight - SLOPE_TOLERANCE && slopeRight <= highRight + SLOPE_TOLERANCE)
                        {
                            // from と to の間にある分線での、離れ具合の2乗の合計
                            int between = to - 1 - from;
                            double cost = costs[from]
                                + (isEnd ? 0.0 : relayCost)
                                + leftDistance * leftDistance + rightDistance * rightDistance
                                + leftSums[to - 1].SquaredDistance(leftSums[from], between, fromTime, fromLeft, slopeLeft)
                                + rightSums[to - 1].SquaredDistance(rightSums[from], between, fromTime, fromRight, slopeRight);

                            if (cost < costs[to])
                            {
                                costs[to] = cost;
                                previous[to] = from;
                            }
                        }
                    }

                    if (isEnd) { break; }

                    // この分線を通りすぎる線は、この分線でも、曲線から MAX_DISTANCE 以内を通らなければならない
                    lowLeft = System.Math.Max(lowLeft, (leftTargets[to - 1] - MAX_DISTANCE - fromLeft) / step);
                    highLeft = System.Math.Min(highLeft, (leftTargets[to - 1] + MAX_DISTANCE - fromLeft) / step);
                    lowRight = System.Math.Max(lowRight, (rightTargets[to - 1] - MAX_DISTANCE - fromRight) / step);
                    highRight = System.Math.Min(highRight, (rightTargets[to - 1] + MAX_DISTANCE - fromRight) / step);

                    // これより先へは、どう結んでも収まらない
                    if (lowLeft > highLeft + SLOPE_TOLERANCE || lowRight > highRight + SLOPE_TOLERANCE) { break; }
                }
            }

            // 終わりの節から逆にたどって、通った分線を集める
            for (int k = previous[last]; k > 0; k = previous[k])
            {
                indices.Add(k - 1);
            }
            indices.Reverse();

            for (int i = 0; i < indices.Count; i++)
            {
                results.Add(allLanes[indices[i]]);
            }
        }

        /// <summary>
        /// 2つの節の間にある分線ごとに、そこに中継点を置くとしたら占めるレーンを求める (曲線にいちばん近いレーン)
        /// </summary>
        /// <param name="start">始まりの節 (時間が早い方)</param>
        /// <param name="end">終わりの節</param>
        /// <param name="control">2つの節の間の制御点 (動かしていなければ null)</param>
        /// <param name="positions">間にある分線の、時間方向の位置 (start.Position と end.Position の間)</param>
        /// <param name="results">結果の入れ先 (positions と同じ順番で入る)</param>
        public static void Interpolate(Node start, Node end, Vector2? control, IList<float> positions, List<Lanes> results)
        {
            results.Clear();
            ratios.Clear();
            leftTargets.Clear();
            rightTargets.Clear();
            if (positions == null) { return; }

            float length = end.Position - start.Position;

            // 制御点の位置
            float time = GetControlTime(control);
            float side = control.HasValue ? Finite(control.Value.x) : 0f;

            // 左の縁と右の縁の、曲線の引っ張り先
            float leftPull = start.LeftEdge + (end.LeftEdge - start.LeftEdge) * time + side;
            float rightPull = start.RightEdge + (end.RightEdge - start.RightEdge) * time + side;

            // 2つの節のうち、細い方の幅 (途中のホールドは、これより細くしない)
            int minWidth = Mathf.Clamp(Mathf.Min(start.Width, end.Width), 1, LANE_COUNT);

            for (int i = 0; i < positions.Count; i++)
            {
                float ratio = Mathf.Approximately(length, 0f) ? 0f : Clamp01((positions[i] - start.Position) / length);

                // 時間方向の割合が ratio になる、曲線上の場所を探す (曲線は時間をさかのぼらないので、二分探索で求まる)
                float low = 0f;
                float high = 1f;
                for (int n = 0; n < SEARCH_COUNT; n++)
                {
                    float middle = (low + high) * 0.5f;
                    if (Bezier(0f, time, 1f, middle) < ratio) { low = middle; }
                    else { high = middle; }
                }
                float u = (low + high) * 0.5f;

                float leftEdge = Bezier(start.LeftEdge, leftPull, end.LeftEdge, u);
                float rightEdge = Bezier(start.RightEdge, rightPull, end.RightEdge, u);

                // 左の縁と右の縁を、いちばん近いレーンの境目へ丸める。レーンの外へはみ出した縁は、端のレーンで止める
                int left = Mathf.Clamp(RoundToInt(leftEdge), 0, LANE_COUNT);
                int right = Mathf.Clamp(RoundToInt(rightEdge), 0, LANE_COUNT);

                // 端で止めたせいで、細い方の節よりも細くなるときは、その幅を保って、端のレーンに沿わせる
                // (同じ幅の節どうしをつないだホールドは、端に押し付けても、幅が変わらない)
                if (right - left < minWidth)
                {
                    left = Mathf.Clamp(left, 0, LANE_COUNT - minWidth);
                    right = left + minWidth;
                }

                results.Add(new Lanes { Left = left, Width = right - left });

                // 丸める前の縁の位置 (中継点を選ぶときに、曲線として使う)。端のレーンでの扱いは、上と同じ
                float leftTarget = Mathf.Clamp(leftEdge, 0f, LANE_COUNT);
                float rightTarget = Mathf.Clamp(rightEdge, 0f, LANE_COUNT);
                if (rightTarget - leftTarget < minWidth)
                {
                    leftTarget = Mathf.Clamp(leftTarget, 0f, LANE_COUNT - minWidth);
                    rightTarget = leftTarget + minWidth;
                }

                ratios.Add(ratio);
                leftTargets.Add(leftTarget);
                rightTargets.Add(rightTarget);
            }
        }

        /// <summary>
        /// 2次ベジェ曲線の、u (0〜1) の場所の値 (p0 から p2 へ、p1 の方へ引っ張られながら進む)
        /// </summary>
        private static float Bezier(float p0, float p1, float p2, float u)
        {
            float v = 1f - u;
            return v * v * p0 + 2f * v * u * p1 + u * u * p2;
        }

        /// <summary>
        /// 四捨五入 (Mathf.RoundToInt は .5 を偶数側へ丸めるので、段差の出方が左右で変わってしまう)
        ///
        /// 左の縁と右の縁は別々に計算するので、ちょうど真ん中 (0.5) の近くでは、計算の誤差で、片方だけ切り捨てになることがある。
        /// そうなると、同じ幅の節どうしをつないだホールドの幅が、途中で1つ変わってしまうので、
        /// 真ん中からごくわずかに小さいだけの値も、真ん中と同じように切り上げる。
        /// </summary>
        private static int RoundToInt(float value)
        {
            return Mathf.FloorToInt(value + 0.5f + ROUND_TOLERANCE);
        }

        private static float Clamp01(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        }

        private static float Finite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }
}
