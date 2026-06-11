using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartConvert
{
    /// <summary>
    /// 譜面ファイルのもととなるクラス
    /// </summary>
    public class ChartDataOrigin
    {
        /// <summary>
        /// 小節データをまとめて一つの譜面とする
        /// </summary>
        public List<BarDataOrigin> BarDatas { get; set; }

        /// <summary>
        /// オフセット[ms]
        /// </summary>
        public float OffsetMs { get; set; }
    }

    /// <summary>
    /// 小節データ
    /// </summary>
    public class BarDataOrigin
    {
        public int BeatCount { get; set; }

        public float BeatUnit { get; set; }

        public int DivisionNum { get; set; }

        /// <summary>
        /// 分節データを纏めて小節とする
        /// </summary>
        public List<SubDivisionDataOrigin> SubDivisionDatas { get; set; }
    }

    /// <summary>
    /// 分節データ
    /// </summary>
    public class SubDivisionDataOrigin
    {
        public float Bpm { get; set; }

        public float SpeedRatio { get; set; }

        public List<NoteDataOrigin_Touch> TouchNoteData { get; set; }
        public List<NoteDataOrigin_DivineTouch> DivineTouchData { get; set; }

        public List<NoteDataOrigin_DynamicUpward> DynamicUpwardData { get; set; }
        public List<NoteDataOrigin_DynamicDownward> DynamicDownwardData { get; set; }
        public List<NoteDataOrigin_DynamicRightward> DynamicRightwardData { get; set; }
        public List<NoteDataOrigin_DynamicLeftward> DynamicLeftwardData { get; set; }

        public List<NoteDataOrigin_HoldStart> HoldStartData { get; set; }
        public List<NoteDataOrigin_DivineHoldStart> DivineHoldStartData { get; set; }
        public List<NoteDataOrigin_HoldEnd> HoldEndData { get; set; }
        public List<NoteDataOrigin_HoldEndUnjudge> HoldEndUnjudgeData { get; set; }
        public List<NoteDataOrigin_HoldRelay> HoldRelayData { get; set; }
        public List<NoteDataOrigin_HoldMeshRelay> HoldMeshRelayData { get; set; }

        public List<NoteDataOrigin_SpaceHoldStart> SpaceHoldStartData { get; set; }
        public List<NoteDataOrigin_SpaceHoldRelay> SpaceHoldRelayData { get; set; }
        public List<NoteDataOrigin_SpaceHoldMeshRelay> SpaceHoldMeshRelayData { get; set; }
        public List<NoteDataOrigin_SpaceHoldEnd> SpaceHoldEndData { get; set; }

        public List<NoteDataOrigin_SpaceBreak> SpaceBreakData { get; set; }
    }

    public class NoteDataOrigin_Touch
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DivineTouch
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DynamicUpward
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DynamicDownward
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DynamicRightward
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DynamicLeftward
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_HoldStart
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DivineHoldStart
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_HoldEnd
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_HoldEndUnjudge
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_HoldRelay
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_HoldMeshRelay
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_SpaceHoldStart
    {
        public int HoldNumber { get; set; }

        public SimpleVector2[] Vertices { get; set; }
    }

    public class NoteDataOrigin_SpaceHoldRelay
    {
        public int HoldNumber { get; set; }

        public SimpleVector2[] Vertices { get; set; }
    }

    public class NoteDataOrigin_SpaceHoldMeshRelay
    {
        public int HoldNumber { get; set; }

        public SimpleVector2[] Vertices { get; set; }
    }

    public class NoteDataOrigin_SpaceHoldEnd
    {
        public int HoldNumber { get; set; }

        public SimpleVector2[] Vertices { get; set; }
    }

    public class NoteDataOrigin_SpaceBreak
    {
        public SimpleVector2[] Vertices { get; set; }
    }
}
