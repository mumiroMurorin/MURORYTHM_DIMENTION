using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using NaughtyAttributes;

public class TutorialDataSetter : MonoBehaviour
{
    [SerializeField] MusicData musicData_tutorial;
    [Space(20)]
    [SerializeField] Difficulty difficulty;
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypeToData;
    [SerializeField] TutorialGuideCharacterDatabase tutorialGuideCharacterDatabase;
    [Expandable]
    [SerializeField] OptionAsset optionAsset;

    IMusicDataSetter musicDataSetter;
    IOptionSetter optionSetter;
    IOptionGetter optionGetter;

    [Inject]
    public void Construct(IMusicDataSetter musicDataSetter, IOptionSetter optionSetter, IOptionGetter optionGetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.optionSetter = optionSetter;
        this.optionGetter = optionGetter;
    }

    void Awake()
    {
        ApplySelectedTutorialSymphonyType();

        musicDataSetter?.SetMusicData(musicData_tutorial);
        musicDataSetter?.SetDifficulty(difficulty);

        optionSetter?.SetOption(optionAsset);
    }

    private void ApplySelectedTutorialSymphonyType()
    {
        if (musicData_tutorial == null) { return; }

        TutorialGuideCharacterType characterType = optionGetter != null && optionGetter.CurrentTutorialGuideCharacterType != null
            ? optionGetter.CurrentTutorialGuideCharacterType.Value
            : TutorialGuideCharacterType.Shikiboo;

        TutorialGuideCharacterData characterData = tutorialGuideCharacterDatabase != null
            ? tutorialGuideCharacterDatabase.Get(characterType)
            : null;

        musicData_tutorial.SymphonyType = characterData != null
            ? characterData.SymphonyType
            : ConvertToSymphonyType(characterType);
    }

    private SymphonyType ConvertToSymphonyType(TutorialGuideCharacterType characterType)
    {
        switch (characterType)
        {
            case TutorialGuideCharacterType.Creation:
                return SymphonyType.Creation;
            case TutorialGuideCharacterType.Destruction:
                return SymphonyType.Destruction;
            default:
                return SymphonyType.None;
        }
    }
}
