using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;

namespace MeshGenerate
{
    /// <summary>
    /// GroundHoldMeshの生成をつかさどるクラス
    /// </summary>
    public class GroundHoldMeshGenerator
    {
        /// <summary>
        /// グラウンド沿いのメッシュを生成する
        /// </summary>
        /// <returns></returns>
        public static Mesh GenerateGroundHoldMesh(List<TimeToRange> timeToRanges, INotePositionCalculator posCalc, float speed, int horizontalDivisionNum, float limitLength, float curveRadius, float radius = 10f)
        {
            Mesh mesh = new Mesh();

            // triangleのindexを32ビットにしてデカいホールドにも対応させる
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            float currentStartZ = 0;
            float maxLength = speed * (posCalc.GetPosition(timeToRanges[^1].Timing) - posCalc.GetPosition(timeToRanges[0].Timing));
            int currentMeshIndex = 0;

            for (int i = 0; i < timeToRanges.Count - 1; i++)
            {
                float length = speed * (posCalc.GetPosition(timeToRanges[i + 1].Timing) - posCalc.GetPosition(timeToRanges[i].Timing));

                // それぞれの端のインデックスを代入
                float startLeft = timeToRanges[i].Range[0];
                float startRight = timeToRanges[i].Range[^1];
                float endLeft = timeToRanges[i + 1].Range[0];
                float endRight = timeToRanges[i + 1].Range[^1];

                // 傾きを計算
                float slopeLeft = (endLeft - startLeft) == 0 ? float.PositiveInfinity : length / (endLeft - startLeft);
                float slopeRight = (endRight - startRight) == 0 ? float.PositiveInfinity : length / (endRight - startRight);


                // さらにMeshを分割する
                float divLength = length / Mathf.Ceil(length / limitLength);
                float localZ = 0;
                for (int j = 0; j < Mathf.Ceil(length / limitLength); j++)
                {
                    float startLeftDiv = GetXInLinearFunction(slopeLeft, 0, localZ) + startLeft;
                    float startRightDiv = GetXInLinearFunction(slopeRight, 0, localZ) + startRight;
                    float endLeftDiv = GetXInLinearFunction(slopeLeft, 0, localZ + divLength) + startLeft;
                    float endRightDiv = GetXInLinearFunction(slopeRight, 0, localZ + divLength) + startRight;

                    // 頂点インデックスリストを作成
                    List<float> indexStart = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft < 0 ? endLeftDiv : startLeftDiv,
                        slopeRight < float.PositiveInfinity && slopeRight > 0 ? endRightDiv + 1 : startRightDiv + 1, horizontalDivisionNum,
                        new float[] { startLeftDiv, startRightDiv + 1, endLeftDiv, endRightDiv + 1 });

                    List<float> indexEnd = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft > 0 ? startLeftDiv : endLeftDiv,
                       slopeRight < float.PositiveInfinity && slopeRight < 0 ? startRightDiv + 1 : endRightDiv + 1, horizontalDivisionNum,
                       new float[] { startLeftDiv, startRightDiv + 1, endLeftDiv, endRightDiv + 1 });

                    // 頂点リストを生成
                    List<Vector3> verticesStart = GenerateVertices(indexStart, startLeftDiv, startRightDiv + 1, slopeLeft, slopeRight, currentStartZ + localZ, radius);
                    List<Vector3> verticesEnd = GenerateVertices(indexEnd, endLeftDiv, endRightDiv + 1, slopeLeft, slopeRight, currentStartZ + localZ + divLength, radius);

                    // 頂点リストの代入
                    vertices.AddRange(verticesStart);
                    vertices.AddRange(verticesEnd);

                    // UV座標の生成,代入
                    List<Vector2> uvListStart = GetUVPositionList(verticesStart, indexStart, startLeftDiv, startRightDiv + 1f, maxLength);
                    List<Vector2> uvListEnd = GetUVPositionList(verticesEnd, indexEnd, endLeftDiv, endRightDiv + 1f, maxLength);
                    uvs.AddRange(uvListStart);
                    uvs.AddRange(uvListEnd);

                    // トライアングルインデックスを生成、代入
                    triangles.AddRange(MeshGenerator.GenerateTriangles(currentMeshIndex, verticesStart.Count, verticesEnd.Count, false));

                    localZ += divLength;
                    currentMeshIndex += verticesStart.Count + verticesEnd.Count;
                }

                currentStartZ += length;
            }

            // 【ノーツ軌道】UV計算後の頂点を円弧へ曲げ、直線距離基準のUVを維持する
            NoteTrackCurve.BendVertices(vertices, curveRadius);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static float GetXInLinearFunction(float a, float b, float y)
        {
            return (y - b) / a;
        }

