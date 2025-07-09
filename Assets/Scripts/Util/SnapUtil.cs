using System;
using System.Collections.Generic;
using System.Linq;

public static class SnapUtil
{
    /// <summary>
    /// 与えられたリストを参照リスト内の最も近い値に変換する
    /// </summary>
    public static List<float> SnapToNearest(this List<float> source, List<float> reference)
    {
        return source.Select(x =>
            reference.OrderBy(r => Math.Abs(r - x)).First()
        ).ToList();
    }
}
