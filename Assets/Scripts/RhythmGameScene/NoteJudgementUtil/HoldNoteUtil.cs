using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace JudgementUtil.Hold
{
    static public class HoldJudgement
    {
        static public List<int> GetJudgeRange(List<TimeToRange> timeToRanges ,float currentTime)
        {
            // 時間外判定
            if (timeToRanges[0].Timing > currentTime) { return new List<int>(); }
            if (timeToRanges[^1].Timing < currentTime) { return timeToRanges[^1].Range.Select(x => (int)x).ToList(); }

            // 今ホールドノーツのどの時間を判定しているのか調べる
            TimeToRange former = new TimeToRange(0, new float[0]);
            TimeToRange latter = new TimeToRange(0, new float[0]);
            for (int i = 0; i < timeToRanges.Count; i++)
            {
                if (timeToRanges[i].Timing > currentTime) { continue; }
                if (timeToRanges[i + 1].Timing < currentTime) { continue; }

                former = timeToRanges[i];
                latter = timeToRanges[i + 1];
            }

            // 判定範囲の計算
            float t0 = former.Timing;
            float t1 = latter.Timing;
            float x0 = former.Range[0];
            float x1 = latter.Range[0];
            float t = currentTime;

            float startRange = x1 - x0 != 0 ?
                (t - t1) * (x1 - x0) / (t1 - t0) + x1 :
                former.Range[0];

            x0 = former.Range[^1];
            x1 = latter.Range[^1];

            float endRange = x1 - x0 != 0 ?
                (t - t1) * (x1 - x0) / (t1 - t0) + x1 :
                former.Range[^1];

            return Enumerable.Range((int)startRange, (int)Mathf.Ceil(endRange) - (int)startRange + 1).ToList() ?? new List<int>();

            //Debug.Log($"Range: {startRange} , {endRange}");
            //Debug.Log("judgeRange: " + string.Join(",", judgeRange.Select(n => n.ToString())));
        }
    }   
}