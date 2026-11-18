using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deform;
using UnityEngine;
using VContainer;

public class ChartGenerator : MonoBehaviour, IChartGenerator
{
    [Serializable]
    private class NoteFactoryBinding
    {
        [SerializeField] private NoteType noteType;
        [SerializeField] private MonoBehaviour factory;

        public NoteType NoteType => noteType;
        public MonoBehaviour Factory => factory;
    }

    [Header("Note Factories")]
    [SerializeField] private NoteFactoryBinding[] noteFactories;

    [Header("Factory Initialization")]
    [SerializeField] private NoteJudgementSettingsCatalog judgementSettingsCatalog;
    [SerializeField] private Transform noteParent;
    [SerializeField] private SerializeInterface<ITimeGetter> timer;
    [SerializeField] private NoteVisibilityController noteVisibilityController;

    [Header("Note Visibility")]
    [SerializeField] private float visibleBehindDistance = 5f;

    private IChartDataGetter chartDataGetter;
    private IMusicDataGetter musicDataGetter;
    private INoteSpawnDataOptionGetter spawnDataOptionHolder;
    private ISliderInputGetter sliderInputGetter;
    private ISpaceInputGetter spaceInputGetter;
    private IJudgementRecorder judgementRecorder;
    private IOptionGetter optionGetter;

