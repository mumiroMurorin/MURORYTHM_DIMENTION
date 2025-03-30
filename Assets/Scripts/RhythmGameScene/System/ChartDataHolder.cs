using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartDataHolder : IChartDataSetter, IChartDataGetter
{
    // •ˆ–Êƒf[ƒ^
    ChartData chart;
    ChartData IChartDataGetter.Chart { get { return chart; } }

    void IChartDataSetter.SetChartData(ChartData data)
    {
        chart = data;
    }
}
