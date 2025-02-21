using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Deform;

namespace Refactoring
{
    public class NoteFactory_HoldMeshSuper : NoteFactory<NoteData_HoldMeshSuper>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("meshの分割数")]
        [SerializeField] int meshDivisionNum = 10;

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

        public override NoteObject<NoteData_HoldMeshSuper> Spawn(NoteData_HoldMeshSuper data)
        {
            // 生成
            NoteObject<NoteData_HoldMeshSuper> note = GenerateNoteInstance(ConvertNoteData(data));

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
        private NoteData_HoldMeshSuper ConvertNoteData(NoteData_HoldMeshSuper data)
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
        private NoteObject<NoteData_HoldMeshSuper> GenerateNoteInstance(NoteData_HoldMeshSuper data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクトを生成
            GameObject noteObj = GenerateMeshObject(data);

            // originにくっつける
            noteObj.transform.SetParent(origin.transform);

            // コンポーネントを取得
            NoteObject<NoteData_HoldMeshSuper> note = origin.GetComponent<NoteObject<NoteData_HoldMeshSuper>>();

            return note;
        }

        /// <summary>
        /// ホールドのメッシュ部分の生成
        /// </summary>
        private GameObject GenerateMeshObject(NoteData_HoldMeshSuper noteData)
        {
            GameObject obj = new GameObject("Mesh");
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            meshFilter.mesh = mesh;

            List<int> triangles = new List<int>();
            List<Vector3> vertices = new List<Vector3>();
            float currentStartZ = 0;
            int currentMeshIndex = 0;

            for (int i = 0; i < noteData.TimeToRanges.Count - 1; i++)
            {
                float length = optionHolder.NoteSpeed * (noteData.TimeToRanges[i + 1].Timing - noteData.TimeToRanges[i].Timing);

                // それぞれの端のインデックスを代入
                float startLeft = noteData.TimeToRanges[i].Range[0];
                float startRight = noteData.TimeToRanges[i].Range[^1];
                float endLeft = noteData.TimeToRanges[i + 1].Range[0];
                float endRight = noteData.TimeToRanges[i + 1].Range[^1];

                // 傾きを計算
                float slopeLeft = (endLeft - startLeft) == 0 ? float.PositiveInfinity : length / (endLeft - startLeft);
                float slopeRight = (endRight - startRight) == 0 ? float.PositiveInfinity : length / (endRight - startRight);

                // 頂点インデックスリストを作成
                List<float> indexStart = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft < 0 ? endLeft : startLeft,
                    slopeRight < float.PositiveInfinity && slopeRight > 0 ? endRight + 1 : startRight + 1, meshDivisionNum);

                List<float> indexEnd = GetMeshPointList(slopeLeft < float.PositiveInfinity && slopeLeft > 0 ? startLeft : endLeft,
                   slopeRight < float.PositiveInfinity && slopeRight < 0 ? startRight + 1 : endRight + 1, meshDivisionNum);

                //Debug.Log("indexStart: " + string.Join(",", indexStart.Select(n => n.ToString())));
                //Debug.Log("indexEnd: " + string.Join(",", indexEnd.Select(n => n.ToString())));

                // 頂点リストを生成
                List<Vector3> verticesStart = GenerateVertices(indexStart, startLeft, startRight + 1, slopeLeft, slopeRight, currentStartZ);
                List<Vector3> verticesEnd = GenerateVertices(indexEnd, endLeft, endRight + 1, slopeLeft, slopeRight, currentStartZ + length);

                //Debug.Log("verticesStart: " + string.Join(",", verticesStart.Select(n => n.ToString())));
                //Debug.Log("verticesEnd: " + string.Join(",", verticesEnd.Select(n => n.ToString())));

                // 頂点リストの代入
                vertices.AddRange(verticesStart);
                vertices.AddRange(verticesEnd);

                // トライアングルインデックスを生成、代入
                triangles.AddRange(GenerateTriangles(currentMeshIndex, verticesStart.Count, verticesEnd.Count));

                //Debug.Log("triangles: " + string.Join(",", triangles.Select(n => n.ToString())));

                currentStartZ += length;
                currentMeshIndex += verticesStart.Count + verticesEnd.Count;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
            return obj;
        }

        /// <summary>
        /// 範囲内のメッシュ頂点リストを返す
        /// </summary>
        private List<float> GetMeshPointList(float first, float end, int divNum)
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
            list.Add(end);
            return list.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>
        /// 指定したインデックスリストからメッシュの頂点座標を計算する
        /// </summary>
        private List<Vector3> GenerateVertices(List<float> indices, float left, float right, float slopeLeft, float slopeRight, float baseZ)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (float f in indices)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = baseZ;

                if (f < left) { z += slopeLeft * (f - left); }
                else if (f > right) { z += slopeRight * (f - right); }

                vertices.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
                //vertices.Add(new Vector3(f, -10, z));  // デバッグ用
            }
            return vertices;
        }

        /// <summary>
        /// メッシュのトライアングルインデックスを生成
        /// </summary>
        private List<int> GenerateTriangles(int startIndex, int countStart, int countEnd)
        {
            List<int> triangles = new List<int>();
            int halfCount = Mathf.Min(countStart, countEnd) - 1;

            for (int i = 0; i < halfCount; i++)
            {
                triangles.Add(startIndex + i + 1);
                triangles.Add(startIndex + i);
                triangles.Add(startIndex + i + countStart);

                triangles.Add(startIndex + i + 1);
                triangles.Add(startIndex + i + countStart);
                triangles.Add(startIndex + i + countStart + 1);
            }
            return triangles;
        }


        /// <summary>
        /// 位置調整など
        /// </summary>
        private void SetTransform(NoteObject<NoteData_HoldMeshSuper> note, NoteData_HoldMeshSuper data)
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
