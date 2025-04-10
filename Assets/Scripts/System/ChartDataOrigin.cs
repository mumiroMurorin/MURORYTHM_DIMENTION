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
        // M/N拍子のM
        public int BeatCount { get; set; }

        // M/N拍子のN
        public float BeatUnit { get; set; }

        // この小節の分割数
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
        // おなじみBPM
        public float Bpm { get; set; }

        // -------------- この分節に配置されているノーツリスト ------------------

        public List<NoteDataOrigin_Touch> TouchNoteData { get; set; }
        public List<NoteDataOrigin_DynamicUpward> DynamicUpwardData { get; set; }
    }

    #region NoteDataOriginClass ノーツデータクラス

    public class NoteDataOrigin_Touch
    {
        public int[] Range { get; set; }
    }

    public class NoteDataOrigin_DynamicUpward
    {
        public int[] Range { get; set; }
    }

    #endregion

}