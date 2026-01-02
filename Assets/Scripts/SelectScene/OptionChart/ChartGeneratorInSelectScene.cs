using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;
using VContainer;
using System;
using System.Linq;
using UniRx;

public class ChartGeneratorInSelectScene : MonoBehaviour, IChartGenerator
{
    [SerializeField] ChartControllerInSelectScene chartController;

    [Header("それぞれのNoteFactory")]
    [SerializeField] NoteFactory<NoteData_Touch> touchNoteFactory;
    [SerializeField] NoteFactory<NoteData_DivineTouch> divineTouchNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundUpward> dynamicGroundUpwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundRightward> dynamicGroundRightwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundLeftward> dynamicGroundLeftwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundDownward> dynamicGroundDownwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldStart> holdStartNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldRelay> holdRelayNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldRelayHidden> holdRelayHiddenNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldEnd> holdEndNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldEndUnjudge> holdEndUnjudgeNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldMesh> holdMeshNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceHoldMesh> spaceHoldMeshNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceHoldRelay> spaceHoldRelayNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceHoldRelayHidden> spaceHoldRelayHiddenNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceBreak> spaceBreakNoteFactory;

    [Header("Factoryの初期化に必要なデータ")]
    [SerializeField] GameObject groundObject;
    [SerializeField] Deformer groundDeformer;
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    INoteSpawnDataOptionHolder spawnDataOptionHolder;
    ISliderInputGetter sliderInputGetter;
    ISpaceInputGetter spaceInputGetter;
    IJudgementRecorder judgementRecorder;
    IOptionGetter optionGetter;

    List<GameObject> noteObjects;

    [Inject]
    public void Constructor(INoteSpawnDataOptionHolder spawnDataOptionHolder, ISliderInputGetter sliderInputGetter, ISpaceInputGetter spaceInputGetter, IJudgementRecorder judgementRecorder, IOptionGetter optionGetter)
    {
        this.spawnDataOptionHolder = spawnDataOptionHolder;
        this.sliderInputGetter = sliderInputGetter;
        this.spaceInputGetter = spaceInputGetter;
        this.judgementRecorder = judgementRecorder;
        this.optionGetter = optionGetter;
    }

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void Initialize()
    {
        // 初期化データの生成
        NoteFactoryInitializingData data = new NoteFactoryInitializingData
        {
            GroundObject = this.groundObject,
            GroundDeformer = this.groundDeformer,
            OptionHolder = this.spawnDataOptionHolder,
            SliderInputGetter = this.sliderInputGetter,
            SpaceInputGetter = this.spaceInputGetter,
            Timer = this.timer.Value,
            JudgementRecorder = this.judgementRecorder
        };

        touchNoteFactory.Initialize(data);
        divineTouchNoteFactory.Initialize(data);
        dynamicGroundUpwardNoteFactory.Initialize(data);
        dynamicGroundRightwardNoteFactory.Initialize(data);
        dynamicGroundLeftwardNoteFactory.Initialize(data);
        dynamicGroundDownwardNoteFactory.Initialize(data);
        holdStartNoteFactory.Initialize(data);
        holdRelayNoteFactory.Initialize(data);
        holdRelayHiddenNoteFactory.Initialize(data);
        holdEndNoteFactory.Initialize(data);
        holdEndUnjudgeNoteFactory.Initialize(data);
        holdMeshNoteFactory.Initialize(data);
        spaceHoldMeshNoteFactory.Initialize(data);
        spaceHoldRelayNoteFactory.Initialize(data);
        spaceHoldRelayHiddenNoteFactory.Initialize(data);
        spaceBreakNoteFactory.Initialize(data);
    }

    /// <summary>
    /// ノーツ全体の生成
    /// </summary>
    /// <param name="ChartData"></param>
    public void Generate(Action callback = null)
    {
        // リセット
        if (noteObjects == null) { noteObjects = new List<GameObject>(); }
        foreach(var obj in noteObjects)
        {
            Destroy(obj);
        }

        GenerateTouchNote(chartController.ChartData.GetNoteDataList(NoteType.Touch).OfType<NoteData_Touch>().ToList());
        GenerateDevineTouchNote(chartController.ChartData.GetNoteDataList(NoteType.DivineTouch).OfType<NoteData_DivineTouch>().ToList());
        GenerateDynamicGroundUpwardNote(chartController.ChartData.GetNoteDataList(NoteType.DynamicGroundUpward).OfType<NoteData_DynamicGroundUpward>().ToList());
        GenerateDynamicGroundRightwardNote(chartController.ChartData.GetNoteDataList(NoteType.DynamicGroundRightward).OfType<NoteData_DynamicGroundRightward>().ToList());
        GenerateDynamicGroundLeftwardNote(chartController.ChartData.GetNoteDataList(NoteType.DynamicGroundLeftward).OfType<NoteData_DynamicGroundLeftward>().ToList());
        GenerateDynamicGroundDownwardNote(chartController.ChartData.GetNoteDataList(NoteType.DynamicGroundDownward).OfType<NoteData_DynamicGroundDownward>().ToList());
        GenerateHoldStartNote(chartController.ChartData.GetNoteDataList(NoteType.HoldStart).OfType<NoteData_HoldStart>().ToList());
        GenerateHoldRelayNote(chartController.ChartData.GetNoteDataList(NoteType.HoldRelay).OfType<NoteData_HoldRelay>().ToList());
        GenerateHoldRelayHiddenNote(chartController.ChartData.GetNoteDataList(NoteType.HoldRelayHidden).OfType<NoteData_HoldRelayHidden>().ToList());
        GenerateHoldEndNote(chartController.ChartData.GetNoteDataList(NoteType.HoldEnd).OfType<NoteData_HoldEnd>().ToList());
        GenerateHoldEndUnjudgeNote(chartController.ChartData.GetNoteDataList(NoteType.HoldEndUnjudge).OfType<NoteData_HoldEndUnjudge>().ToList());
        GenerateHoldMeshNote(chartController.ChartData.GetNoteDataList(NoteType.HoldMesh).OfType<NoteData_HoldMesh>().ToList());
        GenerateSpaceHoldMeshNote(chartController.ChartData.GetNoteDataList(NoteType.SpaceHoldMesh).OfType<NoteData_SpaceHoldMesh>().ToList());
        GenerateSpaceHoldRelayNote(chartController.ChartData.GetNoteDataList(NoteType.SpaceHoldRelay).OfType<NoteData_SpaceHoldRelay>().ToList());
        GenerateSpaceHoldRelayHiddenNote(chartController.ChartData.GetNoteDataList(NoteType.SpaceHoldRelayHidden).OfType<NoteData_SpaceHoldRelayHidden>().ToList());
        GenerateSpaceBreakNote(chartController.ChartData.GetNoteDataList(NoteType.SpaceBreak).OfType<NoteData_SpaceBreak>().ToList());

        callback?.Invoke();
    }

