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
        
        /// <summary>
        /// 小節線上のデータ更新
        /// </summary>
        /// <param name="barData"></param>
        public override void SetBarNumber(int barNumber)
        {
            barInfo_view.SetBarNumber(barNumber);
        }

        public override void OnChangeBpm(float bpm, float backBpm)
        {
            //// 前データと同じbpmの時は表示しない
            //bpm = bpm != backBpm ? bpm : -1;
            subInfo_view.SetBPM(bpm);
        }

        public override void OnChangeBeatCount(int beatCount, int backCount)
        {
            //// 前データと同じbeatCountの時は表示しない
            //beatCount = beatCount != backCount ? beatCount : -1;
            barInfo_view.SetBeatCount(beatCount);
        }

        public override void OnChangeBeatUnit(float beatUnit, float backUnit)
        {
            //// 前データと同じbeatUnitの時は表示しない
            //beatUnit = beatUnit != backUnit ? beatUnit : -1;
            barInfo_view.SetBeatUnit(beatUnit);
        }

        public override void OnChangeDivisionNum(int divNum, int backNum)
        {
            //// 前データと同じdivNumの時は表示しない
            //divNum = divNum != backNum ? divNum : -1;
            barInfo_view.SetDivisionNum(divNum);
        }
    }

    public interface ILinePositioner
    {
        GameObject gameObject { get; }

        IReadOnlyReactiveProperty<float> NextZ { get; }

        ISubDivisionDataGetter SubDivisionData { get; }

        IBarDataGetter BarData { get; }
    }
}
