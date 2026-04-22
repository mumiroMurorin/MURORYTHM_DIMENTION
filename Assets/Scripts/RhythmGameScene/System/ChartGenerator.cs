using System;
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
    [SerializeField] private Transform noteParent;
    [SerializeField] private Deformer groundDeformer;
    [SerializeField] private SerializeInterface<ITimeGetter> timer;

    private IChartDataGetter chartDataGetter;
    private INoteSpawnDataOptionGetter spawnDataOptionHolder;
    private ISliderInputGetter sliderInputGetter;
    private ISpaceInputGetter spaceInputGetter;
    private IJudgementRecorder judgementRecorder;

    [Inject]
    public void Constructor(
        IChartDataGetter chartDataGetter,
        INoteSpawnDataOptionGetter spawnDataOptionHolder,
        ISliderInputGetter sliderInputGetter,
        ISpaceInputGetter spaceInputGetter,
        IJudgementRecorder judgementRecorder,
        IOptionGetter optionGetter)
    {
        this.chartDataGetter = chartDataGetter;
        this.spawnDataOptionHolder = spawnDataOptionHolder;
        this.sliderInputGetter = sliderInputGetter;
        this.spaceInputGetter = spaceInputGetter;
        this.judgementRecorder = judgementRecorder;
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

        NoteFactoryInitializingData data = new NoteFactoryInitializingData
        {
            NoteParent = noteParent,
            GroundDeformer = groundDeformer,
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
        if (chartDataGetter == null || chartDataGetter.Chart == null)
        {
            Debug.LogWarning("[ChartGenerator] Chart data is null.");
            return;
        }

        if (noteFactories == null)
        {
            callback?.Invoke();
            return;
        }

        foreach (var binding in noteFactories)
        {
            SpawnEachType(binding, chartDataGetter.Chart, chartDataGetter.Chart.PositionGraph, _ => { });
        }

        callback?.Invoke();
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

    private static void SpawnEachType(NoteFactoryBinding binding, global::ChartData chartData, INotePositionCalculator positionCalculator, Action<GameObject> onSpawned)
    {
        if (binding == null || binding.Factory == null || chartData == null) { return; }

        MethodInfo spawnMethod = FindSpawnMethod(binding.Factory.GetType());
        if (spawnMethod == null)
        {
            Debug.LogWarning($"[ChartGenerator] Spawn method not found: {binding.Factory.GetType().Name}");
            return;
        }

        foreach (INoteData noteData in chartData.GetNoteDataList(binding.NoteType))
        {
            try
            {
                object spawned = spawnMethod.Invoke(binding.Factory, new object[] { noteData, positionCalculator });
                if (spawned is Component c)
                {
                    onSpawned?.Invoke(c.gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ChartGenerator] Spawn failed ({binding.NoteType}): {ex.Message}");
            }
        }
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
