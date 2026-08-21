using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;

public class NoteFactory_SpaceHoldRelay : NoteFactory<NoteData_SpaceHoldRelay>
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
    [Header("地面補助線")]
    [SerializeField] bool enableGroundLine = true;
    [SerializeField] Material groundLineMaterial;
    [SerializeField] float groundLineWidth = 0.04f;

    INoteSpawnDataOptionGetter optionHolder;
    ISpaceInputGetter spaceInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    Transform noteParent;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.spaceInputGetter = initializingData.SpaceInputGetter;
        this.judgementRecorder = initializingData.JudgementRecorder;
        this.timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_SpaceHoldRelay> Spawn(NoteData_SpaceHoldRelay data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_SpaceHoldRelay> note = GenerateNoteInstance(ConvertNoteData(data, positionCalculator));

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
    private NoteData_SpaceHoldRelay ConvertNoteData(NoteData_SpaceHoldRelay data, INotePositionCalculator positionCalculator)
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
            data.JudgementWindow = judgementSettings.CreateJudgementWindowIfMissing(data.JudgementWindow);
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
    private NoteObject<NoteData_SpaceHoldRelay> GenerateNoteInstance(NoteData_SpaceHoldRelay data)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // ノートオブジェクトの表を生成
        GameObject noteObj = GenerateMeshObject(data);
        noteObj.transform.SetParent(origin.transform);

        // 強調線を生成
        GameObject emphasisLineObj = GeneratEmphasisLineObject(data);
        emphasisLineObj.transform.SetParent(origin.transform);

        if (enableGroundLine)
        {
            // SpaceHoldRelayの最下点から、湾曲したグラウンドまでの補助線を生成
            GameObject groundLineObj = GenerateGroundLineObject(data);
            groundLineObj.transform.SetParent(origin.transform);
        }

        // コンポーネントを取得
        NoteObject<NoteData_SpaceHoldRelay> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldRelay>>();

        return note;
    }

    /// <summary>
    /// ホールドメッシュ部分を生成
    /// </summary>
    private GameObject GenerateMeshObject(NoteData_SpaceHoldRelay noteData)
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
    private GameObject GeneratEmphasisLineObject(NoteData_SpaceHoldRelay noteData)
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
    /// SpaceHoldRelayから地面へ伸びる補助線を生成
    /// </summary>
    private GameObject GenerateGroundLineObject(NoteData_SpaceHoldRelay noteData)
    {
        GameObject obj = new GameObject("SpaceHoldRelayGroundLine");
        NoteLayerUtility.SetNotesLayer(obj);
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

        var points = noteData.Vertices
            .Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS))
            .ToList();

        Vector2 bottomPoint = GetBottomPoint(points);
        float lineZ = 0f;
        Vector3 start = NoteTrackCurve.BendVertex(new Vector3(bottomPoint.x, bottomPoint.y, lineZ), optionHolder.NoteCurveRadius.Value);
        Vector3 end = ProjectToHalfPipeGround(bottomPoint.x, lineZ);

        meshFilter.mesh = MeshGenerator.GenerateLineMesh(new List<Vector3> { start, end }, groundLineWidth, false);
        meshRenderer.material = groundLineMaterial != null ? groundLineMaterial : edgeMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return obj;
    }

    private Vector2 GetBottomPoint(List<Vector2> points)
    {
        if (points == null || points.Count == 0) { return Vector2.zero; }

        float minY = points.Min(p => p.y);
        float averageX = 0f;
        int count = 0;

        // 最下辺が複数点で構成される場合は、中心に近い代表点を使う
        foreach (Vector2 point in points)
        {
            if (!Mathf.Approximately(point.y, minY)) { continue; }

            averageX += point.x;
            count++;
        }

        if (count <= 0)
        {
            return points.OrderBy(p => p.y).First();
        }

        return new Vector2(averageX / count, minY);
    }

    private Vector3 ProjectToHalfPipeGround(float x, float z)
    {
        const float minRadius = 0.01f;
        float halfPipeRadius = Mathf.Max(RADIUS, minRadius);
        float clampedX = Mathf.Clamp(x, -halfPipeRadius, halfPipeRadius);
        float groundY = -Mathf.Sqrt(halfPipeRadius * halfPipeRadius - clampedX * clampedX);

        return NoteTrackCurve.BendVertex(new Vector3(clampedX, groundY, z), optionHolder.NoteCurveRadius.Value);
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_SpaceHoldRelay> note, float spawnZ)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(spawnZ, optionHolder.NoteCurveRadius.Value);
    }
}
