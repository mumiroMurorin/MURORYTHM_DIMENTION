using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class GroundControllerPreview : MonoBehaviour
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;
    
    INoteSpawnDataOptionGetter optionGetter;

    [Inject]
    public void Constructor(INoteSpawnDataOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    ChartData chartData;

    public void Start()
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

    public void SetChartData(ChartData chartData)
    {
        this.chartData = chartData;
    }

    /// <summary>
    /// ƒOƒ‰ƒEƒ“ƒh‚ð“®‚©‚·
    /// </summary>
    private void MoveGround(float time)
    {
        if (chartData == null) { return; }

        float z = chartData.PositionGraph.GetPosition(time);

        this.gameObject.transform.position = new Vector3(
            this.transform.position.x, 
            this.transform.position.y,
            -optionGetter.NoteSpeed.Value * z);
    }
}
