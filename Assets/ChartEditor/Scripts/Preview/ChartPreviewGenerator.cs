using System;
using System.Reflection;
using Deform;
using UnityEngine;

namespace ChartEditor
{
    public class ChartPreviewGenerator : MonoBehaviour, IChartGenerator, IChartDestroyer
    {
        [Serializable]
        private class NoteFactoryBinding
        {
            [SerializeField] private global::NoteType noteType;
            [SerializeField] private MonoBehaviour factory;

            public global::NoteType NoteType => noteType;
            public MonoBehaviour Factory => factory;
        }

        [Header("Spawn/Destroy Target")]
        [SerializeField] private Transform previewRoot;

        [Header("Note Factories")]
        [SerializeField] private NoteFactoryBinding[] noteFactories;

        [Header("Factory Initialization")]
        [SerializeField] private GameObject groundObject;
        [SerializeField] private Deformer groundDeformer;
        [SerializeField] private SerializeInterface<INoteSpawnDataOptionHolder> spawnDataOptionHolder;
        [SerializeField] private SerializeInterface<ISliderInputGetter> sliderInputGetter;
        [SerializeField] private SerializeInterface<ISpaceInputGetter> spaceInputGetter;
        [SerializeField] private SerializeInterface<ITimeGetter> timer;

        private global::ChartData chartData;

        private void Awake()
        {
            InitializeFactories();
        }

        public void SetChartData(global::ChartData chartData)
        {
            this.chartData = chartData;
        }

        public void Generate(Action callback = null)
        {
            if (chartData == null)
            {
                Debug.LogWarning("[ChartPreviewGenerator] Chart data is null.");
                return;
            }

            INotePositionCalculator positionCalculator = new PositionGraphOption();
            if (noteFactories == null)
            {
                callback?.Invoke();
                return;
            }

            foreach (var binding in noteFactories)
            {
                SpawnEachType(binding, chartData, positionCalculator, OnSpawned);
            }

            callback?.Invoke();
        }

        public void DestroyChart()
        {
            if (previewRoot == null) { return; }

            for (int i = previewRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(previewRoot.GetChild(i).gameObject);
            }
        }

        private void InitializeFactories()
        {
            if (spawnDataOptionHolder == null || timer == null || timer.Value == null)
            {
                Debug.LogWarning("[ChartPreviewGenerator] Factory initialization skipped due to missing references.");
                return;
            }

            NoteFactoryInitializingData data = new NoteFactoryInitializingData
            {
                GroundObject = groundObject,
                GroundDeformer = groundDeformer,
                OptionHolder = spawnDataOptionHolder.Value,
                SliderInputGetter = sliderInputGetter != null ? sliderInputGetter.Value : null,
                SpaceInputGetter = spaceInputGetter != null ? spaceInputGetter.Value : null,
                Timer = timer.Value,
                JudgementRecorder = null
            };

            if (noteFactories == null) { return; }
            foreach (var binding in noteFactories)
            {
                InitializeFactory(binding, data);
            }
        }

        private void OnSpawned(GameObject noteObject)
        {
            if (noteObject == null || previewRoot == null) { return; }
            noteObject.transform.SetParent(previewRoot, true);
        }

        private static void InitializeFactory(NoteFactoryBinding binding, NoteFactoryInitializingData data)
        {
            if (binding == null || binding.Factory == null) { return; }

            MethodInfo initializeMethod = binding.Factory.GetType().GetMethod("Initialize", new[] { typeof(NoteFactoryInitializingData) });
            if (initializeMethod == null)
            {
                Debug.LogWarning($"[ChartPreviewGenerator] Initialize method not found: {binding.Factory.GetType().Name}");
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
                Debug.LogWarning($"[ChartPreviewGenerator] Spawn method not found: {binding.Factory.GetType().Name}");
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
                    Debug.LogWarning($"[ChartPreviewGenerator] Spawn failed ({binding.NoteType}): {ex.Message}");
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
}
