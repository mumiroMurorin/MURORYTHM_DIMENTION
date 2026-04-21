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
    [Space(20)]
    [SerializeField] Difficulty difficulty;
    [SerializeField] string chartFilePath;
    [SerializeField] float noteSpeed;
    [SerializeField] bool isAutoMode;
    [SerializeField] bool isFastLateEnabled;

    IMusicDataSetter musicDataSetter;
    IMusicDataGetter musicDataGetter;
    IOptionSetter optionSetter;
    IScoreSetter scoreSetter;
    INoteSpawnDataOptionSetter spawnDataSetter;

    [Inject]
    public void Construct(IScoreSetter scoreSetter, 
        IMusicDataGetter musicDataGetter, 
        IMusicDataSetter musicDataSetter, 
        IOptionSetter optionSetter,
        INoteSpawnDataOptionSetter spawnDataSetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.musicDataGetter = musicDataGetter;
        this.scoreSetter = scoreSetter;
        this.optionSetter = optionSetter;
        this.spawnDataSetter = spawnDataSetter;
    }

    void Awake()
    {
#if UNITY_EDITOR
        if (!isDebugMode) { return; }

        var dif = musicDataGetter.Difficulty.Value;
        if (musicDataGetter.Music.Value == null || musicDataGetter.Music.Value.GetChartPath(dif) == null || musicDataGetter.Music.Value.GetChartPath(dif) == "")
        {
            musicData_debug.SetChartPath(Difficulty.Easy, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Normal, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Hard, Application.dataPath + "/" + chartFilePath);
            musicData_debug.SetChartPath(Difficulty.Master, Application.dataPath + "/" + chartFilePath);
            musicDataSetter.SetMusicData(musicData_debug);
            musicDataSetter.SetDifficulty(difficulty);
        }

        spawnDataSetter.SetNoteSpeed(noteSpeed);
        spawnDataSetter.SetAutoMode(isAutoMode);
        optionSetter.SetIsEnabledFastLate(isFastLateEnabled);
        
#endif
    }
}
