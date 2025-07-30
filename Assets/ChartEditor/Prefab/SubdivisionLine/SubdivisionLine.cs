using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class SubdivisionLine : DeployableLineObject
    {
        [SerializeField] SubdivisionLineInfoView lineInfo_view;

        public override void OnChangeBpm(float bpm, float backBpm)
        {
            //// 前データと同じbpmの時は表示しない
            //bpm = !Mathf.Approximately(bpm, backBpm) ? bpm : -1;
            lineInfo_view.SetBPM(bpm);
        }
    }
}
