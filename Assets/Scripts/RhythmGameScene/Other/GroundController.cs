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
    /// ƒOƒ‰ƒEƒ“ƒh‚ð“®‚©‚·
    /// </summary>
    private void MoveGround(float time)
    {
        float z = chartDataGetter.Chart.PositionGraph.GetPosition(time);

        this.gameObject.transform.position = Vector3.back * optionHolder.NoteSpeed.Value * z;
    }
}
