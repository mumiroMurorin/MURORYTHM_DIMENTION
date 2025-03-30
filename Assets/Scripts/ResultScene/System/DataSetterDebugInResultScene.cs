using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class DataSetterDebugInResultScene : MonoBehaviour
{
    [SerializeField] MusicData musicData_debug;

    IMusicDataSetter musicDataSetter;
    IMusicDataGetter musicDataGetter;
    IScoreSetter scoreSetter;

    [Inject]
    public void Construct(IScoreSetter scoreSetter, IMusicDataSetter musicDataSetter, IMusicDataGetter musicDataGetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.musicDataGetter = musicDataGetter;
        this.scoreSetter = scoreSetter;
    }

    void Awake()
    {
        if (musicDataSetter == null) { return; }
        if (musicDataGetter == null) { return; }
        if (musicDataGetter.Music.Value != null) { return; }

        musicDataSetter.SetMusicData(musicData_debug);
    }
}
