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

        // タッチノーツ
        public List<NoteDataOrigin_Touch> TouchNoteData { get; set; }
        
        // ダイナミックノーツ 
        public List<NoteDataOrigin_DynamicUpward> DynamicUpwardData { get; set; }
        public List<NoteDataOrigin_DynamicDownward> DynamicDownwardData { get; set; }
        public List<NoteDataOrigin_DynamicRightward> DynamicRightwardData { get; set; }
        public List<NoteDataOrigin_DynamicLeftward> DynamicLeftwardData { get; set; }

        // ホールドノーツ
        public List<NoteDataOrigin_HoldStart> HoldStartData { get; set; }
        public List<NoteDataOrigin_HoldEnd> HoldEndData { get; set; }
        public List<NoteDataOrigin_HoldRelay> HoldRelayData { get; set; }
        public List<NoteDataOrigin_HoldMeshRelay> HoldMeshRelayData { get; set; }
        public List<NoteDataOrigin_HoldHiddenJudgedRelay> HoldHiddenJudgedRelayData { get; set; }

        // スペースホールドノーツ
        public List<NoteDataOrigin_SpaceHoldStart> SpaceHoldStartData { get; set; }
        public List<NoteDataOrigin_SpaceHoldRelay> SpaceHoldRelayData { get; set; }
        public List<NoteDataOrigin_SpaceHoldMeshRelay> SpaceHoldMeshRelayData { get; set; }
        public List<NoteDataOrigin_SpaceHoldHiddenJudgedRelay> SpaceHoldHiddenJudgedRelayData { get; set; }
        public List<NoteDataOrigin_SpaceHoldEnd> SpaceHoldEndData { get; set; }

    }

    #region NoteDataOriginClass ノーツデータクラス

    /// <summary>
    /// タッチノーツデータ
    /// </summary>
    public class NoteDataOrigin_Touch
    {
        public int[] Range { get; set; }
    }

    /// <summary>
    /// 上ダイナミックノーツデータ
    /// </summary>
    public class NoteDataOrigin_DynamicUpward
    {
        public int[] Range { get; set; }
    }

    /// <summary>
    /// 下ダイナミックノーツデータ
    /// </summary>
    public class NoteDataOrigin_DynamicDownward
    {
        public int[] Range { get; set; }
    }

    /// <summary>
    /// 右ダイナミックノーツデータ
    /// </summary>
    public class NoteDataOrigin_DynamicRightward
    {
        public int[] Range { get; set; }
    }

    /// <summary>
    /// 左ダイナミックノーツデータ
    /// </summary>
    public class NoteDataOrigin_DynamicLeftward
    {
        public int[] Range { get; set; }
    }

    /// <summary>
    /// ホールド始点データ
    /// </summary>
    public class NoteDataOrigin_HoldStart
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    /// <summary>
    /// ホールド終点データ
    /// </summary>
    public class NoteDataOrigin_HoldEnd
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    /// <summary>
    /// ホールド中継点データ
    /// </summary>
    public class NoteDataOrigin_HoldRelay
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    /// <summary>
    /// ホールドメッシュ中継点データ
    /// </summary>
    public class NoteDataOrigin_HoldMeshRelay
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    /// <summary>
    /// ホールド判定点データ
    /// </summary>
    public class NoteDataOrigin_HoldHiddenJudgedRelay
    {
        public int HoldNumber { get; set; }

        public int[] Range { get; set; }
    }

    /// <summary>
    /// スペースホールド始点データ
    /// </summary>
    public class NoteDataOrigin_SpaceHoldStart
    {
        public int HoldNumber { get; set; }

        public Vector2[] Vertices { get; set; }
    }

    /// <summary>
    /// スペースホールド中継点データ
    /// </summary>
    public class NoteDataOrigin_SpaceHoldRelay
    {
        public int HoldNumber { get; set; }

        public Vector2[] Vertices { get; set; }
    }

    /// <summary>
    /// スペースホールドメッシュ中継点データ
    /// </summary>
    public class NoteDataOrigin_SpaceHoldMeshRelay
    {
        public int HoldNumber { get; set; }

        public Vector2[] Vertices { get; set; }
    }

    /// <summary>
    /// スペースホールド判定点データ
    /// </summary>
    public class NoteDataOrigin_SpaceHoldHiddenJudgedRelay
    {
        public int HoldNumber { get; set; }

        public Vector2[] Vertices { get; set; }
    }

    /// <summary>
    /// スペースホールド終点データ
    /// </summary>
    public class NoteDataOrigin_SpaceHoldEnd
    {
        public int HoldNumber { get; set; }

        public Vector2[] Vertices { get; set; }
    }

    #endregion

}