using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;
using VContainer;
using System;
using System.Linq;
using UniRx;

public class ChartGenerator : MonoBehaviour, IChartGenerator
{
    [Header("それぞれのNoteFactory")]
    [SerializeField] NoteFactory<NoteData_Touch> touchNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundUpward> dynamicGroundUpwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundRightward> dynamicGroundRightwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundLeftward> dynamicGroundLeftwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_DynamicGroundDownward> dynamicGroundDownwardNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldStart> holdStartNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldRelay> holdRelayNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldRelayHidden> holdRelayHiddenNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldEnd> holdEndNoteFactory;
    [SerializeField] NoteFactory<NoteData_HoldMesh> holdMeshNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceHoldMesh> spaceHoldMeshNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceHoldRelay> spaceHoldRelayNoteFactory;
    [SerializeField] NoteFactory<NoteData_SpaceHoldRelayHidden> spaceHoldRelayHiddenNoteFactory;

    [Header("Factoryの初期化に必要なデータ")]
    [SerializeField] GameObject groundObject;
    [SerializeField] Deformer groundDeformer;
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    IChartDataGetter chartDataGetter;
    INoteSpawnDataOptionHolder spawnDataOptionHolder;
    ISliderInputGetter sliderInputGetter;
    ISpaceInputGetter spaceInputGetter;
    IJudgementRecorder judgementRecorder;

    private void Awake()
    {
        Initialize();
    }

    [Inject]
    public void Constructor(IChartDataGetter chartDataGetter, INoteSpawnDataOptionHolder optionHolder, IJudgementRecorder judgementRecorder,
        ISliderInputGetter sliderInputGetter, ISpaceInputGetter spaceInputGetter)
    {
        this.chartDataGetter = chartDataGetter;
        this.spawnDataOptionHolder = optionHolder;
        this.sliderInputGetter = sliderInputGetter;
        this.spaceInputGetter = spaceInputGetter;
        this.judgementRecorder = judgementRecorder;

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
        dynamicGroundUpwardNoteFactory.Initialize(data);
        dynamicGroundRightwardNoteFactory.Initialize(data);
        dynamicGroundLeftwardNoteFactory.Initialize(data);
        dynamicGroundDownwardNoteFactory.Initialize(data);
        holdStartNoteFactory.Initialize(data);
        holdRelayNoteFactory.Initialize(data);
        holdRelayHiddenNoteFactory.Initialize(data);
        holdEndNoteFactory.Initialize(data);
        holdMeshNoteFactory.Initialize(data);
        spaceHoldMeshNoteFactory.Initialize(data);
        spaceHoldRelayNoteFactory.Initialize(data);
        spaceHoldRelayHiddenNoteFactory.Initialize(data);
    }

    /// <summary>
    /// ノーツ全体の生成
    /// </summary>
    /// <param name="chartData"></param>
    public void Generate(Action callback = null)
    {
        GenerateTouchNote(chartDataGetter.Chart.GetNoteDataList(NoteType.Touch).OfType<NoteData_Touch>().ToList());
        GenerateDynamicGroundUpwardNote(chartDataGetter.Chart.GetNoteDataList(NoteType.DynamicGroundUpward).OfType<NoteData_DynamicGroundUpward>().ToList());
        GenerateDynamicGroundRightwardNote(chartDataGetter.Chart.GetNoteDataList(NoteType.DynamicGroundRightward).OfType<NoteData_DynamicGroundRightward>().ToList());
        GenerateDynamicGroundLeftwardNote(chartDataGetter.Chart.GetNoteDataList(NoteType.DynamicGroundLeftward).OfType<NoteData_DynamicGroundLeftward>().ToList());
        GenerateDynamicGroundDownwardNote(chartDataGetter.Chart.GetNoteDataList(NoteType.DynamicGroundDownward).OfType<NoteData_DynamicGroundDownward>().ToList());
        GenerateHoldStartNote(chartDataGetter.Chart.GetNoteDataList(NoteType.HoldStart).OfType<NoteData_HoldStart>().ToList());
        GenerateHoldRelayNote(chartDataGetter.Chart.GetNoteDataList(NoteType.HoldRelay).OfType<NoteData_HoldRelay>().ToList());
        GenerateHoldRelayHiddenNote(chartDataGetter.Chart.GetNoteDataList(NoteType.HoldRelayHidden).OfType<NoteData_HoldRelayHidden>().ToList());
        GenerateHoldEndNote(chartDataGetter.Chart.GetNoteDataList(NoteType.HoldEnd).OfType<NoteData_HoldEnd>().ToList());
        GenerateHoldMeshNote(chartDataGetter.Chart.GetNoteDataList(NoteType.HoldMesh).OfType<NoteData_HoldMesh>().ToList());
        GenerateSpaceHoldMeshNote(chartDataGetter.Chart.GetNoteDataList(NoteType.SpaceHoldMesh).OfType<NoteData_SpaceHoldMesh>().ToList());
        GenerateSpaceHoldRelayNote(chartDataGetter.Chart.GetNoteDataList(NoteType.SpaceHoldRelay).OfType<NoteData_SpaceHoldRelay>().ToList());
        GenerateSpaceHoldRelayHiddenNote(chartDataGetter.Chart.GetNoteDataList(NoteType.SpaceHoldRelayHidden).OfType<NoteData_SpaceHoldRelayHidden>().ToList());

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
            touchNoteFactory.Spawn(data);
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
            dynamicGroundUpwardNoteFactory.Spawn(data);
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
            dynamicGroundRightwardNoteFactory.Spawn(data);
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
            dynamicGroundLeftwardNoteFactory.Spawn(data);
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
            dynamicGroundDownwardNoteFactory.Spawn(data);
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
            holdStartNoteFactory.Spawn(data);
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
            holdRelayNoteFactory.Spawn(data);
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
            holdRelayHiddenNoteFactory.Spawn(data);
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
            holdEndNoteFactory.Spawn(data);
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
            holdMeshNoteFactory.Spawn(data);
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
            spaceHoldMeshNoteFactory.Spawn(data);
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
            spaceHoldRelayNoteFactory.Spawn(data);
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
            spaceHoldRelayHiddenNoteFactory.Spawn(data);
        }
    }
}
