using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class SubdivisionLine : DeployableLineObject
    {
        [SerializeField] SubdivisionLineInfoView lineInfo_view;

        public override void OnChangeBeatCount(int beatCount, int backCount) { }

        public override void OnChangeBeatUnit(float beatUnit, float backUnit) {  }
        
        public override void OnChangeDivisionNum(int divNum, int backNum) { }

        public override void OnChangeBpm(float bpm, float backBpm)
        {
            // 前データと同じbpmの時は表示しない
            bpm = bpm != backBpm ? bpm : -1;
            lineInfo_view.SetBPM(bpm);
        }
    }
}