        /// <summary>
        /// 範囲内のメッシュ頂点リストを返す
        /// </summary>
        private static List<float> GetMeshPointList(float first, float end, int divNum, float[] addIndex)
        {
            if (divNum <= 0)
            {
                Debug.LogError("【Note】メッシュ分割数が0以下です");
                return new List<float>();
            }

            List<float> list = new List<float>();
            for (float f = first; f <= end; f += 1f / divNum)
            {
                list.Add(f);
            }

            foreach (float f in addIndex)
            {
                list.Add(f);
            }

            list.Add(end);
            return list.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>
        /// 指定したインデックスリストからメッシュの頂点座標を計算する
        /// </summary>
        private static List<Vector3> GenerateVertices(List<float> indices, float left, float right, float slopeLeft, float slopeRight, float baseZ, float radius)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (float f in indices)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = baseZ;

                if (f < left) { z += slopeLeft * (f - left); }
                else if (f > right) { z += slopeRight * (f - right); }

                vertices.Add(new Vector3(radius * Mathf.Cos(deg), radius * Mathf.Sin(deg), z));
            }
            return vertices;
        }

        /// <summary>
        /// 引数ラインのUV頂点座標を生成
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static List<Vector2> GetUVPositionList(List<Vector3> vertices, List<float> indices, float left, float right, float length)
        {
            List<Vector2> uvList = new List<Vector2>();

            if (vertices == null || indices == null) { return uvList; }
            if (vertices.Count == 0 || indices.Count == 0) { return uvList; }

            int count = Mathf.Min(vertices.Count, indices.Count);
            float width = right - left;

            for (int i = 0; i < count; i++)
            {
                Vector2 uv = new Vector2();
                uv.x = Mathf.Approximately(width, 0f) ? 0f : Mathf.Clamp01((indices[i] - left) / width);
                uv.y = length > Mathf.Epsilon ? vertices[i].z / length : 0f;
                uvList.Add(uv);
            }

            return uvList;
        }
    }

    /// <summary>
    /// SpaceHoldMeshの生成をつかさどるクラス
    /// </summary>
    public class SpaceHoldMeshGenerator
    {
        /// <summary>
        /// 立体的なオブジェクトの側面を生成する
        /// </summary>
        /// <param name="timeToVertices"></param>
        /// <param name="speed"></param>
        /// <param name="meshDivisionNum"></param>
        /// <param name="isMeshReverse"></param>
        /// <returns></returns>
        public static Mesh GenerateSpaceHoldEdgeMesh(List<TimeToVertices> timeToVertices, INotePositionCalculator posCalc, float speed, int meshDivisionNum, float lerpThresholdDepth, bool isMeshReverse, float curveRadius)
        {
            if (timeToVertices == null) { return new Mesh(); }
            if (timeToVertices.Count == 0) { return new Mesh(); }

            var depthToVerticesList = new List<DepthToVertices>();

            foreach (var t in timeToVertices)
            {
                var depth = speed * posCalc.GetPosition(t.Timing);
                var vertices = t.Vertices;
                depthToVerticesList.Add(new DepthToVertices(depth, vertices));
            }

            return GenerateSpaceHoldEdgeMesh(depthToVerticesList, meshDivisionNum, lerpThresholdDepth, isMeshReverse, curveRadius);
        }

        private static Mesh GenerateSpaceHoldEdgeMesh(List<DepthToVertices> depthToVertices, int meshDivisionNum, float lerpThresholdDepth, bool isMeshReverse, float curveRadius)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;    // ドデカイメッシュに対応

            List<int> triangles = new List<int>();          // 三角形形成順リスト
            List<Vector3> vertices = new List<Vector3>();   // 頂点リスト
            float currentStartZ = 0;                        // 計算済みZ
            int currentMeshIndex = 0;

            // 最大頂点数を調べて分割数を更新する
            float totalDepth = depthToVertices[^1].Depth - depthToVertices[0].Depth;
            List<Vector2> uvs = new List<Vector2>();
            List<Vector2> trackUVs = new List<Vector2>();
            foreach (var t in depthToVertices)
            {
                if (meshDivisionNum < t.Vertices.Length)
                { meshDivisionNum = t.Vertices.Length; }
            }

            // 中継点の数だけ繰り返す
            for (int i = 0; i < depthToVertices.Count - 1; i++)
            {
                float depth = depthToVertices[i + 1].Depth - depthToVertices[i].Depth;    // 奥行
                int verticesCountStart = depthToVertices[i].Vertices.Length;    // 始点頂点数
                int verticesCountEnd = depthToVertices[i + 1].Vertices.Length;  // 終点頂点数
                var verticesStart = new List<Vector3>();              // 始点頂点リスト
                var verticesEnd = new List<Vector3>();                // 終点頂点リスト

                if (verticesCountStart != verticesCountEnd)
                {
                    Debug.LogError($"listAとlistBの長さが一致していません: {verticesCountStart} - {verticesCountEnd}");
                    return null;
                }

                verticesStart = GenerateVertices(depthToVertices[i].Vertices.ToList(), new List<float>(), currentStartZ);
                verticesEnd = GenerateVertices(depthToVertices[i + 1].Vertices.ToList(), new List<float>(), currentStartZ + depth);

                // メッシュを線形補間
                var interpolationVerticesList = LinearInterpolationVertices(verticesStart, verticesEnd, Mathf.CeilToInt(depth / lerpThresholdDepth));

                // 線形補間された全ての頂点リスト
                for (int j = 0; j < interpolationVerticesList.Count - 1; j++)
                {
                    var verticesA = interpolationVerticesList[j];
                    var verticesB = interpolationVerticesList[j + 1];

                    // 頂点リストの代入
                    vertices.AddRange(verticesA);
                    vertices.AddRange(verticesB);
                    uvs.AddRange(GetSpaceHoldUVs(verticesA, totalDepth));
                    uvs.AddRange(GetSpaceHoldUVs(verticesB, totalDepth));
                    trackUVs.AddRange(verticesA.Select(v => new Vector2(v.z, 0f)));
                    trackUVs.AddRange(verticesB.Select(v => new Vector2(v.z, 0f)));

                    // トライアングルインデックスを生成、代入
                    var tris = MeshGenerator.GenerateTriangles(currentMeshIndex, verticesA.Count, verticesB.Count, isMeshReverse);
                    triangles.AddRange(tris);

                    currentMeshIndex += verticesA.Count + verticesB.Count;
                }

                currentStartZ += depth;
            }

            // 不正チェック
            if (!CheckValidMesh(vertices, triangles)) { return null; }

            // 計算した値をそれぞれ代入
            // 【ノーツ軌道】断面形状を維持したままSpaceHold全体を円弧へ曲げる
            NoteTrackCurve.BendVertices(vertices, curveRadius);

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.uv2 = trackUVs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static List<Vector2> GetSpaceHoldUVs(List<Vector3> stripVertices, float totalDepth)
        {
            var uvList = new List<Vector2>();
            if (stripVertices == null || stripVertices.Count == 0)
            {
                return uvList;
            }

            int lastIndex = stripVertices.Count - 1;
            float perimeter = 0f;
            for (int i = 0; i < lastIndex; i++)
            {
                perimeter += Vector3.Distance(stripVertices[i], stripVertices[i + 1]);
            }

            float accumulated = 0f;
            for (int i = 0; i < stripVertices.Count; i++)
            {
                float u;
                if (i == lastIndex)
                {
                    u = 1f;
                }
                else if (perimeter <= Mathf.Epsilon)
                {
                    u = 0f;
                }
                else
                {
                    u = accumulated / perimeter;
                }

                float v = totalDepth > Mathf.Epsilon ? stripVertices[i].z / totalDepth : 0f;
                uvList.Add(new Vector2(u, v));

                if (i < lastIndex)
                {
                    accumulated += Vector3.Distance(stripVertices[i], stripVertices[i + 1]);
                }
            }

            return uvList;
        }

        public static void VertexCountNormalizer(List<TimeToVertices> timeToVertices, int minCount)
        {
            var vertices = new List<Vector3>[timeToVertices.Count];

            // 最大頂点数を調べて分割数を更新する
            foreach (var t in timeToVertices)
            {
                if (minCount < t.Vertices.Length)
                { minCount = t.Vertices.Length; }
            }

            for (int i = 0; i < timeToVertices.Count - 1; i++)
            {
                List<float> ratios;    // 各頂点距離の辺全体の長さに対する割合
                int verticesCountStart = timeToVertices[i].Vertices.Length;    // 始点頂点数
                int verticesCountEnd = timeToVertices[i + 1].Vertices.Length;  // 終点頂点数

                // 始点終点に同数となるような頂点を打ち、頂点リストを生成
                ratios = Enumerable.Range(0, minCount - verticesCountStart).Select(i => i / ((float)minCount - verticesCountStart)).ToList();
                vertices[i] = GenerateVertices(timeToVertices[i].Vertices.ToList(), ratios, 0);

                ratios = Enumerable.Range(0, minCount - verticesCountEnd).Select(i => i / ((float)minCount - verticesCountEnd)).ToList();
                vertices[i + 1] = GenerateVertices(timeToVertices[i + 1].Vertices.ToList(), ratios, 0);
            }

            // 代入
            for(int i = 0; i < timeToVertices.Count; i++)
            {
                timeToVertices[i].Vertices = vertices[i].Select(x => (Vector2)x).ToArray();
            }

            return;
        }

        public static List<Vector2> InterpolatePoints(List<Vector2> startVertices, List<Vector2> endVertices, float t, int minCount = 10)
        {
            List<float> ratios;    // 各頂点距離の辺全体の長さに対する割合
            int verticesCountStart = startVertices.Count;
            int verticesCountEnd = endVertices.Count;

            // 最大頂点数を調べて分割数を更新する
            minCount = Mathf.Max(minCount, verticesCountStart, verticesCountEnd);

            // 始点終点に同数となるような頂点を打ち、頂点リストを生成
            ratios = Enumerable.Range(0, minCount - verticesCountStart).Select(i => i / ((float)minCount - verticesCountStart)).ToList();
            var verticesStart = GenerateVertices(startVertices, ratios, 0);

            ratios = Enumerable.Range(0, minCount - verticesCountEnd).Select(i => i / ((float)minCount - verticesCountEnd)).ToList();
            var verticesEnd = GenerateVertices(endVertices, ratios, 0);


            // 中間点を生成
            var result = new List<Vector2>();

            for (int i = 0; i < verticesStart.Count - 1; i++)
            {
                Vector2 pointA = verticesStart[i];
                Vector2 pointB = verticesEnd[i];

                // 線分ABの中の比率 ratio の点を計算（線形補間）
                Vector2 interpolated = Vector2.Lerp(pointA, pointB, t);
                result.Add(interpolated);
            }

            return result;
        }

        /// <summary>
        /// 線形補間して滑らかなメッシュにする(始点終点共に同じ長さであること)
        /// </summary>
        /// <param name="startVertices"></param>
        /// <param name="endVertices"></param>
        /// <param name="interpolateSteps"></param>
        /// <returns></returns>
        static List<List<Vector3>> LinearInterpolationVertices(List<Vector3> startVertices, List<Vector3> endVertices, int interpolateSteps = 10)
        {
            if(startVertices.Count != endVertices.Count) { return null; }

            var verticesList = new List<List<Vector3>>();

            for (int i = 0; i <= interpolateSteps; i++)
            {
                List<Vector3> vertices = new List<Vector3>();
                float t = i / (float)interpolateSteps;

                for (int j = 0; j < startVertices.Count; j++)
                {
                    Vector3 from = startVertices[j];
                    Vector3 to = endVertices[j];
                    Vector3 lerp = Vector3.Lerp(from, to, t);
                    vertices.Add(lerp);
                }

                verticesList.Add(vertices);
                //Debug.Log($"LinearInterpolation: {vertices.Count}");
            }

            return verticesList;
        }

        /// <summary>
        /// 不正な値がないかチェック
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="triangles"></param>
        /// <returns></returns>
        static bool CheckValidMesh(List<Vector3> vertices, List<int> triangles)
        {
            // 無効数チェック
            foreach (var v in vertices)
            {
                if (HasInvalidVertex(v))
                {
                    Debug.LogError("【Mesh】Invalid vertex found: " + v);
                    return false;
                }
            }

            // 無効数チェック
            foreach (var i in triangles)
            {
                if (i < 0 || i >= vertices.Count)
                {
                    Debug.LogError($"【Mesh】Invalid triangle index: {i}/{vertices.Count}");
                    return false;
                }
            }

            return true;
        }

        static bool HasInvalidVertex(Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
        }

        /// <summary>
        /// 頂点リスト(Mesh生成に最低限必要な頂点)と打点割合リストに従い頂点リストを返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="ratios"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private static List<Vector3> GenerateVertices(List<Vector2> vertices, List<float> ratios, float z)
        {
            List<Vector3> result = new List<Vector3>();

            // 何も入ってなかったらどうしようもないので返す
            if (ratios == null) { return result; }
            if (vertices == null) { return result; }

            // verticesが一点だったら一点に集約させる
            if (vertices.Count < 2)
            {
                for (int i = 0; i < ratios.Count; i++)
                {
                    result.Add(new Vector3(vertices[0].x, vertices[0].y, z));
                }
                return result;
            }

            // 各辺の長さを計算し、全体の長さを求める
            List<float> segmentLengths = new List<float>();
            float totalLength = 0f;

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                float segmentLength = Vector2.Distance(vertices[i], vertices[i + 1]);
                segmentLengths.Add(segmentLength);
                totalLength += segmentLength;
            }

            // verticesが一点だったら一点に集約させる
            if (Mathf.Approximately(totalLength, 0f))
            {
                for (int i = 0; i < ratios.Count; i++)
                {
                    result.Add(new Vector3(vertices[0].x, vertices[0].y, z));
                }
                return result;
            }

            result = new List<Vector3>();

            // 割合リストに従って対応する点を求める
            foreach (float ratio in ratios)
            {
                float targetLength = ratio * totalLength;
                float accumulatedLength = 0f;

                for (int i = 0; i < segmentLengths.Count; i++)
                {
                    float nextAccumulatedLength = accumulatedLength + segmentLengths[i];

                    if (targetLength <= nextAccumulatedLength)
                    {
                        float t = (targetLength - accumulatedLength) / segmentLengths[i];
                        Vector3 point = Vector2.Lerp(vertices[i], vertices[i + 1], t);
                        point = new Vector3(point.x, point.y, z);
                        result.Add(point);
                        break;
                    }

                    accumulatedLength = nextAccumulatedLength;
                }
            }

            result.AddRange(vertices.Select(v => new Vector3(v.x, v.y, z)).ToList());
            result = result.OrderBy(p => GetDistanceFromStart(vertices, p)).ToList();
            // 最後に最初の要素をつける
            result.Add(result[0]);

            return result;
        }

        /// <summary>
        /// 頂点リストに従ってpointの地点を割合で返す
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private static float GetDistanceFromStart(List<Vector2> vertices, Vector2 point)
        {
            float distance = 0f;

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                float segmentLength = Vector2.Distance(vertices[i], vertices[i + 1]);

                if (Vector2.Distance(vertices[i], point) + Vector2.Distance(vertices[i + 1], point) <= segmentLength + 0.0001f)
                {
                    return distance + Vector2.Distance(vertices[i], point);
                }

                distance += segmentLength;
            }

            return distance;
        }

    }

    /// <summary>
    /// その他メッシュの生成をつかさどるクラス
    /// </summary>
    public class MeshGenerator
    {
        /// <summary>
        /// 引数頂点リストからメッシュ(自己交差なし)の生成
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static Mesh GenerateMesh(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                Debug.LogWarning("【Mesh】頂点リストが無効です（3点以上必要）");
                return null;
            }

            // LibTessDotNetの初期化
            Tess tess = new Tess();
            ContourVertex[] contour = new ContourVertex[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                contour[i] = new ContourVertex
                {
                    Position = new Vec3(vertices[i].x, vertices[i].y, vertices[i].z)
                };
            }

            // 頂点リストを輪郭として追加
            tess.AddContour(contour, ContourOrientation.Original);

            // 三角形分割を実行
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            // Mesh作成
            Mesh mesh = new Mesh();
            Vector3[] meshVertices = new Vector3[tess.Vertices.Length];
            int[] meshTriangles = new int[tess.Elements.Length];
            Vector2[] meshUvs = new Vector2[tess.Vertices.Length];
            Vector2 min = GetMinXY(vertices);
            Vector2 max = GetMaxXY(vertices);
            Vector2 size = max - min;
            float invWidth = Mathf.Abs(size.x) > Mathf.Epsilon ? 1f / size.x : 0f;
            float invHeight = Mathf.Abs(size.y) > Mathf.Epsilon ? 1f / size.y : 0f;

            for (int i = 0; i < tess.Vertices.Length; i++)
            {
                meshVertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0);
                Vector2 pos = meshVertices[i];
                meshUvs[i] = new Vector2((pos.x - min.x) * invWidth, (pos.y - min.y) * invHeight);
            }

            for (int i = 0; i < tess.Elements.Length; i++)
            {
                meshTriangles[i] = tess.Elements[i];
            }

            mesh.vertices = meshVertices;
            mesh.triangles = meshTriangles;
            mesh.uv = meshUvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Vector2 GetMinXY(List<Vector3> vertices)
        {
            Vector2 min = vertices[0];
            for (int i = 1; i < vertices.Count; i++)
            {
                min.x = Mathf.Min(min.x, vertices[i].x);
                min.y = Mathf.Min(min.y, vertices[i].y);
            }

            return min;
        }

        private static Vector2 GetMaxXY(List<Vector3> vertices)
        {
            Vector2 max = vertices[0];
            for (int i = 1; i < vertices.Count; i++)
            {
                max.x = Mathf.Max(max.x, vertices[i].x);
                max.y = Mathf.Max(max.y, vertices[i].y);
            }

            return max;
        }

        /// <summary>
        /// 4点からメッシュを生成する
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Mesh GenerateMesh(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var mesh = new Mesh();

            mesh.vertices = new[] { a, b, c, d };
            mesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f), // a
                new Vector2(1f, 0f), // b
                new Vector2(1f, 1f), // c
                new Vector2(0f, 1f)  // d
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 頂点リストを奥にずらした分の箱型メッシュを生成する
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static Mesh GenerateMeshWithDepth(List<Vector2> vertices, float depth)
        {
            if (vertices == null || vertices.Count < 3)
            {
                Debug.LogWarning("【Mesh】頂点リストが無効です（3点以上必要）");
                return null;
            }

            Tess tess = new Tess();
            ContourVertex[] contour = new ContourVertex[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                contour[i] = new ContourVertex()
                {
                    Position = new Vec3(vertices[i].x, vertices[i].y, 0)
                };
            }

            tess.AddContour(contour, ContourOrientation.Original);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            // --- 頂点を組み立て ---
            List<Vector3> meshVerts = new List<Vector3>();
            List<int> meshTris = new List<int>();
            List<Vector2> meshUvs = new List<Vector2>();

            float half = depth * 0.5f;
            float minX = vertices.Min(v => v.x);
            float maxX = vertices.Max(v => v.x);
            float minY = vertices.Min(v => v.y);
            float maxY = vertices.Max(v => v.y);
            float width = Mathf.Max(maxX - minX, Mathf.Epsilon);
            float height = Mathf.Max(maxY - minY, Mathf.Epsilon);
            float invWidth = 1f / width;
            float invHeight = 1f / height;

            // 前面(Z=-half)
            int frontStart = 0;
            for (int i = 0; i < tess.Vertices.Length; i++)
            {
                var pos2D = new Vector2(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y);
                meshVerts.Add(new Vector3(pos2D.x, pos2D.y, -half));
                meshUvs.Add(GetPlanarUV(pos2D, minX, minY, invWidth, invHeight));
            }

            // 背面(Z=+half)
            int backStart = meshVerts.Count;
            for (int i = 0; i < tess.Vertices.Length; i++)
            {
                var pos2D = new Vector2(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y);
                meshVerts.Add(new Vector3(pos2D.x, pos2D.y, half));
                meshUvs.Add(GetPlanarUV(pos2D, minX, minY, invWidth, invHeight));
            }

            // --- 前面ポリゴン ---
            for (int i = 0; i < tess.ElementCount; i++)
            {
                int i0 = tess.Elements[i * 3 + 0];
                int i1 = tess.Elements[i * 3 + 1];
                int i2 = tess.Elements[i * 3 + 2];

                if (i0 == -1 || i1 == -1 || i2 == -1) continue;

                meshTris.Add(frontStart + i0);
                meshTris.Add(frontStart + i1);
                meshTris.Add(frontStart + i2);
            }

            // --- 背面ポリゴン（反転）---
            for (int i = 0; i < tess.ElementCount; i++)
            {
                int i0 = tess.Elements[i * 3 + 0];
                int i1 = tess.Elements[i * 3 + 1];
                int i2 = tess.Elements[i * 3 + 2];

                if (i0 == -1 || i1 == -1 || i2 == -1) continue;

                // 逆順にして裏向きに
                meshTris.Add(backStart + i2);
                meshTris.Add(backStart + i1);
                meshTris.Add(backStart + i0);
            }

            // --- 側面 ---
            float perimeter = GetPerimeterLength(vertices);
            float accumulatedLength = 0f;

            for (int i = 0; i < vertices.Count; i++)
            {
                int next = (i + 1) % vertices.Count;
                float edgeLength = Vector2.Distance(vertices[i], vertices[next]);
                float startU = perimeter > Mathf.Epsilon ? accumulatedLength / perimeter : 0f;
                float endU = perimeter > Mathf.Epsilon ? (accumulatedLength + edgeLength) / perimeter : 0f;

                Vector3 v0 = new Vector3(vertices[i].x, vertices[i].y, -half);
                Vector3 v1 = new Vector3(vertices[next].x, vertices[next].y, -half);
                Vector3 v2 = new Vector3(vertices[next].x, vertices[next].y, +half);
                Vector3 v3 = new Vector3(vertices[i].x, vertices[i].y, +half);

                int baseIndex = meshVerts.Count;
                meshVerts.Add(v0); // 0 下奥
                meshVerts.Add(v1); // 1 下次奥
                meshVerts.Add(v2); // 2 上次手前
                meshVerts.Add(v3); // 3 上手前

                // 側面は周回方向をU、奥行きをVとして同じUV空間に収める
                meshUvs.Add(new Vector2(startU, 0f));
                meshUvs.Add(new Vector2(endU, 0f));
                meshUvs.Add(new Vector2(endU, 1f));
                meshUvs.Add(new Vector2(startU, 1f));

                meshTris.Add(baseIndex + 0);
                meshTris.Add(baseIndex + 2);
                meshTris.Add(baseIndex + 1);

                meshTris.Add(baseIndex + 0);
                meshTris.Add(baseIndex + 3);
                meshTris.Add(baseIndex + 2);

                accumulatedLength += edgeLength;
            }

            // --- Mesh生成 ---
            Mesh mesh = new Mesh();
            mesh.SetVertices(meshVerts);
            mesh.SetTriangles(meshTris, 0);
            mesh.SetUVs(0, meshUvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Vector2 GetPlanarUV(Vector2 position, float minX, float minY, float invWidth, float invHeight)
        {
            return new Vector2((position.x - minX) * invWidth, (position.y - minY) * invHeight);
        }

        private static float GetPerimeterLength(List<Vector2> vertices)
        {
            if (vertices == null || vertices.Count < 2) { return 0f; }

            float perimeter = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                int next = (i + 1) % vertices.Count;
                perimeter += Vector2.Distance(vertices[i], vertices[next]);
            }

            return perimeter;
        }
        /// <summary>
        /// メッシュのトライアングルインデックスを生成
        /// </summary>
        public static List<int> GenerateTriangles(int startIndex, int countStart, int countEnd, bool isReverse)
        {
            List<int> triangles = new List<int>();
            int halfCount = Mathf.Min(countStart, countEnd) - 1;

            for (int i = 0; i < halfCount; i++)
            {
                triangles.Add(isReverse ? startIndex + i : startIndex + i + 1);
                triangles.Add(isReverse ? startIndex + i + 1 : startIndex + i);
                triangles.Add(startIndex + i + countStart);

                triangles.Add(isReverse ? startIndex + i + countStart : startIndex + i + 1);
                triangles.Add(isReverse ? startIndex + i + 1 : startIndex + i + countStart);
                triangles.Add(startIndex + i + countStart + 1);
            }
            return triangles;
        }

        /// <summary>
        /// 正規化
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Vector3 Normalize(Vector3 vector, Vector3 center, float radius)
        {
            return new Vector3((vector.x - center.x) * radius, (vector.y - center.y) * radius, vector.z);
        }

        public static Vector3 GetEularAngle(Vector3 pos, Vector3 target)
        {
            // 点Aへの方向を計算
            Vector2 direction = target - pos;

            // Atan2で角度を計算 (ラジアン)
            float angleRad = Mathf.Atan2(direction.y, direction.x);

            // ラジアンを度数法に変換
            float angleDeg = angleRad * Mathf.Rad2Deg;

            return new Vector3(0, 0, angleDeg);
        }

        /// <summary>
        /// 線を描く
        /// </summary>
        /// <param name="points"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Mesh GenerateLineMesh(List<Vector3> points, float width, bool isLoop = false)
        {
            if (points == null || points.Count < 2) { return null; }

            // ループ処理
            if (isLoop)
            {
                // Add の副作用を避けるためコピーを作る
                points = new List<Vector3>(points);
                points.Add(points[0]);
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // 線の全長を計算（UVの比率に使う）
            float totalLength = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                totalLength += Vector3.Distance(points[i - 1], points[i]);
            }

            float accumulatedLength = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward = Vector3.zero;

                if (isLoop)
                {
                    if (i == 0)
                        forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[points.Count - 2]).normalized).normalized;
                    else if (i == points.Count - 1)
                        forward = ((points[1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized;
                    else
                        forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized;
                }
                else
                {
                    if (i == 0)
                        forward = (points[i + 1] - points[i]).normalized;
                    else if (i == points.Count - 1)
                        forward = (points[i] - points[i - 1]).normalized;
                    else
                        forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized;
                }

                Vector3 right = Vector3.Cross(forward, Vector3.forward).normalized;
                Vector3 offset = right * (width * 0.5f);

                vertices.Add(points[i] - offset);
                vertices.Add(points[i] + offset);

                // ---- UV計算 ----
                if (i > 0)
                {
                    accumulatedLength += Vector3.Distance(points[i - 1], points[i]);
                }
                float u = (totalLength > 0f) ? (accumulatedLength / totalLength) : 0f;

                uvs.Add(new Vector2(0, u));
                uvs.Add(new Vector2(1, u));
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = i * 2;
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }

    public class SpaceHoldShadowMeshGenerator
    {
        const float MinRadius = 0.01f;

        public static Mesh GenerateSpaceHoldShadowMesh(
            List<TimeToVertices> timeToVertices,
            INotePositionCalculator posCalc,
            float speed,
            int shadowDivisionNum,
            float lerpThresholdDepth,
            float trackCurveRadius,
            float halfPipeRadius,
            float radiusOffset)
        {
            if (timeToVertices == null) { return new Mesh(); }
            if (timeToVertices.Count == 0) { return new Mesh(); }
            if (posCalc == null) { return new Mesh(); }

            List<DepthToVertices> depthToVertices = timeToVertices
                .Select(t => new DepthToVertices(speed * posCalc.GetPosition(t.Timing), t.Vertices))
                .ToList();

            return GenerateShadowMeshFromOutline(depthToVertices, shadowDivisionNum, lerpThresholdDepth, trackCurveRadius, halfPipeRadius, radiusOffset);
        }

        private static Mesh GenerateShadowMeshFromOutline(
            List<DepthToVertices> depthToVertices,
            int shadowDivisionNum,
            float lerpThresholdDepth,
            float trackCurveRadius,
            float halfPipeRadius,
            float radiusOffset)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            float currentStartZ = 0f;
            int currentMeshIndex = 0;
            float totalDepth = depthToVertices[^1].Depth - depthToVertices[0].Depth;

            for (int i = 0; i < depthToVertices.Count - 1; i++)
            {
                float depth = depthToVertices[i + 1].Depth - depthToVertices[i].Depth;
                List<Vector3> startOutline = GenerateSectionOutline(depthToVertices[i].Vertices, currentStartZ, shadowDivisionNum, halfPipeRadius, radiusOffset);
                List<Vector3> endOutline = GenerateSectionOutline(depthToVertices[i + 1].Vertices, currentStartZ + depth, shadowDivisionNum, halfPipeRadius, radiusOffset);
                int interpolateSteps = Mathf.Max(1, Mathf.CeilToInt(depth / Mathf.Max(lerpThresholdDepth, 0.001f)));

                for (int j = 0; j < interpolateSteps; j++)
                {
                    float tA = j / (float)interpolateSteps;
                    float tB = (j + 1) / (float)interpolateSteps;
                    List<Vector3> verticesA = GenerateInterpolatedOutline(startOutline, endOutline, tA, trackCurveRadius, halfPipeRadius, radiusOffset);
                    List<Vector3> verticesB = GenerateInterpolatedOutline(startOutline, endOutline, tB, trackCurveRadius, halfPipeRadius, radiusOffset);

                    vertices.AddRange(verticesA);
                    vertices.AddRange(verticesB);
                    uvs.AddRange(GetShadowUVs(verticesA, totalDepth));
                    uvs.AddRange(GetShadowUVs(verticesB, totalDepth));
                    triangles.AddRange(MeshGenerator.GenerateTriangles(currentMeshIndex, verticesA.Count, verticesB.Count, false));

                    currentMeshIndex += verticesA.Count + verticesB.Count;
                }

                currentStartZ += depth;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<Vector3> GenerateSectionOutline(
            Vector2[] sectionVertices,
            float z,
            int shadowDivisionNum,
            float halfPipeRadius,
            float radiusOffset)
        {
            if (sectionVertices == null || sectionVertices.Length == 0)
            {
                return new List<Vector3> { new Vector3(0f, 0f, z), new Vector3(0f, 0f, z) };
            }

            float sourceRadius = Mathf.Max(halfPipeRadius, MinRadius);
            float projectionRadius = Mathf.Max(halfPipeRadius - Mathf.Max(0f, radiusOffset), MinRadius);
            float minX = Mathf.Clamp(sectionVertices.Min(v => v.x), -sourceRadius, sourceRadius);
            float maxX = Mathf.Clamp(sectionVertices.Max(v => v.x), -sourceRadius, sourceRadius);
            float minAngle = GetHalfPipeAngle(minX, sourceRadius);
            float maxAngle = GetHalfPipeAngle(maxX, sourceRadius);
            int divisionCount = Mathf.Max(1, shadowDivisionNum);
            List<Vector3> outline = new List<Vector3>();
            for (int i = 0; i <= divisionCount; i++)
            {
                float t = i / (float)divisionCount;
                float angle = Mathf.Lerp(minAngle, maxAngle, t);
                outline.Add(new Vector3(
                    projectionRadius * Mathf.Sin(angle),
                    -projectionRadius * Mathf.Cos(angle),
                    z));
            }

            return outline;
        }

        private static float GetHalfPipeAngle(float x, float radius)
        {
            return Mathf.Asin(Mathf.Clamp(x / Mathf.Max(radius, MinRadius), -1f, 1f));
        }

        private static List<Vector3> GenerateInterpolatedOutline(
            List<Vector3> startOutline,
            List<Vector3> endOutline,
            float t,
            float trackCurveRadius,
            float halfPipeRadius,
            float radiusOffset)
        {
            List<Vector3> result = new List<Vector3>();
            int count = Mathf.Min(startOutline.Count, endOutline.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 vertex = Vector3.Lerp(startOutline[i], endOutline[i], t);
                result.Add(ProjectToHalfPipe(vertex, trackCurveRadius, halfPipeRadius, radiusOffset));
            }

            return result;
        }

        private static List<Vector2> GetShadowUVs(List<Vector3> stripVertices, float totalDepth)
        {
            List<Vector2> uvList = new List<Vector2>();
            int lastIndex = stripVertices.Count - 1;
            for (int i = 0; i < stripVertices.Count; i++)
            {
                float u = lastIndex > 0 ? i / (float)lastIndex : 0f;
                float v = totalDepth > Mathf.Epsilon ? stripVertices[i].z / totalDepth : 0f;
                uvList.Add(new Vector2(u, v));
            }

            return uvList;
        }

        private static Vector3 ProjectToHalfPipe(Vector3 vertex, float trackCurveRadius, float halfPipeRadius, float radiusOffset)
        {
            float safeHalfPipeRadius = Mathf.Max(halfPipeRadius - Mathf.Max(0f, radiusOffset), MinRadius);

            float x = Mathf.Clamp(vertex.x, -safeHalfPipeRadius, safeHalfPipeRadius);
            float baseY = -Mathf.Sqrt(safeHalfPipeRadius * safeHalfPipeRadius - x * x);
            Vector3 halfPipeVertex = new Vector3(x, baseY, vertex.z);
            return NoteTrackCurve.BendVertex(halfPipeVertex, trackCurveRadius);
        }
    }

    public class SpaceBreakShadowMeshGenerator
    {
        const float MinRadius = 0.01f;

        public static Mesh GenerateSpaceBreakShadowMesh(
            List<Vector2> vertices,
            float depth,
            int shadowDivisionNum,
            float trackCurveRadius,
            float halfPipeRadius,
            float radiusOffset)
        {
            if (vertices == null || vertices.Count == 0) { return new Mesh(); }

            float halfDepth = depth * 0.5f;
            List<Vector3> backOutline = GenerateSectionOutline(vertices, halfDepth, shadowDivisionNum, halfPipeRadius, radiusOffset);
            List<Vector3> frontOutline = GenerateSectionOutline(vertices, -halfDepth, shadowDivisionNum, halfPipeRadius, radiusOffset);

            List<Vector3> meshVertices = new List<Vector3>();
            meshVertices.AddRange(ProjectOutline(backOutline, trackCurveRadius));
            meshVertices.AddRange(ProjectOutline(frontOutline, trackCurveRadius));

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(meshVertices);
            mesh.SetTriangles(MeshGenerator.GenerateTriangles(0, backOutline.Count, frontOutline.Count, false), 0);
            mesh.SetUVs(0, GetShadowUVs(meshVertices, depth));
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<Vector3> GenerateSectionOutline(
            List<Vector2> vertices,
            float z,
            int shadowDivisionNum,
            float halfPipeRadius,
            float radiusOffset)
        {
            float sourceRadius = Mathf.Max(halfPipeRadius, MinRadius);
            float projectionRadius = Mathf.Max(halfPipeRadius - Mathf.Max(0f, radiusOffset), MinRadius);
            float minX = Mathf.Clamp(vertices.Min(v => v.x), -sourceRadius, sourceRadius);
            float maxX = Mathf.Clamp(vertices.Max(v => v.x), -sourceRadius, sourceRadius);
            float minAngle = GetHalfPipeAngle(minX, sourceRadius);
            float maxAngle = GetHalfPipeAngle(maxX, sourceRadius);
            int divisionCount = Mathf.Max(1, shadowDivisionNum);

            List<Vector3> outline = new List<Vector3>();
            for (int i = 0; i <= divisionCount; i++)
            {
                float t = i / (float)divisionCount;
                float angle = Mathf.Lerp(minAngle, maxAngle, t);
                outline.Add(new Vector3(
                    projectionRadius * Mathf.Sin(angle),
                    -projectionRadius * Mathf.Cos(angle),
                    z));
            }

            return outline;
        }

        private static float GetHalfPipeAngle(float x, float radius)
        {
            return Mathf.Asin(Mathf.Clamp(x / Mathf.Max(radius, MinRadius), -1f, 1f));
        }

        private static List<Vector3> ProjectOutline(List<Vector3> outline, float trackCurveRadius)
        {
            List<Vector3> result = new List<Vector3>();
            foreach (Vector3 vertex in outline)
            {
                result.Add(NoteTrackCurve.BendVertex(vertex, trackCurveRadius));
            }

            return result;
        }

        private static List<Vector2> GetShadowUVs(List<Vector3> vertices, float depth)
        {
            List<Vector2> uvs = new List<Vector2>();
            int stripCount = vertices.Count / 2;
            float invDepth = depth > Mathf.Epsilon ? 1f / depth : 0f;

            for (int i = 0; i < vertices.Count; i++)
            {
                int indexInStrip = i % stripCount;
                float u = stripCount > 1 ? indexInStrip / (float)(stripCount - 1) : 0f;
                float v = (vertices[i].z + depth * 0.5f) * invDepth;
                uvs.Add(new Vector2(u, v));
            }

            return uvs;
        }
    }
}
