using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Deform;

namespace Refactoring
{
    public class NoteFactory_SpaceHoldMesh : NoteFactory<NoteData_SpaceHoldMesh>
    {
        readonly Vector3 CENTER_PIVOT = Vector3.zero;
        readonly float RADIUS = 10f; 

        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("meshの分割数")]
        [SerializeField] int meshDivisionNum = 10;

        [Header("三角形の最大高さ(長さ)")]
        [SerializeField] float maxTriangleLength = 0.5f;

        INoteSpawnDataOptionHolder optionHolder;
        ISliderInputGetter sliderInputGetter;
        IJudgementRecorder judgementRecorder;
        ITimeGetter timer;
        GameObject groundObject;
        Deformer groundDeformer;

        public override void Initialize(NoteFactoryInitializingData initializingData)
        {
            this.optionHolder = initializingData.OptionHolder;
            this.groundObject = initializingData.GroundObject;
            this.groundDeformer = initializingData.GroundDeformer;
            this.sliderInputGetter = initializingData.SliderInputGetter;
            this.judgementRecorder = initializingData.JudgementRecorder;
            this.timer = initializingData.Timer;
        }

        public override NoteObject<NoteData_SpaceHoldMesh> Spawn(NoteData_SpaceHoldMesh data)
        {
            // 生成
            NoteObject<NoteData_SpaceHoldMesh> note = GenerateNoteInstance(ConvertNoteData(data));

            // 位置調整
            SetTransform(note, data);

            // 初期化
            note.Initialize(data);

            return note;
        }

        /// <summary>
        /// ノートデータにさらなる情報を追加
        /// </summary>
        /// <param name="data"></param>
        private NoteData_SpaceHoldMesh ConvertNoteData(NoteData_SpaceHoldMesh data)
        {
            // ノーツデータにいろいろ追加
            data.SliderInput = this.sliderInputGetter;
            data.Timer = this.timer;

            return data;
        }

        /// <summary>
        /// ノーツをインスタンス化して返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private NoteObject<NoteData_SpaceHoldMesh> GenerateNoteInstance(NoteData_SpaceHoldMesh data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクト(表)を生成
            GameObject noteObj = GenerateMeshObject(data, false);
            noteObj.transform.SetParent(origin.transform);

            // ノーツオブジェクト(裏)を生成
            GameObject noteObj_ = GenerateMeshObject(data, true);
            noteObj_.transform.SetParent(origin.transform);

            // コンポーネントを取得
            NoteObject<NoteData_SpaceHoldMesh> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldMesh>>();

            return note;
        }

        /// <summary>
        /// ホールドのメッシュ部分の生成
        /// </summary>
        private GameObject GenerateMeshObject(NoteData_SpaceHoldMesh noteData, bool isMeshReverse)
        {
            GameObject obj = new GameObject("Mesh");
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            meshFilter.mesh = mesh;

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            float currentStartZ = 0;
            float maxLength = optionHolder.NoteSpeed * (noteData.TimeToVertices[^1].Timing - noteData.TimeToVertices[0].Timing);
            int currentMeshIndex = 0;

            for (int i = 0; i < noteData.TimeToVertices.Count - 1; i++)
            {
                float length = optionHolder.NoteSpeed * (noteData.TimeToVertices[i + 1].Timing - noteData.TimeToVertices[i].Timing);

                // 各頂点距離の辺全体の長さに対する割合
                int verticesCount;
                List<float> ratios;

                // 頂点リストを生成
                verticesCount = noteData.TimeToVertices[i].Vertices.Count();
                ratios = Enumerable.Range(0, meshDivisionNum - verticesCount).Select(i => i / ((float)meshDivisionNum - verticesCount - 1)).ToList();
                List<Vector3> verticesStart = GenerateVertices(noteData.TimeToVertices[i].Vertices.ToList(), ratios, currentStartZ);

                verticesCount = noteData.TimeToVertices[i + 1].Vertices.Count();
                ratios = Enumerable.Range(0, meshDivisionNum - verticesCount).Select(i => i / ((float)meshDivisionNum - verticesCount - 1)).ToList();
                List<Vector3> verticesEnd = GenerateVertices(noteData.TimeToVertices[i + 1].Vertices.ToList(), ratios, currentStartZ + length);

                // 頂点リストの代入
                vertices.AddRange(verticesStart);
                vertices.AddRange(verticesEnd);

                // UV座標の生成,代入
                //List<Vector2> uvListStart = GetUVPositionList(verticesStart, currentStartZ, maxLength);
                //List<Vector2> uvListEnd = GetUVPositionList(verticesEnd, currentStartZ + length, maxLength);
                //uvs.AddRange(uvListStart);
                //uvs.AddRange(uvListEnd);

                // トライアングルインデックスを生成、代入
                triangles.AddRange(GenerateTriangles(currentMeshIndex, verticesStart.Count, verticesEnd.Count, isMeshReverse));

                currentStartZ += length;
                currentMeshIndex += verticesStart.Count + verticesEnd.Count;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            //mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
            return obj;
        }

        public List<Vector3> GenerateVertices(List<Vector2> vertices, List<float> ratios, float z)
        {
            List<Vector3> result = new List<Vector3>();

            // 何も入ってなかったらどうしようもないので返す
            if (ratios == null) { return result; }
            if (ratios.Count == 0) { return result; }
            if (vertices == null) { return result; }

            // verticesが一点だったら一点に集約させる
            if (vertices.Count < 2) 
            {
                for (int i = 0; i < ratios.Count; i++) 
                { 
                    result.Add(new Vector3((vertices[0].x - CENTER_PIVOT.x) * RADIUS, (vertices[0].y - CENTER_PIVOT.y) * RADIUS, z));
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
                    result.Add(new Vector3((vertices[0].x - CENTER_PIVOT.x) * RADIUS, (vertices[0].y - CENTER_PIVOT.y) * RADIUS, z));
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

            result.AddRange(vertices.Select(v => new Vector3(v.x ,v.y, z)).ToList());
            result = result.OrderBy(p => GetDistanceFromStart(vertices, p)).ToList();
            // 最後に最初の要素をつける
            result.Add(result[0]);

            // 正規化
            result = result.Select(v => new Vector3((v.x - CENTER_PIVOT.x) * RADIUS, (v.y - CENTER_PIVOT.y) * RADIUS, z)).ToList();

            return result;
        }

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

        /// <summary>
        /// 引数ラインのUV頂点座標を生成
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private List<Vector2> GetUVPositionList(List<Vector3> vertices, float baseZ, float length)
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

        /// <summary>
        /// メッシュのトライアングルインデックスを生成
        /// </summary>
        private List<int> GenerateTriangles(int startIndex, int countStart, int countEnd, bool isReverse)
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
        /// 位置調整など
        /// </summary>
        private void SetTransform(NoteObject<NoteData_SpaceHoldMesh> note, NoteData_SpaceHoldMesh data)
        {
            // 位置の調整
            note.transform.position = new Vector3(
                note.transform.position.x,
                note.transform.position.y,
                optionHolder.NoteSpeed * data.Timing
                );

            // 動く地面を親登録
            note.transform.SetParent(groundObject.transform);
        }
    }
}
