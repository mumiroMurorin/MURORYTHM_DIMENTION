using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;
using Deform;

public class NoteFactory_SpaceHoldMesh : NoteFactory<NoteData_SpaceHoldMesh>
{
    readonly Vector3 CENTER_PIVOT = Vector3.zero;
    readonly float RADIUS = 10f;

    [SerializeField] GameObject noteObjectOriginPrefab;

    [Header("meshの分割数")]
    [SerializeField] int meshDivisionNum = 10;

    [Header("mesh1単位の最大長さ")]
    [SerializeField] float maxTriangleLength = 0.5f;

    INoteSpawnDataOptionHolder optionHolder;
    ISliderInputGetter sliderInputGetter;
    ITimeGetter timer;
    GameObject groundObject;
    Deformer groundDeformer;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.groundObject = initializingData.GroundObject;
        this.groundDeformer = initializingData.GroundDeformer;
        this.sliderInputGetter = initializingData.SliderInputGetter;
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

        // 復元
        List<TimeToVertices> timeToVertices = new List<TimeToVertices>();
        foreach (TimeToVertices t in noteData.TimeToVertices)
        {
            timeToVertices.Add(new TimeToVertices(t.Timing, t.Vertices.Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToArray()));
        }

        Mesh mesh = SpaceHoldMeshGenerator.GenerateSpaceHoldEdgeMesh(timeToVertices, optionHolder.NoteSpeed.Value, meshDivisionNum, maxTriangleLength, isMeshReverse);
        meshFilter.mesh = mesh;

        obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
        return obj;
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
            optionHolder.NoteSpeed.Value * data.Timing
            );

        // 動く地面を親登録
        note.transform.SetParent(groundObject.transform);
    }
}
