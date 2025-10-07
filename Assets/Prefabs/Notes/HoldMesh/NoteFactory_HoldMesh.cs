using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;
using Deform;

public class NoteFactory_HoldMesh : NoteFactory<NoteData_HoldMesh>
{
    [SerializeField] GameObject noteObjectOriginPrefab;

    [Header("meshの1レーン内の分割数")]
    [SerializeField] int meshHorizontalDivisionNum = 10;

    [Header("mesh1単位の最大長さ")]
    [SerializeField] float maxTriangleLength = 0.5f;

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
        data.OptionGetter = optionHolder;
        
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
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        Mesh mesh = GroundHoldMeshGenerator.GenerateGroundHoldMesh(noteData.TimeToRanges, optionHolder.NoteSpeed.Value, meshHorizontalDivisionNum, maxTriangleLength);

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshFilter.mesh = mesh;

        obj.AddComponent<Deformable>().AddDeformer(groundDeformer);
        return obj;
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
            optionHolder.NoteSpeed.Value * data.Timing
            );

        // 動く地面を親登録
        note.transform.SetParent(groundObject.transform);
    }
}
