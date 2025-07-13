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

                if (!SetDataFromBarData(bar, chartData.BarDatas[i])) { isSucceed = false; }
            }

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
                new DynamicUpwardConverter(),
                new DynamicDownwardConverter(),
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),
            };

            holdConverters = new List<IChainNoteConvertable>()
            {
                new HoldStartConverter(),
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

        private bool SetDataFromBarData(BarDataOrigin barDataOrigin, ChartEditor.BarDataInChart dataInChartEditor)
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

                if (!SetDataFromSubDivisionData(sub, dataInChartEditor.SubDivisionDatas[i])) { isSucceed = false; }
            }

            return isSucceed;
        }

        private bool SetDataFromSubDivisionData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor)
        {
            bool isSucceed = true;

            // bpmのセット
            dataInChartEditor.SetBpm(dataOrigin.Bpm);

            // 一つずつ取り出して変換
            foreach (var converter in unchainConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor);
            }

            foreach (var converter in holdConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor);
            }

            foreach (var converter in spaceHoldConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor);
            }

            return isSucceed;
        }
    }

}