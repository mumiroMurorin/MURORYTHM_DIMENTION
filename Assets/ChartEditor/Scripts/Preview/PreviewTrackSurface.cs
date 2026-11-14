using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// プレビューの中で、光線(カーソルの位置)がノーツの流れる面のどこに当たっているかを求める
    ///
    /// ノーツは「円弧に沿って奥へ伸びる軌道」のまわりの、半径10の半円筒の面に並んでいる。
    /// (軌道は NoteTrackCurve、面上の並びは各ノーツのファクトリと同じ式を逆向きに使っている)
    /// 当たった位置は「譜面上の距離」と「レーン番号」で返す。
    /// </summary>
    public static class PreviewTrackSurface
    {
        // ノーツが並ぶ面の半径 (NoteTrackCurve の TRACK_SURFACE_OFFSET、ファクトリがタイルを置く半径と同じ)
        const float SURFACE_RADIUS = 10f;
        /// <summary>
        /// スペースのノーツの頂点座標(単位円の中の値)を、断面の中での座標に直す倍率
        /// (各ファクトリが頂点に掛けている RADIUS と同じ。単位円が、ノーツの並ぶ面の円に当たる)
        /// </summary>
        public const float VERTEX_SCALE = SURFACE_RADIUS;
        // レーン1つぶんの角度(度)と、レーン0の中心の角度(度)。真下が -90度
        const float LANE_ANGLE = 11.25f;
        const float FIRST_LANE_ANGLE = -174.375f;
        // 光線をたどる距離の上限と刻み
        const float MAX_RAY_LENGTH = 1500f;
        const float RAY_STEP = 1f;
        const float MIN_RADIUS = 0.01f;

        /// <param name="worldRay">プレビュー世界の光線</param>
        /// <param name="noteParent">ノーツの親 (ノーツと一緒に動く地面)</param>
        /// <param name="curveRadius">軌道の半径</param>
        /// <param name="distance">当たった位置の、譜面上の距離</param>
        /// <param name="lane">当たった位置のレーン番号 (0〜15 の実数。レーンの中心で整数になる)</param>
        public static bool TryGetPoint(Ray worldRay, Transform noteParent, float curveRadius, out float distance, out float lane)
        {
            distance = 0f;
            lane = 0f;
            if (noteParent == null) { return false; }

            // ノーツと同じ座標系で計算する
            Vector3 origin = noteParent.InverseTransformPoint(worldRay.origin);
            Vector3 direction = noteParent.InverseTransformDirection(worldRay.direction);

            return TryGetPointLocal(origin, direction, curveRadius, out distance, out lane);
        }

        /// <summary>
        /// ノーツの親の座標系で表した光線が、ノーツの流れる面のどこに当たっているかを求める
        /// </summary>
        public static bool TryGetPointLocal(Vector3 origin, Vector3 direction, float curveRadius, out float distance, out float lane)
        {
            distance = 0f;
            lane = 0f;

            direction = direction.normalized;
            float safeRadius = Mathf.Max(curveRadius, MIN_RADIUS);

            // 光線を少しずつたどり、面の内側から外側へ抜ける場所を探す
            bool hasPrevious = false;
            float previousT = 0f;
            float previousValue = 0f;

            for (float t = 0f; t <= MAX_RAY_LENGTH; t += RAY_STEP)
            {
                GetSection(origin + direction * t, safeRadius, out _, out float x, out float y);
                float value = Mathf.Sqrt(x * x + y * y) - SURFACE_RADIUS;

                if (hasPrevious && previousValue < 0f && value >= 0f)
                {
                    // 抜けた場所を二分探索で絞り込む
                    float low = previousT;
                    float high = t;
                    for (int i = 0; i < 20; i++)
                    {
                        float middle = (low + high) * 0.5f;
                        GetSection(origin + direction * middle, safeRadius, out _, out float mx, out float my);
                        if (Mathf.Sqrt(mx * mx + my * my) - SURFACE_RADIUS < 0f) { low = middle; }
                        else { high = middle; }
                    }

                    GetSection(origin + direction * high, safeRadius, out float hitDistance, out float hitX, out float hitY);

                    // ノーツが並んでいるのは下半分だけ。上半分を抜けた場合は、その先を探す
                    float angle = Mathf.Atan2(hitY, hitX) * Mathf.Rad2Deg;
                    if (angle <= 0f)
                    {
                        distance = hitDistance;
                        lane = (angle - FIRST_LANE_ANGLE) / LANE_ANGLE;
                        return true;
                    }
                }

                hasPrevious = true;
                previousT = t;
                previousValue = value;
            }

            return false;
        }

        #region レール (宙に浮いているノーツ用)

        // レールをたどる刻みと、絞り込みの回数
        const float RAIL_STEP = 1f;
        const int RAIL_REFINE_COUNT = 30;

        /// <summary>
        /// プレビュー世界の点が、軌道のどの位置の断面にあるかと、断面の中での座標を求める
        /// </summary>
        public static bool TryGetSection(Vector3 worldPoint, Transform noteParent, float curveRadius, out float distance, out float x, out float y)
        {
            distance = 0f;
            x = 0f;
            y = 0f;
            if (noteParent == null) { return false; }

            GetSection(noteParent.InverseTransformPoint(worldPoint), Mathf.Max(curveRadius, MIN_RADIUS), out distance, out x, out y);
            return true;
        }

        /// <summary>
        /// 断面の中での座標を固定したまま軌道に沿って伸びる線(レール)の上で、光線にいちばん近い位置を求める
        ///
        /// スペースのノーツは面の上ではなく宙に浮いていて、時刻を変えると断面の中での位置はそのままに奥・手前へ動く。
        /// 掴んだ点を通るレールの上で「カーソルにいちばん近く見える位置」を探せば、ノーツがカーソルに付いてくる。
        /// </summary>
        /// <param name="railX">レールの、断面の中での横の座標</param>
        /// <param name="railY">レールの、断面の中での縦の座標</param>
        /// <param name="preferredDistance">直前に求めた位置。候補が複数あるとき、これにいちばん近いものを選ぶ</param>
        /// <param name="distance">レール上の位置 (譜面上の距離)</param>
        public static bool TryGetDistanceOnRail(Ray worldRay, Transform noteParent, float curveRadius, float railX, float railY, float preferredDistance, out float distance)
        {
            distance = 0f;
            if (noteParent == null) { return false; }

            Vector3 origin = noteParent.InverseTransformPoint(worldRay.origin);
            Vector3 direction = noteParent.InverseTransformDirection(worldRay.direction);

            return TryGetDistanceOnRailLocal(origin, direction, curveRadius, railX, railY, preferredDistance, out distance);
        }

        /// <summary>
        /// ノーツの親の座標系で表した光線に、レールの上でいちばん近く見える位置を求める
        /// </summary>
        public static bool TryGetDistanceOnRailLocal(Vector3 origin, Vector3 direction, float curveRadius, float railX, float railY, float preferredDistance, out float distance)
        {
            distance = 0f;

            direction = direction.normalized;
            float safeRadius = Mathf.Max(curveRadius, MIN_RADIUS);

            // 視点のある断面から奥へ向かってレールをたどり、光線からのずれが前後より小さい場所(谷)を探す
            GetSection(origin, safeRadius, out float startDistance, out _, out _);

            // 軌道は円弧なので、たどりすぎると一周して手前に戻ってくる。視点から4分の1周までに限る
            float searchLength = Mathf.Min(MAX_RAY_LENGTH, safeRadius * Mathf.PI * 0.5f);

            // 軌道は円弧に沿って曲がっているので、光線は「手前のレール」と「ずっと奥で曲がって戻ってきたレール」の
            // 両方の近くを通ることがある。谷が複数あるときは、直前に求めた位置に近い方を選ぶ
            // (ずれの小ささだけで選ぶと、カーソルを少し動かしただけで遠くへ飛んでしまう)
            bool isFound = false;
            float bestDistance = 0f;
            float bestGap = float.MaxValue;
            // 「直前に求めた位置」との近さは、円周に沿って測る
            // (直前の位置が、別の周回の距離で渡されることがある。そのまま引き算すると、どの谷も同じくらい遠いことになってしまう)
            float circumference = 2f * Mathf.PI * safeRadius;

            float previousSlope = float.MaxValue;
            float currentDistance = startDistance;
            float currentSlope = GetSlopeToRail(origin, direction, safeRadius, currentDistance, railX, railY);
            float endDistance = startDistance + searchLength;

            while (currentDistance <= endDistance)
            {
                float nextDistance = currentDistance + RAIL_STEP;
                float nextSlope = nextDistance <= endDistance
                    ? GetSlopeToRail(origin, direction, safeRadius, nextDistance, railX, railY)
                    : float.MaxValue;

                // 前後より小さければ谷 (視点より後ろの点はずれが最大値なので、谷にならない)
                if (currentSlope < float.MaxValue && currentSlope <= previousSlope && currentSlope <= nextSlope)
                {
                    float gap = Mathf.Abs(PreviewPickMath.WrapDistance(currentDistance - preferredDistance, circumference));
                    if (gap < bestGap)
                    {
                        isFound = true;
                        bestGap = gap;
                        bestDistance = currentDistance;
                    }
                }

                previousSlope = currentSlope;
                currentDistance = nextDistance;
                currentSlope = nextSlope;
            }

            // 視点より前に、レールが見えていない
            if (!isFound) { return false; }

            // 見つけた谷の前後を、さらに細かく絞り込む (3分割して狭めていく)
            float low = bestDistance - RAIL_STEP;
            float high = bestDistance + RAIL_STEP;
            for (int i = 0; i < RAIL_REFINE_COUNT; i++)
            {
                float a = low + (high - low) / 3f;
                float b = high - (high - low) / 3f;
                if (GetSlopeToRail(origin, direction, safeRadius, a, railX, railY) > GetSlopeToRail(origin, direction, safeRadius, b, railX, railY)) { low = a; }
                else { high = b; }
            }

            distance = (low + high) * 0.5f;
            return true;
        }

        /// <summary>
        /// 視点から見て、レール上の点が光線からどれだけずれているか (光線となす角度のタンジェント。0 に近いほど光線の近くに見える)
        /// 視点より後ろにある点は float.MaxValue
        /// </summary>
        private static float GetSlopeToRail(Vector3 origin, Vector3 direction, float curveRadius, float distance, float railX, float railY)
        {
            Vector3 toPoint = GetPoint(distance, railX, railY, curveRadius) - origin;

            // 光線に沿った成分と、光線から横にずれた成分
            float along = Vector3.Dot(toPoint, direction);
            if (along <= 0.0001f) { return float.MaxValue; }

            float across = (toPoint - direction * along).magnitude;
            return across / along;
        }

        /// <summary>
        /// 光線が、指定した位置の断面を通る点を求める (頂点をドラッグして動かすとき用)
        /// </summary>
        /// <param name="distance">断面の位置 (譜面上の距離)</param>
        /// <param name="x">断面の中での横の座標</param>
        /// <param name="y">断面の中での縦の座標</param>
        public static bool TryGetSectionPoint(Ray worldRay, Transform noteParent, float curveRadius, float distance, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (noteParent == null) { return false; }

            Vector3 origin = noteParent.InverseTransformPoint(worldRay.origin);
            Vector3 direction = noteParent.InverseTransformDirection(worldRay.direction);

            return TryGetSectionPointLocal(origin, direction, curveRadius, distance, out x, out y);
        }

        /// <summary>
        /// ノーツの親の座標系で表した光線が、指定した位置の断面を通る点を求める
        /// </summary>
        public static bool TryGetSectionPointLocal(Vector3 origin, Vector3 direction, float curveRadius, float distance, out float x, out float y)
        {
            x = 0f;
            y = 0f;

            float safeRadius = Mathf.Max(curveRadius, MIN_RADIUS);
            float originRadius = Mathf.Max(safeRadius - SURFACE_RADIUS, MIN_RADIUS);
            float angle = distance / safeRadius;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // 断面は、軌道の円弧の中心軸を含む平面
            // 「軸の上の点」「軸から断面に沿って軌道へ向かう向き」「平面の法線」で表す
            Vector3 axisPoint = new Vector3(0f, originRadius, 0f);
            Vector3 towardTrack = new Vector3(0f, -cos, sin);
            Vector3 normal = new Vector3(0f, sin, cos);

            // 光線と平面の交点
            float approach = Vector3.Dot(direction, normal);
            if (Mathf.Abs(approach) < 1e-6f) { return false; }

            float length = Vector3.Dot(axisPoint - origin, normal) / approach;
            if (length <= 0f) { return false; }

            Vector3 point = origin + direction * length;

            // 軸をはさんだ反対側で交わった場合は、断面の上ではない
            float distanceFromAxis = Vector3.Dot(point - axisPoint, towardTrack);
            if (distanceFromAxis <= 0f) { return false; }

            x = point.x;
            y = originRadius - distanceFromAxis;
            return true;
        }

        /// <summary>
        /// 断面の位置と、断面の中での座標から、点の位置を求める (GetSection の逆)
        /// ノーツの親の座標系での位置を返す
        /// </summary>
        public static Vector3 GetPoint(float distance, float x, float y, float curveRadius)
        {
            float originRadius = Mathf.Max(curveRadius - SURFACE_RADIUS, MIN_RADIUS);
            float angle = distance / curveRadius;
            float distanceFromAxis = originRadius - y;

            return new Vector3(x, originRadius - distanceFromAxis * Mathf.Cos(angle), distanceFromAxis * Mathf.Sin(angle));
        }

        /// <summary>
        /// 譜面上の距離とレーン番号から、ノーツの流れる面の上の点の位置を求める (TryGetPoint の逆)
        /// ノーツの親の座標系での位置を返す
        /// </summary>
        /// <param name="lane">レーン番号 (0〜15 の実数。レーンの中心で整数になる)</param>
        public static Vector3 GetLanePoint(float distance, float lane, float curveRadius)
        {
            float angle = (FIRST_LANE_ANGLE + lane * LANE_ANGLE) * Mathf.Deg2Rad;

            return GetPoint(
                distance,
                Mathf.Cos(angle) * SURFACE_RADIUS,
                Mathf.Sin(angle) * SURFACE_RADIUS,
                Mathf.Max(curveRadius, MIN_RADIUS));
        }

        #endregion

        /// <summary>
        /// 点が、軌道のどの位置の断面にあるかと、断面の中での座標を求める
        /// </summary>
        /// <param name="distance">断面の位置 (譜面上の距離)</param>
        /// <param name="x">断面の中での横の座標</param>
        /// <param name="y">断面の中での縦の座標 (円弧の中心へ向かう方向が正)</param>
        private static void GetSection(Vector3 point, float curveRadius, out float distance, out float x, out float y)
        {
            // 軌道は、(y = originRadius, z = 0) を通る横向きの軸を中心とした、半径 originRadius の円弧
            // その軸から見た角度が断面の位置、軸からの距離が断面の中での高さになる
            float originRadius = Mathf.Max(curveRadius - SURFACE_RADIUS, MIN_RADIUS);

            float toAxisY = originRadius - point.y;
            float angle = Mathf.Atan2(point.z, toAxisY);
            float distanceFromAxis = Mathf.Sqrt(toAxisY * toAxisY + point.z * point.z);

            distance = angle * curveRadius;
            x = point.x;
            y = originRadius - distanceFromAxis;
        }
    }
}
