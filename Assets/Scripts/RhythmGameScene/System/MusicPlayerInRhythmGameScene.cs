using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class MusicPlayerInRhythmGameScene : MonoBehaviour, IMusicPlayerInRhythmGameScene
{
    IMusicDataGetter musicDataGetter;

    [Inject]
    public void Construct(IMusicDataGetter musicDataGetter)
    {
        this.musicDataGetter = musicDataGetter;
    }

    /// <summary>
    /// 楽曲の再生
    /// </summary>
    void IMusicPlayerInRhythmGameScene.PlayMusic()
    {
        if (musicDataGetter == null) { Debug.LogWarning("【System】 musicDataGetterがセットされていません"); return; }
        if (musicDataGetter.Music.Value == null) { Debug.LogWarning("【System】 楽曲がセットされていません"); return; }
        SoundManager.Instance.PlayBGM(musicDataGetter.Music.Value.MusicClip, loopFlg: false, isFadeout: false);
    }
}

public interface IMusicPlayerInRhythmGameScene
{
    void PlayMusic();
}