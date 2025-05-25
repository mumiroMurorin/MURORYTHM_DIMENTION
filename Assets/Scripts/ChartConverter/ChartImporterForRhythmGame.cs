using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    /// <summary>
    /// ChartDataOrigin → 音ゲー 
    /// </summary>
    public class ChartImporterForRhythmGame
    {
        // ここに変換関数を記述していく
        private List<IOriginDataToRhythmGameConvertable> converters = new List<IOriginDataToRhythmGameConvertable>();

        public ChartData Import(ChartDataOrigin dataOrigin, INoteSpawnDataOptionHolder optionHolder)
        {
            Initialize();

            bool isSucceed = true;

            ChartData chartData = new ChartData();
            CalcTimingClass calcTiming = new CalcTimingClass(dataOrigin.OffsetMs, optionHolder.OffsetMs.Value);


            // 分線を一つずつ取り出す
            foreach (var bar in dataOrigin.BarDatas)
            {
                if (!SetdataFromBarData(bar, chartData, calcTiming))
                {
                    isSucceed = false;
                }
            }

            if (isSucceed) { Debug.Log("【Converter】譜面データの変換成功"); }
            else { Debug.LogWarning("【Converter】譜面データの変換失敗。ログを確かめてください"); }

            return chartData;
        }

        private void Initialize()
        {
            // ここに変換関数を記述していく
            converters = new List<IOriginDataToRhythmGameConvertable>()
            {
                new TouchNoteConverter(),

                new DynamicUpwardConverter(),
                new DynamicDownwardConverter(),
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),

                new HoldStartConverter(),
                new HoldRelayConverter(),
                new HoldHiddenJudgedRelay(),
                new HoldEndConverter(),
                new HoldMeshConverter(),

                new SpaceHoldRelayConverter(),
                new SpaceHoldMeshConverter(),
            };
        }

        /// <summary>
        /// 小節データから分節データを抽出してノーツデータに変換
        /// </summary>
        /// <param name="barDataOrigin"></param>
        /// <param name="chartData"></param>
        /// <returns></returns>
        private bool SetdataFromBarData(BarDataOrigin barDataOrigin, ChartData chartData, CalcTimingClass calcTiming)
        {
            bool isSucceed = true;

            // 拍子、分割数の取得
            int beatCount = barDataOrigin.BeatCount;
            float beatUnit = barDataOrigin.BeatUnit;
            int divNum = barDataOrigin.DivisionNum;

            // 小節データを一つ一つ取り出してデータを代入
            foreach (var sub in barDataOrigin.SubDivisionDatas)
            {
                float bpm = sub.Bpm;
                float timing = calcTiming.CurrentTiming;

                if (!SetDataFromSubDivisionData(sub, chartData, timing))
                {
                    isSucceed = false;
                }

                calcTiming.AddTiming(beatUnit, divNum, bpm);
            }

            return isSucceed;
        }

        /// <summary>
        /// 分節データからノーツデータに一つ一つ変換
        /// </summary>
        /// <param name="dataOrigin"></param>
        /// <param name="chartData"></param>
        /// <param name="calcTiming"></param>
        /// <returns></returns>
        private bool SetDataFromSubDivisionData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float secondsPassed)
        {
            bool isSucceed = true;

            // 一つずつ取り出して変換
            foreach (var converter in converters)
            {
                if (!converter.AddDataForGameData(dataOrigin, chartData, secondsPassed))
                {
                    isSucceed = false;
                }
            }

            return isSucceed;
        }

        /// <summary>
        /// ノーツが流れてくる時間を計算するクラス
        /// </summary>
        private class CalcTimingClass
        {
            float timePassedSec;

            public float CurrentTiming { get { return timePassedSec; } }

            public CalcTimingClass(float musicOffsetMs, float optionOffsetMs)
            {
                timePassedSec = -(musicOffsetMs + optionOffsetMs) / 1000f;
            }

            public float AddTiming(float beatUnit, float divNum, float bpm)
            {
                // 1分節の時間[sec]
                // = 1秒間に打たれる4分音符の数 * (BeatUnit / 4f) / 分割数
                // = (60f / 1分間に打たれる4分音符の数(BPM)) * (4f / BeatUnit) / 分割数  
                float subDivisionSeconds = (60f / bpm) * (4f / beatUnit) / divNum;

                // 次の分節の時間[sec]
                // = 経過時間[sec] + 1分節の時間[sec]
                timePassedSec += subDivisionSeconds;
                return timePassedSec;
            }

        }
    }

}