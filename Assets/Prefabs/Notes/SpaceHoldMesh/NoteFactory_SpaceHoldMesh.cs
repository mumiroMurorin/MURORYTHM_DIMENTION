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
    [SerializeField] bool enableOutline = true;
    [SerializeField] bool enableJudgementRangeLine = true;
    [SerializeField] bool enableScreenSpaceOutlineTarget = true;
    [SerializeField] bool suppressDuplicateScreenSpaceOutlineBoundaries = true;
    [SerializeField] string screenSpaceOutlineLayerName = "SpaceHoldScreenOutline";
    [SerializeField] int minStencilId = 1;
    [SerializeField] int maxStencilId = 255;
    [SerializeField] float outlineGap = 0.05f;
    [SerializeField] float screenSpaceOutlineBoundaryQuantizeUnit = 0.001f;
    [SerializeField] float shadowRadiusOffset = 0.02f;
    [SerializeField] bool normalizeVerticesWinding = true;
    [SerializeField] bool useClockwiseWinding = true;

    int currentStencilId;
    readonly Dictionary<string, List<ScreenSpaceOutlineBoundarySegment>> registeredScreenSpaceOutlineBoundaries
        = new Dictionary<string, List<ScreenSpaceOutlineBoundarySegment>>();

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
        registeredScreenSpaceOutlineBoundaries.Clear();
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
        LongNoteMeshVisibility.Attach(
            note,
            startDistance,
            optionHolder.NoteCurveRadius.Value,
            timer,
            positionCalculator,
            optionHolder.NoteSpeed.Value);

        // 初期化
        note.Initialize(data);

        return note;
    }

    /// <summary>
    /// ノートデータに必要な情報を追加
    /// </summary>
    private NoteData_SpaceHoldMesh ConvertNoteData(NoteData_SpaceHoldMesh data, INotePositionCalculator positionCalculator)
    {
        data.TimeToVertices = NormalizeTimeToVerticesWinding(data.TimeToVertices);
        data.SpaceInput = this.spaceInputGetter;
        data.Timer = this.timer;
        data.OptionGetter = this.optionHolder;
        data.PositionCalculator = positionCalculator;
        data.NoteSpeed = optionHolder.NoteSpeed.Value;
        data.DepthToVertices = GenerateDepthToVertices(data.TimeToVertices, positionCalculator, data.NoteSpeed);
        data.JudgementRangeLineParent = this.transform;
        data.EnableJudgementRangeLine = enableJudgementRangeLine;
        data.StencilId = GetNextStencilId();

        return data;
    }

    private List<TimeToVertices> NormalizeTimeToVerticesWinding(List<TimeToVertices> source)
    {
        if (source == null) { return null; }
        if (!normalizeVerticesWinding) { return source; }

        return source
            .Select(x => new TimeToVertices(x.Timing, NormalizeWinding(x.Vertices)))
            .ToList();
    }

    private Vector2[] NormalizeWinding(Vector2[] vertices)
    {
        if (vertices == null) { return null; }
        if (vertices.Length < 3) { return vertices.ToArray(); }

        bool isClockwise = CalcSignedArea(vertices) < 0f;
        if (isClockwise == useClockwiseWinding)
        {
            return vertices.ToArray();
        }

        Vector2[] result = vertices.ToArray();
        System.Array.Reverse(result);
        return result;
    }

    private float CalcSignedArea(Vector2[] vertices)
    {
        float signedArea = 0f;

        // 頂点順が揺れると法線が反転するため、断面ポリゴンの符号付き面積で向きを判定する
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 current = vertices[i];
            Vector2 next = vertices[(i + 1) % vertices.Length];
            signedArea += current.x * next.y - next.x * current.y;
        }

        return signedArea * 0.5f;
    }

    private int GetNextStencilId()
    {
        int min = Mathf.Clamp(minStencilId, 0, 255);
        int max = Mathf.Clamp(maxStencilId, min, 255);
        int id = Mathf.Clamp(currentStencilId, min, max);

        currentStencilId = id + 1;
        if (currentStencilId > max)
        {
            currentStencilId = min;
        }

        return id;
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

        MeshRenderer insideOutlineMesh = null;
        MeshRenderer outsideOutlineMesh = null;

        if (enableOutline)
        {
            // アウトライン機能が有効な時だけ、外側に少し広げたメッシュを生成
            insideOutlineMesh = GenerateMeshObject(data, outlineGap, true, positionCalculator);
            insideOutlineMesh.transform.SetParent(origin.transform);

            outsideOutlineMesh = GenerateMeshObject(data, outlineGap, false, positionCalculator);
            outsideOutlineMesh.transform.SetParent(origin.transform);
        }

        // 内側位置に、表向きと裏向きのメッシュを生成
        MeshRenderer mainInsideMesh = GenerateMeshObject(data, 0f, true, positionCalculator);
        mainInsideMesh.transform.SetParent(origin.transform);

        MeshRenderer mainOutsideMesh = GenerateMeshObject(data, 0f, false, positionCalculator);
        mainOutsideMesh.transform.SetParent(origin.transform);

        // 影メッシュを生成
        MeshRenderer shadowMesh = GenerateShadowMeshObject(data, positionCalculator);
        shadowMesh.transform.SetParent(origin.transform);

        MeshRenderer screenOutlineInsideMask = null;
        MeshRenderer screenOutlineOutsideMask = null;
        if (enableScreenSpaceOutlineTarget)
        {
            List<ScreenSpaceOutlineBoundarySegment> screenOutlineBoundarySegments = GetScreenSpaceOutlineBoundarySegments(data, positionCalculator);

            screenOutlineInsideMask = GenerateScreenSpaceOutlineMaskObject(data, true, positionCalculator, screenOutlineBoundarySegments);
            screenOutlineInsideMask.transform.SetParent(origin.transform);

            screenOutlineOutsideMask = GenerateScreenSpaceOutlineMaskObject(data, false, positionCalculator, screenOutlineBoundarySegments);
            screenOutlineOutsideMask.transform.SetParent(origin.transform);
        }

        NoteObject<NoteData_SpaceHoldMesh> note = origin.GetComponent<NoteObject<NoteData_SpaceHoldMesh>>();

        // 既存のSetMaterial呼び出しを維持するため、Renderer管理だけ4枚対応にする
        data.MeshRendererAsset = new HoldMeshRendererAsset(
            mainInsideMesh,
            mainOutsideMesh,
            insideOutlineMesh,
            outsideOutlineMesh,
            shadowMesh,
            screenOutlineInsideMask,
            screenOutlineOutsideMask);

        if (enableScreenSpaceOutlineTarget)
        {
            int outlineLayer = LayerMask.NameToLayer(screenSpaceOutlineLayerName);
            data.MeshRendererAsset.SetScreenSpaceOutlineTarget(EncodeScreenOutlineIdColor(data.StencilId), outlineLayer);
        }

        return note;
    }

    private Color EncodeScreenOutlineIdColor(int id)
    {
        int encodedId = Mathf.Clamp(id, 0, 16777214) + 1;
        float r = (encodedId & 0xFF) / 255f;
        float g = ((encodedId >> 8) & 0xFF) / 255f;
        float b = ((encodedId >> 16) & 0xFF) / 255f;

        return new Color(r, g, b, 1f);
    }

    /// <summary>
    /// ホールドメッシュ部分を生成
    /// </summary>
    private MeshRenderer GenerateMeshObject(NoteData_SpaceHoldMesh noteData, float surfaceOffset, bool isMeshReverse, INotePositionCalculator positionCalculator)
    {
        var obj = Instantiate(noteMeshPrefab);
        NoteLayerUtility.SetNotesLayerRecursively(obj);
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

    private MeshRenderer GenerateScreenSpaceOutlineMaskObject(
        NoteData_SpaceHoldMesh noteData,
        bool isMeshReverse,
        INotePositionCalculator positionCalculator,
        List<ScreenSpaceOutlineBoundarySegment> boundarySegments)
    {
        MeshRenderer meshRenderer;
        if (suppressDuplicateScreenSpaceOutlineBoundaries)
        {
            meshRenderer = GenerateScreenSpaceOutlineBoundaryMaskObject(boundarySegments, isMeshReverse);
        }
        else
        {
            meshRenderer = GenerateMeshObject(noteData, 0f, isMeshReverse, positionCalculator);
        }

        meshRenderer.name = isMeshReverse ? "ScreenOutlineInsideMask" : "ScreenOutlineOutsideMask";
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return meshRenderer;
    }

    private MeshRenderer GenerateScreenSpaceOutlineBoundaryMaskObject(
        List<ScreenSpaceOutlineBoundarySegment> boundarySegments,
        bool isMeshReverse)
    {
        var obj = Instantiate(noteMeshPrefab);
        NoteLayerUtility.SetNotesLayerRecursively(obj);
        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        meshFilter.mesh = GenerateScreenSpaceOutlineBoundaryMaskMesh(boundarySegments, isMeshReverse);
        return meshRenderer;
    }

    private List<ScreenSpaceOutlineBoundarySegment> GetScreenSpaceOutlineBoundarySegments(
        NoteData_SpaceHoldMesh noteData,
        INotePositionCalculator positionCalculator)
    {
        List<ScreenSpaceOutlineBoundarySegment> result = new List<ScreenSpaceOutlineBoundarySegment>();
        if (!suppressDuplicateScreenSpaceOutlineBoundaries)
        {
            return result;
        }

        List<TimeToVertices> timeToVertices = GenerateVisualTimeToVertices(noteData.TimeToVertices, 0f);
        if (timeToVertices == null || timeToVertices.Count < 2) { return result; }
        if (positionCalculator == null) { return result; }

        float noteSpeed = optionHolder.NoteSpeed.Value;
        float baseDepth = positionCalculator.GetPosition(timeToVertices[0].Timing) * noteSpeed;

        for (int timeIndex = 0; timeIndex < timeToVertices.Count - 1; timeIndex++)
        {
            TimeToVertices start = timeToVertices[timeIndex];
            TimeToVertices end = timeToVertices[timeIndex + 1];
            if (start.Vertices == null || end.Vertices == null) { continue; }

            int vertexCount = Mathf.Min(start.Vertices.Length, end.Vertices.Length);
            if (vertexCount < 2) { continue; }

            float startDepth = positionCalculator.GetPosition(start.Timing) * noteSpeed;
            float endDepth = positionCalculator.GetPosition(end.Timing) * noteSpeed;

            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                int nextVertexIndex = (vertexIndex + 1) % vertexCount;
                ScreenSpaceOutlineBoundarySegment segment = new ScreenSpaceOutlineBoundarySegment(
                    start.Vertices[vertexIndex],
                    start.Vertices[nextVertexIndex],
                    end.Vertices[vertexIndex],
                    end.Vertices[nextVertexIndex],
                    startDepth,
                    endDepth,
                    baseDepth);

                AddUncoveredBoundarySegments(segment, result);
            }
        }

        return result;
    }

    private Mesh GenerateScreenSpaceOutlineBoundaryMaskMesh(
        List<ScreenSpaceOutlineBoundarySegment> boundarySegments,
        bool isMeshReverse)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        if (boundarySegments == null || boundarySegments.Count == 0)
        {
            return mesh;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> trackUVs = new List<Vector2>();

        float curveRadius = optionHolder.NoteCurveRadius.Value;
        float safeMaxTriangleLength = Mathf.Max(maxTriangleLength, 0.001f);

        foreach (ScreenSpaceOutlineBoundarySegment segment in boundarySegments)
        {
            int stepCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(segment.EndDepth - segment.StartDepth) / safeMaxTriangleLength));

            for (int step = 0; step < stepCount; step++)
            {
                float t0 = step / (float)stepCount;
                float t1 = (step + 1) / (float)stepCount;
                int startIndex = vertices.Count;

                Vector3 a0 = CreateBoundaryVertex(segment, t0);
                Vector3 b0 = CreateBoundaryVertexOpposite(segment, t0);
                Vector3 a1 = CreateBoundaryVertex(segment, t1);
                Vector3 b1 = CreateBoundaryVertexOpposite(segment, t1);

                vertices.Add(a0);
                vertices.Add(b0);
                vertices.Add(a1);
                vertices.Add(b1);

                float uv0 = step / (float)stepCount;
                float uv1 = (step + 1) / (float)stepCount;
                uvs.Add(new Vector2(0f, uv0));
                uvs.Add(new Vector2(1f, uv0));
                uvs.Add(new Vector2(0f, uv1));
                uvs.Add(new Vector2(1f, uv1));
                trackUVs.Add(new Vector2(a0.z, 0f));
                trackUVs.Add(new Vector2(b0.z, 0f));
                trackUVs.Add(new Vector2(a1.z, 0f));
                trackUVs.Add(new Vector2(b1.z, 0f));

                if (isMeshReverse)
                {
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 0);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 3);
                }
                else
                {
                    triangles.Add(startIndex + 0);
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 3);
                    triangles.Add(startIndex + 2);
                }
            }
        }

        NoteTrackCurve.BendVertices(vertices, curveRadius);

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, trackUVs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Vector3 CreateBoundaryVertex(ScreenSpaceOutlineBoundarySegment segment, float t)
    {
        Vector2 point = Vector2.Lerp(segment.StartA, segment.EndA, t);
        float depth = Mathf.Lerp(segment.StartDepth, segment.EndDepth, t);
        return new Vector3(point.x, point.y, depth - segment.BaseDepth);
    }

    private Vector3 CreateBoundaryVertexOpposite(ScreenSpaceOutlineBoundarySegment segment, float t)
    {
        Vector2 point = Vector2.Lerp(segment.StartB, segment.EndB, t);
        float depth = Mathf.Lerp(segment.StartDepth, segment.EndDepth, t);
        return new Vector3(point.x, point.y, depth - segment.BaseDepth);
    }

    private void AddUncoveredBoundarySegments(
        ScreenSpaceOutlineBoundarySegment segment,
        List<ScreenSpaceOutlineBoundarySegment> destination)
    {
        string key = CreateBoundaryTrajectoryKey(segment);
        if (!registeredScreenSpaceOutlineBoundaries.TryGetValue(key, out List<ScreenSpaceOutlineBoundarySegment> registeredSegments))
        {
            registeredSegments = new List<ScreenSpaceOutlineBoundarySegment>();
            registeredScreenSpaceOutlineBoundaries.Add(key, registeredSegments);
        }

        List<NormalizedInterval> uncoveredIntervals = new List<NormalizedInterval>
        {
            new NormalizedInterval(0f, 1f)
        };

        foreach (ScreenSpaceOutlineBoundarySegment registeredSegment in registeredSegments)
        {
            if (!TryGetCoincidentInterval(segment, registeredSegment, out NormalizedInterval coveredInterval)) { continue; }

            SubtractInterval(uncoveredIntervals, coveredInterval);
            if (uncoveredIntervals.Count == 0) { break; }
        }

        foreach (NormalizedInterval interval in uncoveredIntervals)
        {
            destination.Add(SliceBoundarySegment(segment, interval.Start, interval.End));
        }

        registeredSegments.Add(segment);
    }

    private bool TryGetCoincidentInterval(
        ScreenSpaceOutlineBoundarySegment current,
        ScreenSpaceOutlineBoundarySegment registered,
        out NormalizedInterval currentInterval)
    {
        currentInterval = default;

        float tolerance = Mathf.Max(screenSpaceOutlineBoundaryQuantizeUnit, 0.000001f);
        float currentMinDepth = Mathf.Min(current.StartDepth, current.EndDepth);
        float currentMaxDepth = Mathf.Max(current.StartDepth, current.EndDepth);
        float registeredMinDepth = Mathf.Min(registered.StartDepth, registered.EndDepth);
        float registeredMaxDepth = Mathf.Max(registered.StartDepth, registered.EndDepth);
        float overlapMinDepth = Mathf.Max(currentMinDepth, registeredMinDepth);
        float overlapMaxDepth = Mathf.Min(currentMaxDepth, registeredMaxDepth);

        if (overlapMaxDepth - overlapMinDepth <= tolerance) { return false; }

        GetBoundaryPointsAtDepth(current, overlapMinDepth, out Vector2 currentMinA, out Vector2 currentMinB);
        GetBoundaryPointsAtDepth(current, overlapMaxDepth, out Vector2 currentMaxA, out Vector2 currentMaxB);
        GetBoundaryPointsAtDepth(registered, overlapMinDepth, out Vector2 registeredMinA, out Vector2 registeredMinB);
        GetBoundaryPointsAtDepth(registered, overlapMaxDepth, out Vector2 registeredMaxA, out Vector2 registeredMaxB);

        float toleranceSquared = tolerance * tolerance;
        bool sameDirection =
            ArePointsNear(currentMinA, registeredMinA, toleranceSquared) &&
            ArePointsNear(currentMinB, registeredMinB, toleranceSquared) &&
            ArePointsNear(currentMaxA, registeredMaxA, toleranceSquared) &&
            ArePointsNear(currentMaxB, registeredMaxB, toleranceSquared);
        bool oppositeDirection =
            ArePointsNear(currentMinA, registeredMinB, toleranceSquared) &&
            ArePointsNear(currentMinB, registeredMinA, toleranceSquared) &&
            ArePointsNear(currentMaxA, registeredMaxB, toleranceSquared) &&
            ArePointsNear(currentMaxB, registeredMaxA, toleranceSquared);

        if (!sameDirection && !oppositeDirection) { return false; }

        float startT = GetBoundaryParameterAtDepth(current, overlapMinDepth);
        float endT = GetBoundaryParameterAtDepth(current, overlapMaxDepth);
        currentInterval = new NormalizedInterval(Mathf.Min(startT, endT), Mathf.Max(startT, endT));
        return true;
    }

    private void GetBoundaryPointsAtDepth(
        ScreenSpaceOutlineBoundarySegment segment,
        float depth,
        out Vector2 pointA,
        out Vector2 pointB)
    {
        float t = GetBoundaryParameterAtDepth(segment, depth);
        pointA = Vector2.LerpUnclamped(segment.StartA, segment.EndA, t);
        pointB = Vector2.LerpUnclamped(segment.StartB, segment.EndB, t);
    }

    private float GetBoundaryParameterAtDepth(ScreenSpaceOutlineBoundarySegment segment, float depth)
    {
        float depthLength = segment.EndDepth - segment.StartDepth;
        if (Mathf.Abs(depthLength) <= Mathf.Epsilon) { return 0f; }

        return Mathf.Clamp01((depth - segment.StartDepth) / depthLength);
    }

    private bool ArePointsNear(Vector2 a, Vector2 b, float toleranceSquared)
    {
        return (a - b).sqrMagnitude <= toleranceSquared;
    }

    private void SubtractInterval(List<NormalizedInterval> intervals, NormalizedInterval covered)
    {
        const float intervalEpsilon = 0.000001f;
        List<NormalizedInterval> remaining = new List<NormalizedInterval>();

        foreach (NormalizedInterval current in intervals)
        {
            float overlapStart = Mathf.Max(current.Start, covered.Start);
            float overlapEnd = Mathf.Min(current.End, covered.End);
            if (overlapEnd - overlapStart <= intervalEpsilon)
            {
                remaining.Add(current);
                continue;
            }

            if (current.Start < overlapStart - intervalEpsilon)
            {
                remaining.Add(new NormalizedInterval(current.Start, overlapStart));
            }

            if (overlapEnd < current.End - intervalEpsilon)
            {
                remaining.Add(new NormalizedInterval(overlapEnd, current.End));
            }
        }

        intervals.Clear();
        intervals.AddRange(remaining);
    }

    private ScreenSpaceOutlineBoundarySegment SliceBoundarySegment(
        ScreenSpaceOutlineBoundarySegment segment,
        float startT,
        float endT)
    {
        return new ScreenSpaceOutlineBoundarySegment(
            Vector2.LerpUnclamped(segment.StartA, segment.EndA, startT),
            Vector2.LerpUnclamped(segment.StartB, segment.EndB, startT),
            Vector2.LerpUnclamped(segment.StartA, segment.EndA, endT),
            Vector2.LerpUnclamped(segment.StartB, segment.EndB, endT),
            Mathf.LerpUnclamped(segment.StartDepth, segment.EndDepth, startT),
            Mathf.LerpUnclamped(segment.StartDepth, segment.EndDepth, endT),
            segment.BaseDepth);
    }

    private string CreateBoundaryTrajectoryKey(ScreenSpaceOutlineBoundarySegment segment)
    {
        float depthLength = segment.EndDepth - segment.StartDepth;
        if (Mathf.Abs(depthLength) <= Mathf.Epsilon)
        {
            return $"D:{QuantizeValue(segment.StartDepth)}:{QuantizePoint(segment.StartA)}:{QuantizePoint(segment.StartB)}";
        }

        string trajectoryA = CreatePointTrajectoryKey(segment.StartA, segment.EndA, segment.StartDepth, depthLength);
        string trajectoryB = CreatePointTrajectoryKey(segment.StartB, segment.EndB, segment.StartDepth, depthLength);
        if (string.CompareOrdinal(trajectoryA, trajectoryB) > 0)
        {
            (trajectoryA, trajectoryB) = (trajectoryB, trajectoryA);
        }

        return $"T:{trajectoryA}|{trajectoryB}";
    }

    private string CreatePointTrajectoryKey(Vector2 start, Vector2 end, float startDepth, float depthLength)
    {
        Vector2 slope = (end - start) / depthLength;
        Vector2 intercept = start - slope * startDepth;
        return $"{QuantizePoint(slope)}:{QuantizePoint(intercept)}";
    }

    private string QuantizePoint(Vector2 point)
    {
        return $"{QuantizeValue(point.x)},{QuantizeValue(point.y)}";
    }

    private int QuantizeValue(float value)
    {
        float unit = Mathf.Max(screenSpaceOutlineBoundaryQuantizeUnit, 0.000001f);
        return Mathf.RoundToInt(value / unit);
    }

    private readonly struct NormalizedInterval
    {
        public NormalizedInterval(float start, float end)
        {
            Start = start;
            End = end;
        }

        public float Start { get; }
        public float End { get; }
    }

    private readonly struct ScreenSpaceOutlineBoundarySegment
    {
        public ScreenSpaceOutlineBoundarySegment(
            Vector2 startA,
            Vector2 startB,
            Vector2 endA,
            Vector2 endB,
            float startDepth,
            float endDepth,
            float baseDepth)
        {
            StartA = startA;
            StartB = startB;
            EndA = endA;
            EndB = endB;
            StartDepth = startDepth;
            EndDepth = endDepth;
            BaseDepth = baseDepth;
        }

        public Vector2 StartA { get; }
        public Vector2 StartB { get; }
        public Vector2 EndA { get; }
        public Vector2 EndB { get; }
        public float StartDepth { get; }
        public float EndDepth { get; }
        public float BaseDepth { get; }
    }

    private MeshRenderer GenerateShadowMeshObject(NoteData_SpaceHoldMesh noteData, INotePositionCalculator positionCalculator)
    {
        var obj = Instantiate(shadowMeshPrefab);
        NoteLayerUtility.SetNotesLayerRecursively(obj);
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
