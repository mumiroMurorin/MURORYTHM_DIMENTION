using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class ChartDataHolder : IChartDataSetter, IChartDataGetter
    {
        // ïàñ ÉfÅ[É^
        ChartData chart;
        ChartData IChartDataGetter.Chart { get { return chart; } }

        void IChartDataSetter.SetChartData(ChartData data)
        {
            chart = data;
        }
    }

}