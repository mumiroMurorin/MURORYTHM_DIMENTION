using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class MusicDataListController : MonoBehaviour
{
    IMusicDataListSetter musicDataListSetter;
    IMusicDataListGetter musicDataListGetter;

    [Inject]
    public void Construct(IMusicDataListSetter musicDataListSetter, IMusicDataListGetter musicDataListGetter)
    {
        this.musicDataListSetter = musicDataListSetter;
        this.musicDataListGetter = musicDataListGetter;
    }

    /// <summary>
    /// MusicTopic‚ÌˆÚ“®
    /// </summary>
    /// <param name="index"></param>
    public void MoveMusicTopic(int delta)
    {
        musicDataListSetter.SetMusicIndex(musicDataListGetter.CurrentMusicIndex.Value + delta);
    }

    /// <summary>
    /// “ïˆÕ“x‚Ì•ÏX
    /// </summary>
    /// <param name="diff"></param>
    public void ChangeDifficulty(int delta)
    {
        musicDataListSetter.SetDifficulty(musicDataListGetter.Difficulty.Value + delta);
    }
}
