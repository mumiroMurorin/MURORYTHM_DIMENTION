using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Deform;

namespace Refactoring
{
    public class NoteFactory_HoldMesh : NoteFactory<NoteData_HoldMesh>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("meshの分割数")]
        [SerializeField] int meshDivisionNum = 10;
        [Header("meshのマテリアル(未判定時)")]
        [SerializeField] Material meshMaterialDefault;
        [Header("meshのマテリアル(タッチ時)")]
        [SerializeField] Material meshMaterialTouched;
        [Header("meshのマテリアル(非タッチ時)")]
        [SerializeField] Material meshMaterialNoneTouched;

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

        public override NoteObject<NoteData_HoldMesh> Spawn(NoteData_HoldMesh data)
        {
            // 生成
            NoteObject<NoteData_HoldMesh> note = GenerateNoteInstance(ConvertNoteData(data));

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
        private NoteData_HoldMesh ConvertNoteData(NoteData_HoldMesh data)
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
        private NoteObject<NoteData_HoldMesh> GenerateNoteInstance(NoteData_HoldMesh data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクトを生成
            GameObject noteObj = GenerateMeshObject(data);

            // originにくっつける
            noteObj.transform.SetParent(origin.transform);

            // コンポーネントを取得
            NoteObject<NoteData_HoldMesh> note = origin.GetComponent<NoteObject<NoteData_HoldMesh>>();

            return note;
        }

        /// <summary>
        /// ホールドのメッシュ部分の生成
        /// </summary>
        private GameObject GenerateMeshObject(NoteData_HoldMesh noteData)
        {
            GameObject obj = new GameObject("Mesh");
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;

            // Holdの長さ
            float length = optionHolder.NoteSpeed * (noteData.EndTiming - noteData.Timing);

            // ノート端のそれぞれのindex
            int startLeft = noteData.StartRange[0];
            int startRight = noteData.StartRange[noteData.StartRange.Length - 1];
            int endLeft = noteData.EndRange[0];
            int endRight = noteData.EndRange[noteData.EndRange.Length - 1];

            // 横2辺の傾きを導出
            float slopeLeft = length / (endLeft - startLeft);
            float slopeRight = length / (endRight - startRight);

            // ノート端～端までに入る分割インデックス
            List<float> index_f = new List<float>();
            List<float> index_l = new List<float>();

            // ----- 分割インデックスリストに各メッシュ頂点を追加 -----
            // --- StartRangeに追加 ---
            float firstIndex = startLeft;
            float endIndex = startRight + 1;

            // 左辺の傾きが負の場合インデックスを変更 「＼」
            if (slopeLeft < float.PositiveInfinity && slopeLeft < 0) 
            {
                index_f.Add(startLeft);
                index_l.Add(startLeft);
                firstIndex = endLeft;
            }

            // 右辺の傾きが正の場合インデックスを変更 「／」
            if (slopeRight < float.PositiveInfinity && slopeRight > 0) 
            {
                index_f.Add(endRight + 1);
                index_l.Add(startRight + 1);
                endIndex = endRight + 1;
            }

            // メッシュ生成の各点を導出、代入
            index_f.AddRange(GetMeshPointList(firstIndex, endIndex, meshDivisionNum));

            // --- EndRangeに追加 ---
            firstIndex = endLeft;
            endIndex = endRight + 1;

            // 左辺の傾きが正の場合インデックスを変更 「／」
            if (slopeLeft < float.PositiveInfinity && slopeLeft > 0)
            {
                index_l.Add(endLeft);
                index_f.Add(endLeft);
                firstIndex = startLeft;
            }

            // 右辺の傾きが負の場合インデックスを変更 「＼」
            if (slopeRight < float.PositiveInfinity && slopeRight < 0) 
            {
                index_l.Add(endRight + 1);
                index_f.Add(endRight + 1);
                endIndex = startRight + 1;
            }

            // メッシュ生成の各点を導出、代入
            index_l.AddRange(GetMeshPointList(firstIndex, endIndex, meshDivisionNum)); //リストに代入

            index_f.Sort();                         //ソート
            index_l.Sort();                         //ソート
            index_f = index_f.Distinct().ToList();  //重複要素削除
            index_l = index_l.Distinct().ToList();  //重複要素削除
            if (slopeLeft < float.PositiveInfinity && slopeLeft != 0) { index_l.Remove(firstIndex); }  //傾きが0でない場合、最初の値を削除
            if (slopeRight < float.PositiveInfinity && slopeRight != 0) { index_f.Remove(endIndex); }  //傾きが0でない場合、最後の値を削除

            // ----- 点の座標を導出 -----
            List<Vector3> vertices_f = new List<Vector3>();   //前ノート頂点座標リスト
            List<Vector3> vertices_l = new List<Vector3>();   //後ノート頂点座標リスト

            // Indexから座標を導出(Start)
            foreach (float f in index_f)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = 0;

                // 斜め左辺の場合 「＼」
                if (f < startLeft) { z = slopeLeft * (f - endLeft) + length; }
                // 斜め右辺の場合 「／」
                else if (f > startRight + 1){ z = slopeRight * (f - startRight - 1); }

                vertices_f.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
            }

            // Indexから座標を導出(End)
            foreach (float f in index_l)
            {
                float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
                float z = length;

                // 斜め左辺の場合 「／」
                if (f < endLeft) { z = slopeLeft * (f - startLeft); }
                // 斜め右辺の場合 「＼」
                else if (f > endRight + 1) { z = slopeRight * (f - endRight - 1) + length; }
                vertices_l.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
            }

            // メッシュ生成番号の割り振り
            List<int> triangles = new List<int>();         // メッシュ生成番号
            List<Vector3> vertices = new List<Vector3>();  // メッシュ生成(点)座標

            // 結合
            vertices.AddRange(vertices_f);
            vertices.AddRange(vertices_l);

            int num = 0;
            int harfCount = vertices.Count % 2 != 0 && slopeLeft >= float.PositiveInfinity ?
                (vertices.Count - 1) / 2 - 1 : (vertices.Count - 1) / 2;

            // メッシュtriangleを計算
            while (num + 1 <= harfCount)
            {
                triangles.Add(num + 1);
                triangles.Add(num);
                triangles.Add(num + 1 + harfCount);

                if (num + 2 + harfCount >= vertices.Count) { break; }
                triangles.Add(num + 1 + harfCount);
                triangles.Add(num + 2 + harfCount);
                triangles.Add(++num);
            }

            // 右辺の傾きのみ0(無限)のとき、最後のメッシュを追加
            if (slopeLeft >= float.PositiveInfinity && slopeRight < float.PositiveInfinity)
            {
                triangles.Add(num + 1 + harfCount);
                triangles.Add(num + 2 + harfCount);
                triangles.Add(num);
            }

            // メッシュの点と面を設定して再計算
            mesh.vertices = vertices.ToArray(); //代入
            mesh.triangles = triangles.ToArray(); //代入
            mesh.RecalculateNormals();

            //Debug.Log("頂点list: " + string.Join(",", vertices_f.Select(n => n.ToString())));
            //Debug.Log("メッシュlist: " + string.Join(",", triangles.Select(n => n.ToString())));

            // マテリアルの設定
            meshRenderer.material = meshMaterialDefault;

            // Deformの設定
            Deformable d = obj.AddComponent<Deformable>();
            d.AddDeformer(groundDeformer);

            return obj;
        }

        /// <summary>
        /// 範囲内のメッシュ頂点リストを返す(endに+1することを忘れないように)
        /// </summary>
        /// <param name="first"></param>
        /// <param name="end"></param>
        /// <param name="div_num"></param>
        /// <returns></returns>
        private List<float> GetMeshPointList(float first, float end, int div_num)
        {
            if (div_num == 0)
            {
                Debug.LogError("【Note】メッシュ分割数が0です");
                return null;
            }

            List<float> list = new List<float>();
            float f = 0;
            while (f < end)
            {
                if (first < f) { list.Add(f); }
                f += 1f / div_num;
            }
            list.Add(first);
            list.Add(end);
            list.Sort();
            return list;
        }

        /// <summary>
        /// 位置調整など
        /// </summary>
        private void SetTransform(NoteObject<NoteData_HoldMesh> note, NoteData_HoldMesh data)
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

    public static class MeshGenerator
    {
        /// <summary>
        /// メッシュを生成して返す
        /// </summary>
        /// <returns></returns>
        public static Mesh GenerateMesh(List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh();

            mesh.vertices = vertices.ToArray(); //代入
            mesh.triangles = triangles.ToArray(); //代入
            mesh.RecalculateNormals();

            return mesh;
        }
    }

}
