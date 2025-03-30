using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class MusicDataListLoader : MonoBehaviour, IMusicDataListLoader
{
    [SerializeField] MusicDataList musicDataList;

    ISelectSceneDataSetter selectSceneDataSetter;

    [Inject]
    public void Construct(ISelectSceneDataSetter selectSceneDataSetter)
    {
        this.selectSceneDataSetter = selectSceneDataSetter;
    }

    void IMusicDataListLoader.LoadMusicDataList()
    {
        selectSceneDataSetter.SetMusicList(musicDataList.MusicDatas);
    }
}

public interface IMusicDataListLoader
{
    void LoadMusicDataList();
}
