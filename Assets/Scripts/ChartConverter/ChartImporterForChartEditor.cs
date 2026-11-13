using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert 
{
    /// <summary>
    /// ChartDataOrigin → ChartEditor (外部ファイルから譜面エディタにインポート)
    /// </summary>
    public class ChartImporterForChartEditor
    {
        private List<IUnchainedNoteConvertable> unchainConverters = new List<IUnchainedNoteConvertable>();
        private List<IChainNoteConvertable> holdConverters = new List<IChainNoteConvertable>();
        private List<IChainNoteConvertable> spaceHoldConverters = new List<IChainNoteConvertable>();

        /// <summary>
        /// エディタに読み込めるデータかを検査する (読み込み前に呼ぶ)
        /// </summary>
        /// <param name="error">読み込めない理由</param>
        public static bool TryValidate(ChartDataOrigin dataOrigin, out string error)
        {
            error = string.Empty;

            if (dataOrigin == null)
            {
                error = "ファイルの中身が空です。";
                return false;
            }

            if (dataOrigin.BarDatas == null)
            {
                error = "小節データ(BarDatas)がありません。譜面ファイルではない可能性があります。";
                return false;
            }

            for (int i = 0; i < dataOrigin.BarDatas.Count; i++)
            {
                var bar = dataOrigin.BarDatas[i];
                string barName = $"{i + 1}小節目";

                if (bar == null)
                {
                    error = $"{barName}のデータがありません。";
                    return false;
                }

                if (bar.BeatCount < 1 || bar.DivisionNum < 1 || !(bar.BeatUnit > 0f) || float.IsInfinity(bar.BeatUnit))
                {
                    error = $"{barName}の拍子・分割数が不正です。(BeatCount: {bar.BeatCount}, BeatUnit: {bar.BeatUnit}, DivisionNum: {bar.DivisionNum})";
                    return false;
                }

                if (bar.SubDivisionDatas == null)
                {
                    error = $"{barName}に分線データ(SubDivisionDatas)がありません。";
                    return false;
                }

                int expectedCount = bar.BeatCount * bar.DivisionNum;
                if (bar.SubDivisionDatas.Count > expectedCount)
                {
                    error = $"{barName}の分線の数が拍子と合いません。({bar.SubDivisionDatas.Count}本 / 最大{expectedCount}本)";
                    return false;
                }

                if (bar.SubDivisionDatas.Contains(null))
                {
                    error = $"{barName}に空の分線データがあります。";
                    return false;
                }
            }

            return true;
        }

        public bool Import(ChartDataOrigin dataOrigin, ref ChartEditor.ChartData chartData, IChartEditorDataSetter dataSetter)
        {
            bool isSucceed = true;

            // 初期化
            Initialize();

            dataSetter.SetOffset(dataOrigin.OffsetMs);
            chartData.AddBar(dataOrigin.BarDatas.Count);

            // 分線を一つずつ取り出す
            for (int i = 0; i < dataOrigin.BarDatas.Count; i++)
            {
                var bar = dataOrigin.BarDatas[i];

                if (!SetDataFromBarData(bar, chartData.BarDatas[i], chartData.AddNote)) { isSucceed = false; }
            }

            // 分線のBPMを直接書き換えたので、線の位置を先頭から更新させる
            if (chartData.BarDatas.Count > 0) { chartData.NotifyBpmChanged(0, 0); }

            if (isSucceed) { Debug.Log("【Converter】譜面データの変換成功"); }
            else { Debug.LogWarning("【Converter】譜面データの変換失敗。ログを確かめてください"); }

            return isSucceed;
        }

        private void Initialize()
        {
            // ここに変換関数を記述していく
            unchainConverters = new List<IUnchainedNoteConvertable>()
            {
                new TouchNoteConverter(),
                new DivineTouchNoteConverter(),

                new DynamicUpwardConverter(),
                new DynamicDownwardConverter(),
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),

                new SpaceBreakConverter(),
            };

            holdConverters = new List<IChainNoteConvertable>()
            {
                new HoldStartConverter(),
                new DivineHoldStartConverter(),
                new HoldRelayConverter(),
                new HoldEndConverter(),
                new HoldEndUnjudgeConverter(),
                new HoldMeshRelayConverter(),
            };

            spaceHoldConverters = new List<IChainNoteConvertable>()
            {
                new SpaceHoldStartConverter(),
                new SpaceHoldRelayConverter(),
                new SpaceHoldEndConverter(),
                new SpaceHoldMeshRelayConverter(),
            };
        }

        private bool SetDataFromBarData(BarDataOrigin barDataOrigin, ChartEditor.BarDataInChart dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            bool isSucceed = true;

            // 拍子、分割数の取得
            int beatCount = barDataOrigin.BeatCount;
            float beatUnit = barDataOrigin.BeatUnit;
            int divNum = barDataOrigin.DivisionNum;

            dataInChartEditor.SetBeatCount(beatCount);
            dataInChartEditor.SetBeatUnit(beatUnit);
            dataInChartEditor.SetDivisionNum(divNum);

            // 小節データを一つ一つ取り出してデータを代入
            for (int i = 0; i < barDataOrigin.SubDivisionDatas.Count; i++)
            {
                var sub = barDataOrigin.SubDivisionDatas[i];

                if (!SetDataFromSubDivisionData(sub, dataInChartEditor.SubDivisionDatas[i], onAddNoteData)) { isSucceed = false; }
            }

            return isSucceed;
        }

        private bool SetDataFromSubDivisionData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            bool isSucceed = true;

            // bpmのセット
            dataInChartEditor.SetBpm(dataOrigin.Bpm);
            dataInChartEditor.SetSpeedRatio(dataOrigin.SpeedRatio);

            // 一つずつ取り出して変換
            foreach (var converter in unchainConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor, onAddNoteData);
            }

            foreach (var converter in holdConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor, onAddNoteData);
            }

            foreach (var converter in spaceHoldConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor, onAddNoteData);
            }

            return isSucceed;
        }
    }

}