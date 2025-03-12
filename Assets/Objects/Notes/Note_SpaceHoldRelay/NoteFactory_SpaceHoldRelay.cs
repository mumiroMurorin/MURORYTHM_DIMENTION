using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LibTessDotNet;
using Deform;

namespace Refactoring
{
    public class NoteFactory_SpaceHoldRelay : NoteFactory<NoteData_SpaceHoldRelay>
    {
        readonly Vector3 CENTER_PIVOT = Vector3.zero;
        readonly float RADIUS = 10f; 

        [SerializeField] GameObject noteObjectOriginPrefab;
        [Header("【強調線】太さ")]
        [SerializeField] float enphasisLineWidth = 0.1f;

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

        public override NoteObject<NoteData_SpaceHoldRelay> Spawn(NoteData_SpaceHoldRelay data)
        {
            // 生成
            NoteObject<NoteData_SpaceHoldRelay> note = GenerateNoteInstance(ConvertNoteData(data));

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
        private NoteData_SpaceHoldRelay ConvertNoteData(NoteData_SpaceHoldRelay data)
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
        private NoteObject<NoteData_SpaceHoldRelay> GenerateNoteInstance(NoteData_SpaceHoldRelay data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクト(表)を生成
            GameObject noteObj = GenerateMeshObject(data);
            noteObj.transform.SetParent(origin.transform);
            //GameObject emphasisLineObj = GenerateEmphasisLine(data);
            //emphasisLineObj.transform.SetParent(origin.transform);

            // コンポーネントを取得
            NoteObject<NoteData_SpaceHoldRelay> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldRelay>>();

            return note;
        }

        /// <summary>
        /// ホールドのメッシュ部分の生成
        /// </summary>
        private GameObject GenerateMeshObject(NoteData_SpaceHoldRelay noteData)
        {
            GameObject obj = new GameObject("Mesh");
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            Mesh mesh = GenerateMesh(noteData.Vertices.ToList());
            meshFilter.mesh = mesh;

            if(mesh == null) { return obj; }

            List<Vector2> uvs = new List<Vector2>();

            //mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
            return obj;
        }

        /// <summary>
        /// メッシュ(自己交差なし)の生成
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private Mesh GenerateMesh(List<Vector2> vertices)
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
                    Position = new Vec3((vertices[i].x - CENTER_PIVOT.x) * RADIUS, (vertices[i].y - CENTER_PIVOT.y) * RADIUS, 0)
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
        /// 強調線の生成
        /// </summary>
        /// <param name="noteData"></param>
        /// <returns></returns>
        private GameObject GenerateEmphasisLine(NoteData_SpaceHoldRelay noteData)
        {
            GameObject lineObj = new GameObject("EnphasisLine");
            var lineRenderer = lineObj.AddComponent<LineRenderer>();

            // 各種設定
            lineRenderer.loop = true;
            lineRenderer.startWidth = lineRenderer.endWidth = enphasisLineWidth;
            lineRenderer.useWorldSpace = false;

            // 線を引く
            var positions = noteData.Vertices.Select(v => new Vector3((v.x - CENTER_PIVOT.x) * RADIUS, (v.y - CENTER_PIVOT.y) * RADIUS, 0f)).ToArray();
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);

            lineObj.AddComponent<Deformable>().AddDeformer(groundDeformer);

            return lineObj;
        }

        /// <summary>
        /// 位置調整など
        /// </summary>
        private void SetTransform(NoteObject<NoteData_SpaceHoldRelay> note, NoteData_SpaceHoldRelay data)
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
