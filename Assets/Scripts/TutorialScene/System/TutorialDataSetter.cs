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
    [Expandable]
    [SerializeField] OptionAsset optionAsset;

    IMusicDataSetter musicDataSetter;
    IOptionSetter optionSetter;

    [Inject]
    public void Construct(IMusicDataSetter musicDataSetter, IOptionSetter optionSetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.optionSetter = optionSetter;
    }

    void Awake()
    {
        musicDataSetter?.SetMusicData(musicData_tutorial);
        musicDataSetter?.SetDifficulty(difficulty);

        optionSetter?.SetOption(optionAsset);
    }
}
