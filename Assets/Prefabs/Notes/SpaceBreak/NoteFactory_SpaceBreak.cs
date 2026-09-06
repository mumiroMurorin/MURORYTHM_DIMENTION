using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;
using RayFire;

public class NoteFactory_SpaceBreak : NoteFactory<NoteData_SpaceBreak>
{
    readonly Vector3 CENTER_PIVOT = Vector3.zero;
    readonly float RADIUS = 10f;

    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] SpaceBreakJudgementSettings judgementSettings;
    [SerializeField] GameObject noteMeshPrefab;
    [SerializeField] GameObject noteShadowPrefab;
    [SerializeField] GameObject frangmentParentPrefab;
    [Header("厚さ")]
    [SerializeField] float noteDepth = 0.1f;
    [Header("欠片分割数")]
    [SerializeField] int fragmentAmount = 20;
    [Header("メインメッシュのマテリアル")]
    [SerializeField] Material mainMaterial;
    [Header("輪郭線のマテリアル")]
    [SerializeField] Material edgeMaterial;
    [Header("輪郭線の太さ")]
    [SerializeField] float edgeWidth = 0.05f;
    [SerializeField] float edgeDepthOffset = 0f;
    [SerializeField] int shadowDivisionNum = 24;
    [SerializeField] float shadowRadiusOffset = 0.02f;
    [Header("地面線の太さ")]
    [SerializeField] bool enableGroundLine = true;
    [SerializeField] Material groundLineMaterial;
    [SerializeField] float groundLineWidth = 0.04f;

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

    public override NoteObject<NoteData_SpaceBreak> Spawn(NoteData_SpaceBreak data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_SpaceBreak> note = GenerateNoteInstance(ConvertNoteData(data));

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
    private NoteData_SpaceBreak ConvertNoteData(NoteData_SpaceBreak data)
    {
        // ノーツデータにいろいろ追加
        data.SpaceInput = this.spaceInputGetter;
        data.Timer = this.timer;
        data.JudgementRecorder = this.judgementRecorder;
        data.OptionGetter = optionHolder;
        data.JudgementSettings = judgementSettings;
        if (judgementSettings != null)
        {
            data.JudgementWindow = judgementSettings.CreateJudgementWindowIfMissing(data.JudgementWindow, currentDifficulty);
        }

        return data;
    }

    /// <summary>
    /// ノーツをインスタンス化して返す
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private NoteObject<NoteData_SpaceBreak> GenerateNoteInstance(NoteData_SpaceBreak data)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);
        bool shouldGenerateFragments = true;
        if (origin.TryGetComponent(out NoteObjectPreview_SpaceBreak previewSpaceBreak))
        {
            shouldGenerateFragments = previewSpaceBreak.IsFragmentPrebuildEnabled;
        }

        // ノーツオブジェクト(表)を生成
        GameObject noteObj = GenerateMeshObject(data, shouldGenerateFragments);
        noteObj.transform.SetParent(origin.transform);

        // 影オブジェクトを生成
        MeshRenderer shadowMesh = GenerateShadowMeshObject(data);
        shadowMesh.transform.SetParent(origin.transform, false);

        if (enableGroundLine)
        {
            // SpaceBreakの最下点から、湾曲したグラウンドまでの線を生成
            MeshRenderer groundLineMesh = GenerateGroundLineObject(data);
            groundLineMesh.transform.SetParent(origin.transform, false);
        }

        // コンポーネントを取得
        NoteObject<NoteData_SpaceBreak> note = origin.GetComponent<NoteObject<NoteData_SpaceBreak>>();
        data.MeshRendererAsset = new SpaceBreakMeshRendererAsset(shadowMesh);

        return note;
    }

    /// <summary>
    /// ホールドのメッシュ部分の生成
    /// </summary>
    private GameObject GenerateMeshObject(NoteData_SpaceBreak noteData, bool shouldGenerateFragments)
    {
        var obj = Instantiate(noteMeshPrefab);
        NoteLayerUtility.SetNotesLayer(obj);

        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        var points = noteData.Vertices.Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToList();
        var mesh = MeshGenerator.GenerateMeshWithDepth(points, noteDepth);
        meshFilter.mesh = mesh;

        if (mesh != null)
        {
            meshRenderer.material = mainMaterial;
        }

        // 成功演出のためにMeshを保存
        noteData.Mesh = mesh;

        if (shouldGenerateFragments)
        {
            GenerateFlagments(obj, noteData);
        }

        if (mesh != null)
        {
            var edgeObj = GenerateEdgeObject(points);
            edgeObj.transform.SetParent(obj.transform, false);
        }

        return obj;
    }

    private MeshRenderer GenerateShadowMeshObject(NoteData_SpaceBreak noteData)
    {
        var obj = Instantiate(noteShadowPrefab);
        NoteLayerUtility.SetNotesLayerRecursively(obj);
        obj.name = "SpaceBreakShadow";

        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        List<Vector2> points = noteData.Vertices
            .Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS))
            .ToList();

        Mesh mesh = SpaceBreakShadowMeshGenerator.GenerateSpaceBreakShadowMesh(
            points,
            noteDepth,
            shadowDivisionNum,
            optionHolder.NoteCurveRadius.Value,
            RADIUS,
            shadowRadiusOffset);

        meshFilter.mesh = mesh;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return meshRenderer;
    }

    private MeshRenderer GenerateGroundLineObject(NoteData_SpaceBreak noteData)
    {
        var obj = new GameObject("SpaceBreakGroundLine");
        NoteLayerUtility.SetNotesLayer(obj);

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

        List<Vector2> points = noteData.Vertices
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

        return meshRenderer;
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
    /// 前面・背面の輪郭線を生成
    /// </summary>
    private GameObject GenerateEdgeObject(List<Vector2> points)
    {
        var edgeParent = new GameObject("SpaceBreakEdge");
        NoteLayerUtility.SetNotesLayer(edgeParent);

        float halfDepth = noteDepth * 0.5f;
        int baseRenderQueue = mainMaterial != null ? mainMaterial.renderQueue : 3002;

        var backPoints = points.Select(p => new Vector3(p.x, p.y, halfDepth + edgeDepthOffset)).ToList();
        var frontPoints = points.Select(p => new Vector3(p.x, p.y, -halfDepth - edgeDepthOffset)).ToList();

        CreateLineObject("SpaceBreakEdge_Back", backPoints, edgeParent.transform);
        CreateLineObject("SpaceBreakEdge_Front", frontPoints, edgeParent.transform);

        return edgeParent;
    }

    private void CreateLineObject(string name, List<Vector3> points, Transform parent)
    {
        var obj = new GameObject(name);
        NoteLayerUtility.SetNotesLayer(obj);
        obj.transform.SetParent(parent, false);

        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        meshFilter.mesh = MeshGenerator.GenerateLineMesh(points, edgeWidth, true);
        meshRenderer.material = edgeMaterial;
    }

    private void GenerateFlagments(GameObject meshObj, NoteData_SpaceBreak noteData)
    {
        if (!meshObj.TryGetComponent(out RayfireRigid rf)) { rf = meshObj.AddComponent<RayfireRigid>(); }

        // 色々設定
        rf.meshDemolition.am = fragmentAmount;  // フラグメント数
        rf.physics.ct = RFColliderType.None;    // 元メッシュのコライダーを消す
        rf.meshDemolition.cld = false;          // 子の輪郭線メッシュを破片化対象から外す
        rf.meshDemolition.prp.col = RFColliderType.None;    // 破片のコライダーを消す
        rf.reset.destroyDelay = float.MaxValue;    // 自動でプールされるのを防ぐ 
        //rf.simulationType = SimType.Inactive;
        //rf.demolitionType = DemolitionType.AwakePrefragment;    

        rf.demolitionEvent.LocalEvent += (rf) => OnDemolished(rf, noteData, meshObj);
        rf.Initialize();
        rf.Demolish();

        if (meshObj.TryGetComponent(out Rigidbody rb)) { Destroy(rb); }
    }

    private void OnDemolished(RayfireRigid rigid, NoteData_SpaceBreak noteData, GameObject origin)
    {
        // 欠片たちを一つの親オブジェクトにまとめる
        var parent = Instantiate(frangmentParentPrefab).transform;
        parent.SetParent(origin.transform);
        parent.localPosition = Vector3.zero;
        parent.gameObject.SetActive(false);

        foreach (RayfireRigid frag in rigid.fragments)
        {
            frag.gameObject.transform.SetParent(parent);
        }

        // 爆発の準備
        if (!parent.TryGetComponent(out FragmentsBomb bomb)) { bomb = parent.gameObject.AddComponent<FragmentsBomb>(); }

        bomb.Initialize();
        noteData.FlagmentBomb = bomb;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_SpaceBreak> note, float spawnZ)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(spawnZ, optionHolder.NoteCurveRadius.Value);
    }
}
