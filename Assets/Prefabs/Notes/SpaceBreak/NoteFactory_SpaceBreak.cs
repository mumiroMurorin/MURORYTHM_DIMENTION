using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;
using Deform;
using RayFire;

public class NoteFactory_SpaceBreak : NoteFactory<NoteData_SpaceBreak>
{
    readonly Vector3 CENTER_PIVOT = Vector3.zero;
    readonly float RADIUS = 10f;

    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] GameObject noteMeshPrefab;
    [SerializeField] GameObject frangmentParentPrefab;
    [Header("厚さ")]
    [SerializeField] float noteDepth = 0.1f;
    [Header("欠片分割数")]
    [SerializeField] int fragmentAmount = 20;
    [Header("メインメッシュのマテリアル")]
    [SerializeField] Material mainMaterial;

    INoteSpawnDataOptionHolder optionHolder;
    ISpaceInputGetter spaceInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    GameObject groundObject;
    Deformer groundDeformer;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.groundObject = initializingData.GroundObject;
        this.groundDeformer = initializingData.GroundDeformer;
        this.spaceInputGetter = initializingData.SpaceInputGetter;
        this.judgementRecorder = initializingData.JudgementRecorder;
        this.timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_SpaceBreak> Spawn(NoteData_SpaceBreak data)
    {
        // 生成
        NoteObject<NoteData_SpaceBreak> note = GenerateNoteInstance(ConvertNoteData(data));

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
    private NoteData_SpaceBreak ConvertNoteData(NoteData_SpaceBreak data)
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
    private NoteObject<NoteData_SpaceBreak> GenerateNoteInstance(NoteData_SpaceBreak data)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // ノーツオブジェクト(表)を生成
        GameObject noteObj = GenerateMeshObject(data);
        noteObj.transform.SetParent(origin.transform);

        // コンポーネントを取得
        NoteObject<NoteData_SpaceBreak> note = origin.GetComponent<NoteObject<NoteData_SpaceBreak>>();

        return note;
    }

    /// <summary>
    /// ホールドのメッシュ部分の生成
    /// </summary>
    private GameObject GenerateMeshObject(NoteData_SpaceBreak noteData)
    {
        var obj = Instantiate(noteMeshPrefab);

        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        var points = noteData.Vertices.Select(v => (Vector2)MeshGenerator.Normalize(v, CENTER_PIVOT, RADIUS)).ToList();
        var mesh = MeshGenerator.GenerateMeshWithDepth(points, noteDepth);
        meshFilter.mesh = mesh;

        if(mesh != null)
        {
            meshRenderer.material = mainMaterial;

            if (!obj.TryGetComponent(out Deformable d)) { obj.AddComponent<Deformable>().AddDeformer(groundDeformer); }
            else { d.AddDeformer(groundDeformer); }
        }

        // 成功演出のためにMeshを保存
        noteData.Mesh = mesh;

        GenerateFlagments(obj, noteData);

        return obj;
    }

    private void GenerateFlagments(GameObject meshObj, NoteData_SpaceBreak noteData)
    {
        if (!meshObj.TryGetComponent(out RayfireRigid rf)) { rf = meshObj.AddComponent<RayfireRigid>(); }

        // 色々設定
        rf.meshDemolition.am = fragmentAmount;  // フラグメント数
        rf.meshDemolition.prp.col = RFColliderType.None;    // コライダーを消す
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
    private void SetTransform(NoteObject<NoteData_SpaceBreak> note, NoteData_SpaceBreak data)
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
