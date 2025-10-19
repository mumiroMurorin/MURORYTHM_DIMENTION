using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;

public class DataSetterDebugInRhythmGameScene : MonoBehaviour
{
    [Header("デバッグモード")]
    [SerializeField] bool isDebugMode;

    [SerializeField] MusicData musicData_debug;
    [SerializeField] string chartFilePath;
    [SerializeField] float noteSpeed;
    [SerializeField] bool isAutoMode;
    [SerializeField] bool isFastLateEnabled;

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

        Difficulty difficulty = musicDataGetter.Difficulty.Value;

        if (musicDataGetter.Music.Value == null || musicDataGetter.Music.Value.GetChartPath(difficulty) == null || musicDataGetter.Music.Value.GetChartPath(difficulty) == "")
        {
            musicData_debug.SetChartPath(Difficulty.Easy, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Normal, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Hard, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Master, Application.dataPath + "/" + chartFilePath);
            musicDataSetter.SetMusicData(musicData_debug);
        }

        optionSetter.SetNoteSpeed(noteSpeed);
        optionSetter.SetAutoMode(isAutoMode);
        optionSetter.SetIsEnabledFastLate(isFastLateEnabled);
#endif
    }
}
