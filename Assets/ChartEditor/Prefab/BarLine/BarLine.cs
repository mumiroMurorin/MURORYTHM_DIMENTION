using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class BarLine : DeployableLineObject
    {
        [SerializeField] BarLineInfoView barInfo_view;
        [SerializeField] SubdivisionLineInfoView subInfo_view;
        
        int barNumber = 0;

        /// <summary>
        /// 小節線上のデータ更新
        /// </summary>
        /// <param name="barData"></param>
        private void SetBarLineData(IBarDataGetter barData, IBarDataGetter backData)
        {
            // 小節番号
            int barNumber = this.barNumber;
        }

        public override void OnChangeBpm(float bpm, float backBpm)
        {
            // 前データと同じbpmの時は表示しない
            bpm = bpm != backBpm ? bpm : -1;
            subInfo_view.SetBPM(bpm);
        }

        public override void OnChangeBeatCount(int beatCount, int backCount)
        {
            // 前データと同じbeatCountの時は表示しない
            beatCount = beatCount != backCount ? beatCount : -1;
            subInfo_view.SetBPM(beatCount);
        }

        public override void OnChangeBeatUnit(float beatUnit, float backUnit)
        {
            // 前データと同じbeatUnitの時は表示しない
            beatUnit = beatUnit != backUnit ? beatUnit : -1;
            subInfo_view.SetBPM(beatUnit);
        }

        public override void OnChangeDivisionNum(int divNum, int backNum)
        {
            // 前データと同じdivNumの時は表示しない
            divNum = divNum != backNum ? divNum : -1;
            subInfo_view.SetBPM(divNum);
        }
    }

    public interface IBarLineData : ISubdivisionLineData
    {
        IBarDataGetter BarData { get; }
    }

    public interface ILinePositioner
    {
        GameObject gameObject { get; }

        IReadOnlyReactiveProperty<float> NextZ { get; }

        ISubDivisionDataGetter SubDivisionData { get; }

        IBarDataGetter BarData { get; }
    }
}
