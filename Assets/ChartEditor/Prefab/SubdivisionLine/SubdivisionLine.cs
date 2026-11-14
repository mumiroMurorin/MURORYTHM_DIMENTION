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
            // 前の線と同じBPMのときは表示しない
            lineInfo_view.SetBPM(bpm, !Mathf.Approximately(bpm, backBpm));
        }

        public override void OnChangeSpeedRatio(float speedRatio, float backSpeedRatio)
        {
            // 前の線と同じスピード倍率のときは表示しない
            lineInfo_view.SetSpeedRatio(speedRatio, !Mathf.Approximately(speedRatio, backSpeedRatio));
        }
    }
}
