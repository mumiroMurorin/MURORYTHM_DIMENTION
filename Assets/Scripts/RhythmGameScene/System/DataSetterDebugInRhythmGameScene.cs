using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class DataSetterDebugInRhythmGameScene : MonoBehaviour
{
    [Header("デバッグモード(リリース時は必ずオフに)")]
    [SerializeField] bool isDebugMode;
    [SerializeField] MusicData musicData_debug;
    [SerializeField] float noteSpeed;

    IMusicDataSetter musicDataSetter;
    IMusicDataGetter musicDataGetter;
    IOptionSetter optionSetter;
    IScoreSetter scoreSetter;

    [Inject]
    public void Construct(IScoreSetter scoreSetter, IMusicDataGetter musicDataGetter, IMusicDataSetter musicDataSetter, IOptionSetter optionSetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.musicDataGetter = musicDataGetter;
        this.scoreSetter = scoreSetter;
        this.optionSetter = optionSetter;
    }

    void Awake()
    {
        if (!isDebugMode) { return; }

        if (musicDataSetter == null) { return; }
        if (musicDataGetter.Music.Value == null) { musicDataSetter.SetMusicData(musicData_debug); }

        if(optionSetter == null) { return; }
        optionSetter.SetNoteSpeed(noteSpeed);
    }
}
