using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class MusicDataListController : MonoBehaviour
{
    [SerializeField] SerializeInterface<TransitionerInSelectScene.IPhaseTransitionableInSelectScene> transitioner;
    [SerializeField] SerializeInterface<TransitionerInSelectScene.IPhaseStatusGetterInSelectScene> phaseGetter;

    IMusicDataListSetter musicDataListSetter;
    IMusicDataListGetter musicDataListGetter;

    public IMusicDataListGetter Getter { get { return musicDataListGetter; } }

    [Inject]
    public void Construct(IMusicDataListSetter musicDataListSetter, IMusicDataListGetter musicDataListGetter)
    {
        this.musicDataListSetter = musicDataListSetter;
        this.musicDataListGetter = musicDataListGetter;
    }

    /// <summary>
    /// MusicTopicの移動
    /// </summary>
    /// <param name="index"></param>
    public void MoveMusicTopic(int delta)
    {
        musicDataListSetter.SetMusicIndex(musicDataListGetter.CurrentMusicIndex.Value + delta);
    }

    /// <summary>
    /// 難易度の変更
    /// </summary>
    /// <param name="diff"></param>
    public void ChangeDifficulty(int delta)
    {
        musicDataListSetter.SetDifficulty(musicDataListGetter.Difficulty.Value + delta);
    }

    /// <summary>
    /// 現在選択されている楽曲、難易度が遊べるか返す
    /// </summary>
    /// <returns></returns>
    public bool IsPlayableMusicOnCurrentSelecting()
    {
        var diff = musicDataListGetter.Difficulty.Value;
        return musicDataListGetter.CurrentMusicData.Value.GetDifficulty(diff) != -1;
    }
}
