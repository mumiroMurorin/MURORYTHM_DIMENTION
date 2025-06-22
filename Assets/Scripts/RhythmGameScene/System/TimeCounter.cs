using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class TimeCounter : MonoBehaviour, ITimeGetter, ITimeController
{
    [SerializeField] float firstIntervalSeconds = 2f;

    bool isCounting;

    private ReactiveProperty<float> time = new ReactiveProperty<float>();
    public float Time { get { return time.Value; } }
    public IReadOnlyReactiveProperty<float> TimeRP => time;

    public void ResetTimer()
    {
        time.Value = -firstIntervalSeconds;
        isCounting = false;
    }

    public void StartTimer()
    {
        //time = -firstIntervalSeconds;
        isCounting = true;
    }

    public void StopTimer()
    {
        isCounting = false;
    }

    private void FixedUpdate()
    {
        if (isCounting) 
        {
            time.Value += UnityEngine.Time.fixedDeltaTime;
        }
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

