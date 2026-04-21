using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;
using Deform;

public class NoteFactory_SpaceHoldRelay : NoteFactory<NoteData_SpaceHoldRelay>
{
    readonly Vector3 CENTER_PIVOT = Vector3.zero;
    readonly float RADIUS = 10f;

    [SerializeField] GameObject noteObjectOriginPrefab;
    [Header("【強調線】太さ")]
    [SerializeField] float enphasisLineWidth = 0.1f;
    [Header("メインメッシュのマテリアル")]
    [SerializeField] Material mainMaterial;
    [Header("強調線のマテリアル")]
    [SerializeField] Material edgeMaterial;

    INoteSpawnDataOptionGetter optionHolder;
    ISpaceInputGetter spaceInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    Transform noteParent;
    Deformer groundDeformer;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.groundDeformer = initializingData.GroundDeformer;
        this.spaceInputGetter = initializingData.SpaceInputGetter;
        this.judgementRecorder = initializingData.JudgementRecorder;
        this.timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_SpaceHoldRelay> Spawn(NoteData_SpaceHoldRelay data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_SpaceHoldRelay> note = GenerateNoteInstance(ConvertNoteData(data));

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
    private NoteData_SpaceHoldRelay ConvertNoteData(NoteData_SpaceHoldRelay data)
    {
        // ノーツデータにいろいろ追加
        data.SpaceInput = this.spaceInputGetter;
        data.Timer = this.timer;
        data.JudgementRecorder = this.judgementRecorder;
        data.OptionGetter = optionHolder;

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

        // 強調線の生成
        GameObject emphasisLineObj = GeneratEmphasisLineObject(data);
        emphasisLineObj.transform.SetParent(origin.transform);

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
        var points = noteData.Vertices.Select(v => MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToList();
        Mesh mesh = MeshGenerator.GenerateMesh(points);
        meshFilter.mesh = mesh;

        if (mesh == null) { return obj; }

        meshRenderer.material = mainMaterial;

        obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
        return obj;
    }

    /// <summary>
    /// ホールドの強調線の生成
    /// </summary>
    private GameObject GeneratEmphasisLineObject(NoteData_SpaceHoldRelay noteData)
    {
        GameObject obj = new GameObject("EmphasisLine");
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        var points = noteData.Vertices.Select(v => MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToList();
        Mesh mesh = MeshGenerator.GenerateLineMesh(points, enphasisLineWidth, isLoop: true);
        meshFilter.mesh = mesh;
        // 成功演出のためにMeshを保存
        noteData.Mesh = mesh;

        if (mesh == null) { return obj; }

        meshRenderer.material = edgeMaterial;

        obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
        return obj;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_SpaceHoldRelay> note, float spawnZ)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.transform.localPosition = new Vector3(
            note.transform.position.x,
            note.transform.position.y,
            spawnZ
            );
    }
}
