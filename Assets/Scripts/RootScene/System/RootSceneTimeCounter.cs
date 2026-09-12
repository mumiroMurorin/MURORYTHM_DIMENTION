using UniRx;
using UnityEngine;

public class RootSceneTimeCounter : MonoBehaviour, ITimeGetter, ITimeController
{
    readonly ReactiveProperty<float> time = new ReactiveProperty<float>();

    float elapsedTime;
    bool isCounting;

    public float Time => time.Value;
    public IReadOnlyReactiveProperty<float> TimeRP => time;

    public void StartTimer()
    {
        isCounting = true;
    }

    public void StopTimer()
    {
        isCounting = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        time.Value = 0f;
        isCounting = false;
    }

    void Update()
    {
        if (!isCounting) { return; }

        elapsedTime += UnityEngine.Time.unscaledDeltaTime;
        time.Value = elapsedTime;
    }
}
