using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class DataSetterDebugInRhythmGameScene : MonoBehaviour
{
    [Header("デバッグモード")]
    [SerializeField] bool isDebugMode;
    [SerializeField] MusicData musicData_debug;
    [SerializeField] string chartFilePath;
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
#if UNITY_EDITOR
        if (!isDebugMode) { return; }
        if (musicDataSetter == null) { return; }

        if (musicDataGetter.Music.Value == null)
        {
            musicData_debug.SetChartPath(Difficulty.Initiate, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Fanatic, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Skyclad, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Dream, Application.dataPath + "/" + chartFilePath);
            musicDataSetter.SetMusicData(musicData_debug);
        }

        if (optionSetter == null) { return; }
        optionSetter.SetNoteSpeed(noteSpeed);
#endif
    }
}
