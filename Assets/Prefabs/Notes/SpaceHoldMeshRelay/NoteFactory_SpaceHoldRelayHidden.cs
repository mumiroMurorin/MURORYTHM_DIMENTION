using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;

public class NoteFactory_SpaceHoldRelayHidden : NoteFactory<NoteData_SpaceHoldRelayHidden>
{
    readonly Vector3 CENTER_PIVOT = Vector3.zero;
    readonly float RADIUS = 10f;

    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] SpaceHoldJudgementSettings judgementSettings;
    [Header("強調線の太さ")]
    [SerializeField] float enphasisLineWidth = 0.1f;
    [Header("メインメッシュのマテリアル")]
    [SerializeField] Material mainMaterial;
    [Header("強調線のマテリアル")]
    [SerializeField] Material edgeMaterial;

    INoteSpawnDataOptionGetter optionHolder;
    ISpaceInputGetter spaceInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    Difficulty currentDifficulty = Difficulty.Normal;
    Transform noteParent;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.spaceInputGetter = initializingData.SpaceInputGetter;
        this.judgementRecorder = initializingData.JudgementRecorder;
        this.timer = initializingData.Timer;
        this.currentDifficulty = initializingData.Difficulty;
    }

    public override NoteObject<NoteData_SpaceHoldRelayHidden> Spawn(NoteData_SpaceHoldRelayHidden data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_SpaceHoldRelayHidden> note = GenerateNoteInstance(ConvertNoteData(data, positionCalculator));

        // 位置調整
        SetTransform(note, positionCalculator.GetPosition(data.Timing) * optionHolder.NoteSpeed.Value);

        // 初期化
        note.Initialize(data);

        return note;
    }

    /// <summary>
    /// ノートデータに必要な情報を追加
    /// </summary>
    /// <param name="data"></param>
    private NoteData_SpaceHoldRelayHidden ConvertNoteData(NoteData_SpaceHoldRelayHidden data, INotePositionCalculator positionCalculator)
    {
        // ノートデータに必要な情報を追加
        data.SpaceInput = this.spaceInputGetter;
        data.Timer = this.timer;
        data.JudgementRecorder = this.judgementRecorder;
        data.OptionGetter = optionHolder;
        data.PositionCalculator = positionCalculator;
        data.NoteSpeed = optionHolder.NoteSpeed.Value;
        data.DepthToVertices = GenerateDepthToVertices(data.TimeToVertices, positionCalculator, data.NoteSpeed);
        data.JudgementSettings = judgementSettings;
        if (judgementSettings != null)
        {
            data.JudgementWindow = judgementSettings.CreateJudgementWindowIfMissing(data.JudgementWindow, currentDifficulty);
        }

        return data;
    }

    private List<DepthToVertices> GenerateDepthToVertices(List<TimeToVertices> timeToVertices, INotePositionCalculator positionCalculator, float noteSpeed)
    {
        if (timeToVertices == null) { return null; }
        if (positionCalculator == null) { return null; }

        return timeToVertices
            .Select(x => new DepthToVertices(positionCalculator.GetPosition(x.Timing) * noteSpeed, x.Vertices))
            .ToList();
    }

    /// <summary>
    /// ノートをインスタンス化して返す
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private NoteObject<NoteData_SpaceHoldRelayHidden> GenerateNoteInstance(NoteData_SpaceHoldRelayHidden data)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // ノートオブジェクトの表を生成
        GameObject noteObj = GenerateMeshObject(data);
        noteObj.transform.SetParent(origin.transform);

        // 強調線を生成
        GameObject emphasisLineObj = GeneratEmphasisLineObject(data);
        emphasisLineObj.transform.SetParent(origin.transform);

        // コンポーネントを取得
        NoteObject<NoteData_SpaceHoldRelayHidden> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldRelayHidden>>();

        return note;
    }

    /// <summary>
    /// ホールドメッシュ部分を生成
    /// </summary>
    private GameObject GenerateMeshObject(NoteData_SpaceHoldRelayHidden noteData)
    {
        GameObject obj = new GameObject("Mesh");
        NoteLayerUtility.SetNotesLayer(obj);
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        var points = noteData.Vertices.Select(v => MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToList();
        Mesh mesh = MeshGenerator.GenerateMesh(points);
        meshFilter.mesh = mesh;

        if (mesh == null) { return obj; }

        meshRenderer.material = mainMaterial;

        return obj;
    }

    /// <summary>
    /// ホールド強調線を生成
    /// </summary>
    private GameObject GeneratEmphasisLineObject(NoteData_SpaceHoldRelayHidden noteData)
    {
        GameObject obj = new GameObject("EmphasisLine");
        NoteLayerUtility.SetNotesLayer(obj);
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        var points = noteData.Vertices.Select(v => MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToList();
        Mesh mesh = MeshGenerator.GenerateLineMesh(points, enphasisLineWidth, isLoop: true);
        meshFilter.mesh = mesh;
        // 成功演出のためにMeshを保存
        noteData.Mesh = mesh;

        if (mesh == null) { return obj; }

        meshRenderer.material = edgeMaterial;

        return obj;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_SpaceHoldRelayHidden> note, float spawnZ)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(spawnZ, optionHolder.NoteCurveRadius.Value);
    }
}
