using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class TimeCounter : MonoBehaviour, ITimeGetter, ITimeController
{
    [SerializeField] float firstIntervalSeconds = 2f;
    [SerializeField] bool syncWithMusic = true;
    [SerializeField] BGM_Type syncBgmType = BGM_Type.MusicTrack;
    [SerializeField] float maxCountdownDeltaTime = 0.05f;

    bool isCounting;
    bool hasSyncedWithMusic;

    private ReactiveProperty<float> time = new ReactiveProperty<float>();
    public float Time { get { return time.Value; } }
    public IReadOnlyReactiveProperty<float> TimeRP => time;

    public void ResetTimer()
    {
        time.Value = -firstIntervalSeconds;
        isCounting = false;
        hasSyncedWithMusic = false;
    }

    public void StartTimer()
    {
        isCounting = true;
    }

    public void StopTimer()
    {
        isCounting = false;
    }

    private void Update()
    {
        if (!isCounting) { return; }

        // 楽曲開始前だけ内部カウントで進める
        if (time.Value < 0f)
        {
            AdvanceInternalTime(true);
            return;
        }

        // 楽曲開始後はAudioSourceの再生位置を正とする
        if (syncWithMusic &&
            SoundManager.TryGetInstance(out SoundManager soundManager) &&
            soundManager.TryGetBGMPlaybackSeconds(syncBgmType, out float musicTime))
        {
            hasSyncedWithMusic = true;
            time.Value = Mathf.Max(time.Value, musicTime);
            return;
        }

        // 楽曲終了後は最後の再生位置から内部カウントで進め続ける
        if (!syncWithMusic || hasSyncedWithMusic)
        {
            AdvanceInternalTime(false);
        }
    }

    private void AdvanceInternalTime(bool clampToZero)
    {
        float deltaTime = Mathf.Min(UnityEngine.Time.deltaTime, maxCountdownDeltaTime);
        float nextTime = time.Value + deltaTime;
        time.Value = clampToZero ? Mathf.Min(nextTime, 0f) : nextTime;
    }
}

/// <summary>
/// タイマースタート、ストップする
/// </summary>
public interface ITimeController
{
    void StartTimer();

    void StopTimer();

    void ResetTimer();
}

public interface ITimeGetter
{
    float Time { get; }

    IReadOnlyReactiveProperty<float> TimeRP { get; }
}

