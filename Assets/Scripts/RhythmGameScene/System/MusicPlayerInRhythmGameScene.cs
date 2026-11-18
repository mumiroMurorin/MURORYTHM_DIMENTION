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
    // 楽曲データの読み込みをこれ以上待たない時間 (秒)
    const float AUDIO_LOAD_TIMEOUT_SECONDS = 30f;

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

        MusicData musicData = musicDataGetter?.Music?.Value;
        if (musicData == null)
        {
            // 次へ進まずにエラーを通知する (元のシーンへ戻る)
            GameFlowError.Raise("楽曲が選ばれていないため、曲を読み込めません");
            return;
        }

        LoadMusicAsync(musicData.MusicClip, onEndLoading, cts.Token).Forget();
    }

    private async UniTask LoadMusicAsync(AudioClip clip, Action onEndLoading, CancellationToken token)
    {
        // 音源が無い (読み込みに失敗した) ときは、曲が鳴らず譜面も進まないため、次へ進まずにエラーを通知する
        if (clip == null)
        {
            GameFlowError.Raise("楽曲の音源がありません (音源ファイルの読み込みに失敗した可能性があります)");
            return;
        }

        // 楽曲の事前読み込み (失敗やタイムアウトもエラーにする)
        clip.LoadAudioData();

        float elapsedSeconds = 0f;
        while (clip.loadState != AudioDataLoadState.Loaded)
        {
            if (clip.loadState == AudioDataLoadState.Failed)
            {
                GameFlowError.Raise($"楽曲の音源を読み込めませんでした: {clip.name}");
                return;
            }

            if (elapsedSeconds >= AUDIO_LOAD_TIMEOUT_SECONDS)
            {
                GameFlowError.Raise($"楽曲の音源の読み込みが {AUDIO_LOAD_TIMEOUT_SECONDS} 秒以内に終わりませんでした: {clip.name}");
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
            elapsedSeconds += Time.unscaledDeltaTime;
        }

        SoundManager.Instance.SetBGM(clip, BGM_Type.MusicTrack);

        onEndLoading?.Invoke();
    }

    /// <summary>
    /// 楽曲の再生
    /// </summary>
    public void PlayMusic()
    {
        if (musicDataGetter == null) { Debug.LogWarning("【System】 musicDataGetterがセットされていません"); return; }
        if (musicDataGetter.Music.Value == null) { Debug.LogWarning("【System】 楽曲がセットされていません"); return; }

        SoundManager.Instance.PlayBGM(BGM_Type.MusicTrack, loopFlg: false, isFadeout: false);
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