    /// <summary>
    /// タッチノーツの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateTouchNote(List<NoteData_Touch> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_Touch data in noteDatas)
        {
            noteObjects.Add(touchNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }


    /// <summary>
    /// 神タッチノーツの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateDevineTouchNote(List<NoteData_DivineTouch> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_DivineTouch data in noteDatas)
        {
            noteObjects.Add(divineTouchNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ダイナミックグラウンド(↑)ノーツの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateDynamicGroundUpwardNote(List<NoteData_DynamicGroundUpward> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_DynamicGroundUpward data in noteDatas)
        {
            noteObjects.Add(dynamicGroundUpwardNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ダイナミックグラウンド(→)ノーツの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateDynamicGroundRightwardNote(List<NoteData_DynamicGroundRightward> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_DynamicGroundRightward data in noteDatas)
        {
            noteObjects.Add(dynamicGroundRightwardNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ダイナミックグラウンド(←)ノーツの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateDynamicGroundLeftwardNote(List<NoteData_DynamicGroundLeftward> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_DynamicGroundLeftward data in noteDatas)
        {
            noteObjects.Add(dynamicGroundLeftwardNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ダイナミックグラウンド(↓)ノーツの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateDynamicGroundDownwardNote(List<NoteData_DynamicGroundDownward> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_DynamicGroundDownward data in noteDatas)
        {
            noteObjects.Add(dynamicGroundDownwardNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ホールドノーツ始点の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateHoldStartNote(List<NoteData_HoldStart> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_HoldStart data in noteDatas)
        {
            noteObjects.Add(holdStartNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ホールドノーツ中継点の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateHoldRelayNote(List<NoteData_HoldRelay> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_HoldRelay data in noteDatas)
        {
            noteObjects.Add(holdRelayNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ホールドノーツ判定点の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateHoldRelayHiddenNote(List<NoteData_HoldRelayHidden> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_HoldRelayHidden data in noteDatas)
        {
            noteObjects.Add(holdRelayHiddenNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ホールドノーツ終点の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateHoldEndNote(List<NoteData_HoldEnd> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_HoldEnd data in noteDatas)
        {
            noteObjects.Add(holdEndNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ホールドノーツ終点(判定なし)の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateHoldEndUnjudgeNote(List<NoteData_HoldEndUnjudge> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_HoldEndUnjudge data in noteDatas)
        {
            noteObjects.Add(holdEndUnjudgeNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// ホールドメッシュの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateHoldMeshNote(List<NoteData_HoldMesh> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_HoldMesh data in noteDatas)
        {
            noteObjects.Add(holdMeshNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// スペースホールドメッシュの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateSpaceHoldMeshNote(List<NoteData_SpaceHoldMesh> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_SpaceHoldMesh data in noteDatas)
        {
            noteObjects.Add(spaceHoldMeshNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }


    /// <summary>
    /// スペースホールド中点の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateSpaceHoldRelayNote(List<NoteData_SpaceHoldRelay> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_SpaceHoldRelay data in noteDatas)
        {
            noteObjects.Add(spaceHoldRelayNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// スペースホールド中点の生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateSpaceHoldRelayHiddenNote(List<NoteData_SpaceHoldRelayHidden> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (NoteData_SpaceHoldRelayHidden data in noteDatas)
        {
            noteObjects.Add(spaceHoldRelayHiddenNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }

    /// <summary>
    /// スペースブレイクの生成
    /// </summary>
    /// <param name="noteData_Touches"></param>
    private void GenerateSpaceBreakNote(List<NoteData_SpaceBreak> noteDatas)
    {
        if (noteDatas == null) { return; }

        foreach (var data in noteDatas)
        {
            noteObjects.Add(spaceBreakNoteFactory.Spawn(data, new PositionGraphOption()).gameObject);
        }
    }
}
