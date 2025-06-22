using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using UniRx;

public class MusicPlayerInRhythmGameScene : MonoBehaviour, IMusicPlayerInRhythmGameScene
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    IMusicDataGetter musicDataGetter;
    CancellationTokenSource cts;
    CompositeDisposable disposable = new CompositeDisposable();

    [Inject]
    public void Construct(IMusicDataGetter musicDataGetter)
    {
        this.musicDataGetter = musicDataGetter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        disposable = new CompositeDisposable();

        timer?.Value.TimeRP
            .Skip(1)
            .Where(time => time >= 0)
            .Subscribe(_ => {
                PlayMusic();
                disposable.Dispose();
                disposable.Clear();
            })
            .AddTo(disposable)
            .AddTo(this.gameObject);
    }

    void IMusicPlayerInRhythmGameScene.LoadMusic(Action onEndLoading)
    {
        cts = new CancellationTokenSource();

        LoadMusicAsync(musicDataGetter.Music.Value.MusicClip, onEndLoading, cts.Token).Forget();
    }

    private async UniTask LoadMusicAsync(AudioClip clip, Action onEndLoading, CancellationToken token)
    {
        clip.LoadAudioData();
        await UniTask.WaitUntil(() => clip.loadState == AudioDataLoadState.Loaded, cancellationToken: token);

        SoundManager.Instance.SetBGM(clip, BGM_Type.MusicTrack);

        onEndLoading.Invoke();
    }

    /// <summary>
    /// 楽曲の再生
    /// </summary>
    public void PlayMusic()
    {
        if (musicDataGetter == null) { Debug.LogWarning("【System】 musicDataGetterがセットされていません"); return; }
        if (musicDataGetter.Music.Value == null) { Debug.LogWarning("【System】 楽曲がセットされていません"); return; }

        SoundManager.Instance.PlayBGM(BGM_Type.MusicTrack);
    }

    private void OnDestroy()
    {
        if(cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
}

public interface IMusicPlayerInRhythmGameScene
{
    void LoadMusic(Action onEndLoading);

    void PlayMusic();
}