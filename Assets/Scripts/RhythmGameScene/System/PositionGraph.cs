using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSegment
{
    public float StartTime { get; }
    public float EndTime { get; set; }
    public float Length => EndTime - StartTime;


    public float Slope { get; }
    public float StartPosition { get; set; }


    public TimeSegment(float startTime, float slope)
    {
        StartTime = startTime;
        Slope = slope;
    }

    public bool Contains(float t)
    {
        return t >= StartTime && t < EndTime;
    }

    public float PositionAt(float t)
    {
        return StartPosition + (t - StartTime) * Slope;
    }
}

public class PositionGraph : INotePositionCalculator
{
    private readonly List<TimeSegment> segments = new();

    // 区間を追加したあと、まだ並べ直し・累積距離の再計算をしていないとき true
    private bool isDirty;

    /// <summary>
    /// 登録済みの区間 (時系列順)
    /// </summary>
    public IReadOnlyList<TimeSegment> Segments
    {
        get
        {
            RebuildIfDirty();
            return segments;
        }
    }

    /// <summary>
    /// 時間区間と傾きを登録（呼び出し順不問）
    /// </summary>
    /// <param name="startTime">スタート秒</param>
    /// <param name="slope">傾き</param>
    public void AddSegment(float startTime, float slope)
    {
        segments.Add(new TimeSegment(startTime, slope));

        // 追加のたびに並べ直すと、区間数の2乗に比例して重くなる(分線ごとに1区間ある)
        // 実際に位置を求めるときにまとめて1回だけ並べ直す
        isDirty = true;
    }

    private void RebuildIfDirty()
    {
        if (!isDirty) { return; }

        isDirty = false;
        Rebuild();
    }

    /// <summary>
    /// 区間を時系列順に並べ、累積距離を再計算
    /// </summary>
    private void Rebuild()
    {
        // 時間順にソート
        segments.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));

        float pos = 0f;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            segment.StartPosition = pos;
            segment.EndTime = i != segments.Count - 1 ? segments[i + 1].StartTime : float.MaxValue;
            pos += segment.Length * segment.Slope;
        }
    }

    /// <summary>
    /// 時刻tの位置(z)を返す
    /// </summary>
    public float GetPosition(float t)
    {
        RebuildIfDirty();

        // 最初の区間以前
        if (segments.Count > 0 && t < segments[0].StartTime)
        {
            var last = segments[0];
            return last.StartPosition - (last.StartTime - t) * last.Slope;
        }

        // tを含む区間を二分探索で探す (開始時刻がt以下の区間のうち、最後のもの)
        // 先頭から順に探すのと同じ区間が見つかる
        int low = 0;
        int high = segments.Count - 1;
        int found = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (segments[middle].StartTime <= t)
            {
                found = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (found >= 0 && segments[found].Contains(t))
        {
            return segments[found].PositionAt(t);
        }

        // 最後の区間以降
        if (segments.Count > 0 && t >= segments[^1].EndTime)
        {
            var last = segments[^1];
            return last.StartPosition + (t - last.StartTime) * last.Slope;
        }

        return 0f;
    }
}

/// <summary>
/// オプションのディスプレイ等に使用
/// </summary>
public class PositionGraphOption : INotePositionCalculator
{
    /// <summary>
    /// 常に一次関数 y = x を返す
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public float GetPosition(float t)
    {
        return t;
    }
}

public interface INotePositionCalculator
{
    float GetPosition(float t);
}