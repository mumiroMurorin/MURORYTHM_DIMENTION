using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/MusicDataList", fileName = "MusicDataList")]
public class MusicDataList : ScriptableObject
{
    [SerializeField] List<MusicData> musicDatas;

    public List<MusicData> MusicDatas { get { return musicDatas; } }
}
