using UnityEngine;
using VContainer;
using System.Collections.Generic;
using UniRx;
using System.Linq;
using Tutorial;
using System.Threading;

public class TutorialController : MonoBehaviour, IDisposer
{
    [SerializeField] TutorialActionAsset defaultActionAsset;
    [SerializeField] TutorialGuideCharacterDatabase guideCharacterDatabase;
    [SerializeField] SpeechBubbleTutorial speechBubble;
    [SerializeField] TutorialSceneObjectReference[] sceneObjectReferences;
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    public System.Action OnFinishTutorialListener;

    IOptionGetter optionGetter;
    TutorialActionNode[] runtimeActions;

    [Inject]
    public void Construct(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // ゲームの最初だけ実行
        timer?.Value.TimeRP
            .Where(time => 0 < time)
            .Take(1)
            .Subscribe(_ => OnStartStage())
            .AddTo(this.gameObject);
    }

    private TutorialActionNode[] ResolveActions()
    {
        TutorialGuideCharacterData data = GetSelectedGuideCharacterData();
        if (data != null && data.TutorialActionAsset != null && data.TutorialActionAsset.Actions != null && data.TutorialActionAsset.Actions.Length > 0)
        {
            return data.TutorialActionAsset.Actions;
        }

        return defaultActionAsset?.Actions;
    }

    private void InitializeGuideCharacter()
    {
        TutorialGuideCharacterData data = GetSelectedGuideCharacterData();
        if (data == null) { return; }

        if (data.EmotionAsset != null)
        {
            speechBubble?.SetEmotionAsset(data.EmotionAsset);
        }
    }

    private TutorialGuideCharacterData GetSelectedGuideCharacterData()
    {
        TutorialGuideCharacterType characterType = optionGetter != null
            ? optionGetter.CurrentTutorialGuideCharacterType.Value
            : TutorialGuideCharacterType.Shikiboo;

        return guideCharacterDatabase?.Get(characterType);
    }

    private void InitializeActions(TutorialActionNode[] targetActions)
    {
        if (targetActions == null || targetActions.Length == 0) { return; }

        TutorialRuntimeContext context = new TutorialRuntimeContext(speechBubble, this, sceneObjectReferences);
        foreach (TutorialActionNode action in targetActions)
        {
            action?.Initialize(context);
        }

        for (int i = 0; i < targetActions.Length - 1; i++)
        {
            targetActions[i]?.SetNextNode(targetActions[i + 1]);
        }

        // 最後にコールバック関数を呼ぶ
        targetActions.Last()?.SetNextNode(new Callback(() => {
            OnFinishTutorialListener?.Invoke();
        }));
    }

    private void OnStartStage()
    {
        runtimeActions = ResolveActions();
        InitializeGuideCharacter();
        InitializeActions(runtimeActions);
        runtimeActions?[0]?.Do();
    }


    List<CancellationTokenSource> ctsList = new List<CancellationTokenSource>();

    public void SetCts(CancellationTokenSource cts)
    {
        ctsList.Add(cts);
    }

    private void OnDestroy()
    {
        foreach(var cts in ctsList)
        {
            cts?.CancelAndDispose();
        }
    }
}

public interface IDisposer
{
    void SetCts(CancellationTokenSource cts);
}