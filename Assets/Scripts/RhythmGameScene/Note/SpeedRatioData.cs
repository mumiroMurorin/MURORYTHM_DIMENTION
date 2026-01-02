using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
/// <summary>
/// スピード倍率(ソフラン)データ
/// 譜面データ(ChartData)に組み込まれる
/// </summary>
public class SpeedRatioData
{
    public SpeedRatioData(float ratio, float timing)
    {
        this.Ratio = ratio;
        this.Timing = timing;
    }

    public float Ratio { get; set; }

    public float Timing { get; set; }
}
