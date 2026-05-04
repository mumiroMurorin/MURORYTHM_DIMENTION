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
    [SerializeField] GameObject noteMeshPrefab;

    [Header("meshの分割数")]
    [SerializeField] int meshDivisionNum = 10;

    [Header("mesh1単位の最大長さ")]
    [SerializeField] float maxTriangleLength = 0.5f;

    INoteSpawnDataOptionGetter optionHolder;
    ISpaceInputGetter spaceInputGetter;
    ITimeGetter timer;
    Transform noteParent;
    Deformer groundDeformer;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.groundDeformer = initializingData.GroundDeformer;
        this.spaceInputGetter = initializingData.SpaceInputGetter;
        this.timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_SpaceHoldMesh> Spawn(NoteData_SpaceHoldMesh data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_SpaceHoldMesh> note = GenerateNoteInstance(ConvertNoteData(data), positionCalculator);

        // 位置調整
        SetTransform(note, positionCalculator.GetPosition(data.Timing) * optionHolder.NoteSpeed.Value);

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
        data.SpaceInput = this.spaceInputGetter;
        data.Timer = this.timer;
        data.OptionGetter = this.optionHolder;

        return data;
    }

    /// <summary>
    /// ノーツをインスタンス化して返す
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private NoteObject<NoteData_SpaceHoldMesh> GenerateNoteInstance(NoteData_SpaceHoldMesh data, INotePositionCalculator positionCalculator)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // ノーツオブジェクト(表)を生成
        var outsideMesh = GenerateMeshObject(data, false, positionCalculator);
        outsideMesh.transform.SetParent(origin.transform);

        // ノーツオブジェクト(裏)を生成
        var insideMesh = GenerateMeshObject(data, true, positionCalculator);
        insideMesh.transform.SetParent(origin.transform);

        // コンポーネントを取得
        NoteObject<NoteData_SpaceHoldMesh> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldMesh>>();

        // レンダラーの登録
        data.MeshRendererAsset = new HoldMeshRendererAsset(insideMesh, outsideMesh);

        return note;
    }

    /// <summary>
    /// ホールドのメッシュ部分の生成
    /// </summary>
    private MeshRenderer GenerateMeshObject(NoteData_SpaceHoldMesh noteData, bool isMeshReverse, INotePositionCalculator positionCalculator)
    {
        var obj = Instantiate(noteMeshPrefab);
        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        // 復元
        List<TimeToVertices> timeToVertices = new List<TimeToVertices>();
        foreach (TimeToVertices t in noteData.TimeToVertices)
        {
            timeToVertices.Add(new TimeToVertices(t.Timing, t.Vertices.Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToArray()));
        }

        Mesh mesh = SpaceHoldMeshGenerator.GenerateSpaceHoldEdgeMesh(timeToVertices, positionCalculator, optionHolder.NoteSpeed.Value, meshDivisionNum, maxTriangleLength, isMeshReverse);
        meshFilter.mesh = mesh;

        if (!obj.TryGetComponent(out Deformable d)) { obj.AddComponent<Deformable>().AddDeformer(groundDeformer); }
        else { d.AddDeformer(groundDeformer); }

        return meshRenderer;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_SpaceHoldMesh> note, float spawnZ)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(spawnZ);
    }
}
