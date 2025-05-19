using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class MusicDataListLoaderDebug : MonoBehaviour, IMusicDataListLoader
{
    [SerializeField] MusicDataList musicDataList;

    ISelectSceneDataSetter selectSceneDataSetter;
    CancellationTokenSource cts;

    [Inject]
    public void Construct(ISelectSceneDataSetter selectSceneDataSetter)
    {
        this.selectSceneDataSetter = selectSceneDataSetter;
    }

    void IMusicDataListLoader.LoadMusicDataList(Action onFinishAction)
    {
        selectSceneDataSetter.SetMusicList(musicDataList.MusicDatas);

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        cts = new CancellationTokenSource();

        LoadAudioDatasAsync(onFinishAction, cts.Token).Forget();
    }

    /// <summary>
    /// 楽曲のロードを非同期で行う
    /// </summary>
    /// <param name="onEndAction"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask LoadAudioDatasAsync(Action onEndAction, CancellationToken token)
    {
        foreach (var data in musicDataList.MusicDatas)
        {
            if (data.SampleClip.loadState == AudioDataLoadState.Loaded) { continue; }

            data.SampleClip.LoadAudioData();
            await UniTask.WaitUntil(() => data.SampleClip.loadState == AudioDataLoadState.Loaded, cancellationToken: token);
        }

        onEndAction.Invoke();
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

public interface IMusicDataListLoader
{
    void LoadMusicDataList(Action onFinishAction);
}
