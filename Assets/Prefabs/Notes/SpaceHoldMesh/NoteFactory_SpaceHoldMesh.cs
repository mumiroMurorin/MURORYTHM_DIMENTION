using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;

public class NoteFactory_SpaceHoldMesh : NoteFactory<NoteData_SpaceHoldMesh>
{
    readonly Vector3 CENTER_PIVOT = Vector3.zero;
    readonly float RADIUS = 10f;

    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] GameObject noteMeshPrefab;
    [SerializeField] GameObject shadowMeshPrefab;
    [SerializeField] Material shadowMaterial;

    [Header("meshの分割数")]
    [SerializeField] int meshDivisionNum = 10;
    [SerializeField] int shadowDivisionNum = 24;

    [Header("mesh1単位の最大長さ")]
    [SerializeField] float maxTriangleLength = 0.5f;

    [Header("アウトライン生成の隙間")]
    [SerializeField] float outlineGap = 0.05f;
    [SerializeField] float shadowRadiusOffset = 0.02f;

    INoteSpawnDataOptionGetter optionHolder;
    ISpaceInputGetter spaceInputGetter;
    ITimeGetter timer;
    Transform noteParent;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.spaceInputGetter = initializingData.SpaceInputGetter;
        this.timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_SpaceHoldMesh> Spawn(NoteData_SpaceHoldMesh data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_SpaceHoldMesh> note = GenerateNoteInstance(ConvertNoteData(data, positionCalculator), positionCalculator);

        // 位置調整
        float startDistance = positionCalculator.GetPosition(data.Timing) * optionHolder.NoteSpeed.Value;
        float endTiming = data.TimeToVertices != null && data.TimeToVertices.Count > 0
            ? data.TimeToVertices.Max(x => x.Timing)
            : data.Timing;
        float endDistance = positionCalculator.GetPosition(endTiming) * optionHolder.NoteSpeed.Value;
        SetTransform(note, startDistance, endDistance);

        // 初期化
        note.Initialize(data);

        return note;
    }

    /// <summary>
    /// ノートデータに必要な情報を追加
    /// </summary>
    private NoteData_SpaceHoldMesh ConvertNoteData(NoteData_SpaceHoldMesh data, INotePositionCalculator positionCalculator)
    {
        data.SpaceInput = this.spaceInputGetter;
        data.Timer = this.timer;
        data.OptionGetter = this.optionHolder;
        data.PositionCalculator = positionCalculator;
        data.NoteSpeed = optionHolder.NoteSpeed.Value;
        data.DepthToVertices = GenerateDepthToVertices(data.TimeToVertices, positionCalculator, data.NoteSpeed);
        data.JudgementRangeLineParent = this.transform;

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
    /// ノーツをインスタンス化して返す
    /// </summary>
    private NoteObject<NoteData_SpaceHoldMesh> GenerateNoteInstance(NoteData_SpaceHoldMesh data, INotePositionCalculator positionCalculator)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // 外側位置に、アウトラインメッシュを生成
        MeshRenderer outlineMesh = GenerateMeshObject(data, outlineGap, false, positionCalculator);
        outlineMesh.transform.SetParent(origin.transform);

        // 内側位置に、表向きと裏向きのメッシュを生成
        MeshRenderer insideForwardMesh = GenerateMeshObject(data, 0f, false, positionCalculator);
        insideForwardMesh.transform.SetParent(origin.transform);

        MeshRenderer insideReverseMesh = GenerateMeshObject(data, 0f, true, positionCalculator);
        insideReverseMesh.transform.SetParent(origin.transform);

        MeshRenderer shadowMesh = GenerateShadowMeshObject(data, positionCalculator);
        shadowMesh.transform.SetParent(origin.transform);

        NoteObject<NoteData_SpaceHoldMesh> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldMesh>>();

        // 既存のSetMaterial呼び出しを維持するため、Renderer管理だけ4枚対応にする
        data.MeshRendererAsset = new HoldMeshRendererAsset(
            insideForwardMesh,
            insideReverseMesh,
            outlineMesh,
            shadowMesh);

        return note;
    }

    /// <summary>
    /// ホールドメッシュ部分を生成
    /// </summary>
    private MeshRenderer GenerateMeshObject(NoteData_SpaceHoldMesh noteData, float surfaceOffset, bool isMeshReverse, INotePositionCalculator positionCalculator)
    {
        var obj = Instantiate(noteMeshPrefab);
        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        // 判定用データは触らず、見た目用コピーだけを内外方向へずらす
        List<TimeToVertices> timeToVertices = GenerateVisualTimeToVertices(noteData.TimeToVertices, surfaceOffset);

        Mesh mesh = SpaceHoldMeshGenerator.GenerateSpaceHoldEdgeMesh(
            timeToVertices,
            positionCalculator,
            optionHolder.NoteSpeed.Value,
            meshDivisionNum,
            maxTriangleLength,
            isMeshReverse,
            optionHolder.NoteCurveRadius.Value);
        meshFilter.mesh = mesh;

        return meshRenderer;
    }

    private MeshRenderer GenerateShadowMeshObject(NoteData_SpaceHoldMesh noteData, INotePositionCalculator positionCalculator)
    {
        var obj = Instantiate(shadowMeshPrefab);
        obj.name = "SpaceHoldShadow";

        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        List<TimeToVertices> timeToVertices = GenerateVisualTimeToVertices(noteData.TimeToVertices, 0f);
        Mesh mesh = SpaceHoldShadowMeshGenerator.GenerateSpaceHoldShadowMesh(
            timeToVertices,
            positionCalculator,
            optionHolder.NoteSpeed.Value,
            shadowDivisionNum,
            maxTriangleLength,
            optionHolder.NoteCurveRadius.Value,
            RADIUS,
            shadowRadiusOffset);

        meshFilter.mesh = mesh;
        if (shadowMaterial != null)
        {
            meshRenderer.material = shadowMaterial;
        }

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return meshRenderer;
    }

    private List<TimeToVertices> GenerateVisualTimeToVertices(List<TimeToVertices> source, float surfaceOffset)
    {
        List<TimeToVertices> result = new List<TimeToVertices>();
        if (source == null) { return result; }

        foreach (TimeToVertices t in source)
        {
            Vector2[] offsetVertices = OffsetVerticesFromCenter(t.Vertices, surfaceOffset);
            Vector2[] normalizedVertices = offsetVertices
                .Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS))
                .ToArray();

            result.Add(new TimeToVertices(t.Timing, normalizedVertices));
        }

        return result;
    }

    private Vector2[] OffsetVerticesFromCenter(Vector2[] vertices, float offset)
    {
        if (vertices == null) { return new Vector2[0]; }
        if (vertices.Length == 0) { return new Vector2[0]; }
        if (Mathf.Approximately(offset, 0f)) { return vertices.ToArray(); }

        Vector2 center = CalcCenter(vertices);
        Vector2[] result = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 direction = vertices[i] - center;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                result[i] = vertices[i];
                continue;
            }

            result[i] = vertices[i] + direction.normalized * offset;
        }

        return result;
    }

    private Vector2 CalcCenter(Vector2[] vertices)
    {
        Vector2 center = Vector2.zero;
        for (int i = 0; i < vertices.Length; i++)
        {
            center += vertices[i];
        }

        return center / vertices.Length;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_SpaceHoldMesh> note, float startDistance, float endDistance)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(startDistance, endDistance, optionHolder.NoteCurveRadius.Value);
    }
}