    [Inject]
    public void Constructor(
        IChartDataGetter chartDataGetter,
        IMusicDataGetter musicDataGetter,
        INoteSpawnDataOptionGetter spawnDataOptionHolder,
        ISliderInputGetter sliderInputGetter,
        ISpaceInputGetter spaceInputGetter,
        IJudgementRecorder judgementRecorder,
        IOptionGetter optionGetter)
    {
        this.chartDataGetter = chartDataGetter;
        this.musicDataGetter = musicDataGetter;
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

    private void Initialize()
    {
        if (timer == null || timer.Value == null)
        {
            Debug.LogWarning("[ChartGenerator] Timer is not assigned.");
            return;
        }

        if (noteVisibilityController == null)
        {
            noteVisibilityController = GetComponent<NoteVisibilityController>();
        }

        if (noteVisibilityController == null)
        {
            noteVisibilityController = gameObject.AddComponent<NoteVisibilityController>();
        }

        NoteFactoryInitializingData data = new NoteFactoryInitializingData
        {
            NoteParent = noteParent,
            Difficulty = musicDataGetter != null ? musicDataGetter.Difficulty.Value : Difficulty.Normal,
            JudgementSettingsCatalog = judgementSettingsCatalog,
            OptionHolder = spawnDataOptionHolder,
            SliderInputGetter = sliderInputGetter,
            SpaceInputGetter = spaceInputGetter,
            Timer = timer.Value,
            JudgementRecorder = judgementRecorder
        };

        if (noteFactories == null) { return; }
        foreach (var binding in noteFactories)
        {
            InitializeFactory(binding, data);
        }
    }

    public void Generate(Action callback = null)
    {
        Initialize();

        if (chartDataGetter == null || chartDataGetter.Chart == null)
        {
            // 次へ進まずにエラーを通知する (選曲画面へ戻る)
            GameFlowError.Raise("[ChartGenerator] 譜面データが無いため、ノーツを生成できません");
            return;
        }

        if (noteFactories == null)
        {
            // 判定のあるノーツを生成できないと曲が終わらないため、エラーにする
            if (chartDataGetter.Chart.MaxCombo > 0)
            {
                GameFlowError.Raise("[ChartGenerator] ノーツの生成設定 (Note Factories) がありません");
                return;
            }

            callback?.Invoke();
            return;
        }

        // 生成前に前回の登録情報を破棄する
        noteVisibilityController.Clear();
        noteVisibilityController.Initialize(
            timer.Value,
            chartDataGetter.Chart.PositionGraph,
            spawnDataOptionHolder.NoteSpeed.Value,
            spawnDataOptionHolder.NoteCurveRadius.Value,
            visibleBehindDistance,
            optionGetter?.NoteVisibleDistance.Value ?? 100f);

        // 各ノーツについて生成 (判定のあるノーツで生成できなかった数を数える)
        int failedJudgedNoteCount = 0;
        foreach (var binding in noteFactories)
        {
            failedJudgedNoteCount += SpawnEachType(
                binding,
                chartDataGetter.Chart,
                chartDataGetter.Chart.PositionGraph,
                spawned => noteVisibilityController.Register(spawned));
        }

        // 生成設定 (Note Factories) に無い種類のノーツも、生成できなかった扱いにする
        failedJudgedNoteCount += CountJudgedNotesWithoutFactory(chartDataGetter.Chart);

        noteVisibilityController.CompleteRegistration();

        // 判定のあるノーツが1つでも欠けると、判定数が MaxCombo に届かず曲が終わらないため、次へ進まずにエラーにする
        if (failedJudgedNoteCount > 0)
        {
            GameFlowError.Raise($"[ChartGenerator] 判定のあるノーツ {failedJudgedNoteCount} 個を生成できませんでした (このままでは曲が終わらないため中断します)");
            return;
        }

        callback?.Invoke();
    }

    /// <summary>
    /// 判定数 (MaxCombo) に数えられるノーツか (ChartData.AddNoteData と同じ条件)
    /// </summary>
    private static bool IsJudgedNote(INoteData noteData)
    {
        return noteData is IJudgableNoteData || noteData is IClippedJudgableNote;
    }

    private static int CountJudgedNotes(IEnumerable<INoteData> noteDataList)
    {
        int count = 0;
        foreach (INoteData noteData in noteDataList)
        {
            if (IsJudgedNote(noteData)) { count++; }
        }

        return count;
    }

    /// <summary>
    /// 生成できる設定が無い種類の、判定のあるノーツの数
    /// </summary>
    private int CountJudgedNotesWithoutFactory(global::ChartData chartData)
    {
        HashSet<NoteType> spawnableTypes = new HashSet<NoteType>();
        foreach (var binding in noteFactories)
        {
            if (binding == null || binding.Factory == null) { continue; }
            if (FindSpawnMethod(binding.Factory.GetType()) == null) { continue; }

            spawnableTypes.Add(binding.NoteType);
        }

        int count = 0;
        foreach (IEnumerable<INoteData> noteDataList in chartData.AllNoteDataLists)
        {
            INoteData first = noteDataList.FirstOrDefault();
            if (first == null || spawnableTypes.Contains(first.NoteType)) { continue; }

            int judgedCount = CountJudgedNotes(noteDataList);
            if (judgedCount > 0)
            {
                Debug.LogError($"[ChartGenerator] {first.NoteType} の生成設定がありません ({judgedCount} 個)");
                count += judgedCount;
            }
        }

        return count;
    }

    private static void InitializeFactory(NoteFactoryBinding binding, NoteFactoryInitializingData data)
    {
        if (binding == null || binding.Factory == null) { return; }

        MethodInfo initializeMethod = binding.Factory.GetType().GetMethod("Initialize", new[] { typeof(NoteFactoryInitializingData) });
        if (initializeMethod == null)
        {
            Debug.LogWarning($"[ChartGenerator] Initialize method not found: {binding.Factory.GetType().Name}");
            return;
        }

        initializeMethod.Invoke(binding.Factory, new object[] { data });
    }

    /// <summary>
    /// 1種類のノーツを生成する。戻り値は、判定のあるノーツのうち生成できなかった数。
    /// </summary>
    private static int SpawnEachType(NoteFactoryBinding binding, global::ChartData chartData, INotePositionCalculator positionCalculator, Action<Component> onSpawned)
    {
        if (binding == null || chartData == null) { return 0; }

        // 生成設定が壊れている種類は、ここでは数えない
        // (他に使える生成設定が無ければ、CountJudgedNotesWithoutFactory で生成できなかった数に入る)
        if (binding.Factory == null)
        {
            Debug.LogError($"[ChartGenerator] {binding.NoteType} の Factory が設定されていません");
            return 0;
        }

        MethodInfo spawnMethod = FindSpawnMethod(binding.Factory.GetType());
        if (spawnMethod == null)
        {
            Debug.LogError($"[ChartGenerator] Spawn method not found: {binding.Factory.GetType().Name}");
            return 0;
        }

        int failedJudgedNoteCount = 0;
        foreach (INoteData noteData in chartData.GetNoteDataList(binding.NoteType))
        {
            try
            {
                object spawned = spawnMethod.Invoke(binding.Factory, new object[] { noteData, positionCalculator });
                if (spawned is Component c)
                {
                    onSpawned?.Invoke(c);
                }
            }
            catch (Exception ex)
            {
                // リフレクション経由の例外は TargetInvocationException に包まれるので、中身を出す
                Exception cause = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                Debug.LogError($"[ChartGenerator] Spawn failed ({binding.NoteType}, {noteData.Timing:F3}s): {cause.Message}");
                Debug.LogException(cause);

                if (IsJudgedNote(noteData)) { failedJudgedNoteCount++; }
            }
        }

        return failedJudgedNoteCount;
    }

    private static MethodInfo FindSpawnMethod(Type factoryType)
    {
        foreach (MethodInfo m in factoryType.GetMethods())
        {
            if (m.Name != "Spawn") { continue; }
            ParameterInfo[] p = m.GetParameters();
            if (p.Length != 2) { continue; }
            if (typeof(INotePositionCalculator).IsAssignableFrom(p[1].ParameterType))
            {
                return m;
            }
        }

        return null;
    }
}
