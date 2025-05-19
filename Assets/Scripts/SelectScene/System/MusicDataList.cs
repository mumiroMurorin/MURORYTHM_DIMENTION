using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MusicDataList
{
    [SerializeField] List<MusicData> musicDatas = new List<MusicData>();

    public List<MusicData> MusicDatas { get { return musicDatas; } }
}
