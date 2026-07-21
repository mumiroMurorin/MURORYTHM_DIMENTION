using UnityEngine;
using VContainer;

public class TutorialStageLoader : StageLoaderBase
{
    [SerializeField] TutorialGuideCharacterDatabase tutorialGuideCharacterDatabase;

    IOptionGetter optionGetter;

    [Inject]
    public void Constructor(IMusicDataGetter musicDataGetter, IOptionGetter optionGetter)
    {
        SetMusicDataGetter(musicDataGetter);
        this.optionGetter = optionGetter;
    }

    private void Start()
    {
        LoadSelectedStage();
    }

    protected override bool TryGetStageType(out StageType stageType)
    {
        stageType = default;

        TutorialGuideCharacterType characterType = optionGetter != null
            ? optionGetter.CurrentTutorialGuideCharacterType.Value
            : TutorialGuideCharacterType.Shikiboo;

        TutorialGuideCharacterData data = tutorialGuideCharacterDatabase != null
            ? tutorialGuideCharacterDatabase.Get(characterType)
            : null;

        if (data == null)
        {
            Debug.LogError($"【System】チュートリアルキャラクターに対応するStageが見つかりません: {characterType}");
            return false;
        }

        stageType = data.StageType;
        return true;
    }
}