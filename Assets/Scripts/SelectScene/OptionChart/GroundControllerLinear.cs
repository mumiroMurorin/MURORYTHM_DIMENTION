using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class GroundControllerLinear : MonoBehaviour
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    INoteSpawnDataOptionGetter optionHolder;

    [Inject]
    public void Constructor(INoteSpawnDataOptionGetter optionHolder)
    {
        this.optionHolder = optionHolder;
    }

    public void Initialize()
    {
        Bind();
    }

    private void Bind()
    {
        if (timer == null) { return; }
        if (timer.Value == null) { return; }

        timer.Value.TimeRP
            .Subscribe(MoveGround)
            .AddTo(this.gameObject);
    }

    /// <summary>
    /// グラウンドを動かす
    /// </summary>
    private void MoveGround(float time)
    {
        // 選曲画面のプレビューも本編と同じ円軌道で進行させる
        float distance = optionHolder.NoteSpeed.Value * time;
        NoteTrackCurve.SetProgress(this.gameObject.transform, distance, optionHolder.NoteCurveRadius.Value);
    }
}
