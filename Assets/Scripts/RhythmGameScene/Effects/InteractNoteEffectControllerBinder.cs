using UniRx;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class InteractNoteEffectControllerBinder : MonoBehaviour
{
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;
    [SerializeField] Transform generatedControllerParent;

    IScoreGetter scoreGetter;
    IMusicDataGetter musicDataGetter;
    IObjectResolver resolver;
    InteractNoteEffectSpawnController effectController;
    bool initialized;

    [Inject]
    public virtual void Constructor(
        IScoreGetter scoreGetter,
        IMusicDataGetter musicDataGetter,
        IObjectResolver resolver)
    {
        this.scoreGetter = scoreGetter;
        this.musicDataGetter = musicDataGetter;
        this.resolver = resolver;
    }

    protected virtual void Start()
    {
        Initialize();
    }

    protected void Initialize()
    {
        if (initialized) { return; }
        initialized = true;

        effectController = CreateEffectController(GetCurrentMusicData());
        BindJudgement();
    }

    protected virtual MusicData GetCurrentMusicData()
    {
        return musicDataGetter?.Music?.Value;
    }

    private InteractNoteEffectSpawnController CreateEffectController(MusicData musicData)
    {
        if (musicData == null) { return null; }

        InteractNoteEffectSpawnController prefab = symphonyTypePresentationDatabase?.GetInteractNoteEffectControllerPrefab(musicData.SymphonyType);
        if (prefab == null)
        {
            Debug.LogWarning($"[InteractNoteEffectControllerBinder] Interact effect controller prefab is not set: {musicData.SymphonyType}");
            return null;
        }

        Transform parent = generatedControllerParent != null ? generatedControllerParent : transform;
        InteractNoteEffectSpawnController controller = resolver != null
            ? resolver.Instantiate(prefab, parent)
            : Instantiate(prefab, parent);

        controller.transform.localPosition = Vector3.zero;
        controller.transform.localRotation = Quaternion.identity;
        controller.transform.localScale = Vector3.one;
        return controller;
    }

    private void BindJudgement()
    {
        if (scoreGetter == null) { return; }
        if (effectController == null) { return; }

        scoreGetter.NoteJudgementDatas
            .ObserveAdd()
            .Subscribe(value => effectController.SpawnEffect(value.Value))
            .AddTo(this.gameObject);
    }
}
