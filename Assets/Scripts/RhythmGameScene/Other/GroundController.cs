using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class GroundController : MonoBehaviour
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    INoteSpawnDataOptionGetter optionHolder;
    IChartDataGetter chartDataGetter;

    [Inject]
    public void Constructor(INoteSpawnDataOptionGetter optionHolder, IChartDataGetter chartDataGetter)
    {
        this.optionHolder = optionHolder;
        this.chartDataGetter = chartDataGetter;
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
        float z = chartDataGetter.Chart.PositionGraph.GetPosition(time);

        // 【ノーツ軌道】全ノーツを個別更新せず、親を円の中心まわりに回転させる
        float distance = optionHolder.NoteSpeed.Value * z;
        NoteTrackCurve.SetProgress(this.gameObject.transform, distance, optionHolder.NoteCurveRadius.Value);
    }
}
