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
        public static Mesh GenerateGroundHoldMesh(List<TimeToRange> timeToRanges, float speed, int horizontalDivisionNum, float limitLength, float radius = 10f)
        {
            Mesh mesh = new Mesh();

            // triangleのindexを32ビットにしてデカいホールドにも対応させる
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            float currentStartZ = 0;
            float maxLength = speed * (timeToRanges[^1].Timing - timeToRanges[0].Timing);
            int currentMeshIndex = 0;

            for (int i = 0; i < timeToRanges.Count - 1; i++)
            {
                float length = speed * (timeToRanges[i + 1].Timing - timeToRanges[i].Timing);

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
                    List<Vector2> uvListStart = GetUVPositionList(verticesStart, currentStartZ + localZ, maxLength);
                    List<Vector2> uvListEnd = GetUVPositionList(verticesEnd, currentStartZ + localZ + divLength, maxLength);
                    uvs.AddRange(uvListStart);
                    uvs.AddRange(uvListEnd);

                    // トライアングルインデックスを生成、代入
                    triangles.AddRange(MeshGenerator.GenerateTriangles(currentMeshIndex, verticesStart.Count, verticesEnd.Count, false));

                    localZ += divLength;
                    currentMeshIndex += verticesStart.Count + verticesEnd.Count;
                }

                currentStartZ += length;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

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
                //vertices.Add(new Vector3(f, -10, z));  // デバッグ用
            }
            return vertices;
        }

        /// <summary>
        /// 引数ラインのUV頂点座標を生成
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static List<Vector2> GetUVPositionList(List<Vector3> vertices, float baseZ, float length)
        {
            List<Vector2> uvList = new List<Vector2>();

            Vector3 firstMatch = vertices.FirstOrDefault(v => Mathf.Approximately(v.z, baseZ));
            Vector3 lastMatch = vertices.LastOrDefault(v => Mathf.Approximately(v.z, baseZ));
            float minX = firstMatch.x;
            float maxX = lastMatch.x;

            foreach (Vector3 pos in vertices)
            {
                Vector2 uv = new Vector2();
                uv.x = Mathf.Clamp((pos.x - minX) / (maxX - minX), 0f, 1f);
                uv.y = pos.z / length;
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
        public static Mesh GenerateSpaceHoldEdgeMesh(List<TimeToVertices> timeToVertices, float speed, int meshDivisionNum, float lerpThresholdDepth, bool isMeshReverse)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;    // ドデカイメッシュに対応

            List<int> triangles = new List<int>();          // 三角形形成順リスト
            List<Vector3> vertices = new List<Vector3>();   // 頂点リスト
            List<Vector2> uvs = new List<Vector2>();        // UV座標リスト
            float currentStartZ = 0;                        // 計算済みZ
            float maxLength = speed * (timeToVertices[^1].Timing - timeToVertices[0].Timing);    // Mesh全体のZ長
            int currentMeshIndex = 0;

            // 最大頂点数を調べて分割数を更新する
            foreach (var timeToVertice in timeToVertices)
            {
                if (meshDivisionNum < timeToVertice.Vertices.Length)
                { meshDivisionNum = timeToVertice.Vertices.Length; }
            }

            // 中継点の数だけ繰り返す
            for (int i = 0; i < timeToVertices.Count - 1; i++)
            {
                float depth = speed * (timeToVertices[i + 1].Timing - timeToVertices[i].Timing);    // 奥行
                int verticesCountStart = timeToVertices[i].Vertices.Count();    // 始点頂点数
                int verticesCountEnd = timeToVertices[i + 1].Vertices.Count();  // 終点頂点数
                List<Vector3> verticesStart = new List<Vector3>();              // 始点頂点リスト
                List<Vector3> verticesEnd = new List<Vector3>();                // 終点頂点リスト

                // 頂点数が違う場合は、長いほうの辺の長さを参照して点を追加する
                if (verticesCountStart != verticesCountEnd)
                {
                    List<float> ratios;    // 各頂点距離の辺全体の長さに対する割合

                    // 始点終点に同数となるような頂点を打ち、頂点リストを生成
                    ratios = Enumerable.Range(0, meshDivisionNum - verticesCountStart).Select(i => i / ((float)meshDivisionNum - verticesCountStart - 1)).ToList();
                    verticesStart = GenerateVertices(timeToVertices[i].Vertices.ToList(), ratios, currentStartZ);

                    ratios = Enumerable.Range(0, meshDivisionNum - verticesCountEnd).Select(i => i / ((float)meshDivisionNum - verticesCountEnd - 1)).ToList();
                    verticesEnd = GenerateVertices(timeToVertices[i + 1].Vertices.ToList(), ratios, currentStartZ + depth);
                }
                // 頂点数が同じときは、そのままつなげる
                else
                {
                    verticesStart = GenerateVertices(timeToVertices[i].Vertices.ToList(), new List<float>(), currentStartZ);
                    verticesEnd = GenerateVertices(timeToVertices[i + 1].Vertices.ToList(), new List<float>(), currentStartZ + depth);
                }

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
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
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
                    //lerp.z = t * zOffset;
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
                Debug.LogWarning("【Note】頂点リストが無効です（3点以上必要）");
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

            for (int i = 0; i < tess.Vertices.Length; i++)
            {
                meshVertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0);
            }

            for (int i = 0; i < tess.Elements.Length; i++)
            {
                meshTriangles[i] = tess.Elements[i];
            }

            mesh.vertices = meshVertices;
            mesh.triangles = meshTriangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
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

        /// <summary>
        /// 線を描く
        /// </summary>
        /// <param name="points"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Mesh GenerateLineMesh(List<Vector3> points, float width, bool isLoop = false)
        {
            if (points == null || points.Count < 2) return null;

            if(isLoop) { points.Add(points[0]); }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 forward = Vector3.zero;

                if (isLoop)
                {
                    if (i == 0) { forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[points.Count - 2]).normalized).normalized; }
                    else if (i == points.Count - 1) { forward = ((points[1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized; }
                    else { forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized; }
                }
                else
                {
                    if (i == 0) { forward = (points[i + 1] - points[i]).normalized; }
                    else if (i == points.Count - 1) { forward = (points[i] - points[i - 1]).normalized; }
                    else { forward = ((points[i + 1] - points[i]).normalized + (points[i] - points[i - 1]).normalized).normalized; }
                }

                Vector3 right = Vector3.Cross(forward, Vector3.forward).normalized;
                Vector3 offset = right * (width * 0.5f);

                vertices.Add(points[i] - offset);
                vertices.Add(points[i] + offset);

                uvs.Add(new Vector2(i / (float)(points.Count - 1), 0));
                uvs.Add(new Vector2(i / (float)(points.Count - 1), 1));
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
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
