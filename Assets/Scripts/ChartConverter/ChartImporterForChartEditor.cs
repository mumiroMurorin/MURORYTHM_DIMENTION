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

        // 読み込み中の状態 (Begin で設定し、ImportBar / End で使う)
        ChartDataOrigin importingOrigin;
        ChartEditor.ChartData importingChartData;
        bool isImportSucceed;

        /// <summary>
        /// 読み込む小節の数 (Begin の後に有効)
        /// </summary>
        public int BarCount => importingOrigin != null ? importingOrigin.BarDatas.Count : 0;

        /// <summary>
        /// 譜面をまとめて読み込む
        /// </summary>
        public bool Import(ChartDataOrigin dataOrigin, ref ChartEditor.ChartData chartData, IChartEditorDataSetter dataSetter)
        {
            Begin(dataOrigin, chartData, dataSetter);

            for (int i = 0; i < BarCount; i++)
            {
                ImportBar(i);
            }

            return End();
        }

        /// <summary>
        /// 読み込みの開始。この後 ImportBar を小節の数だけ先頭から順に呼び、最後に End を呼ぶ
        /// (小節ごとに分けてあるのは、読み込みを数フレームに分けて画面を止めないため)
        /// </summary>
        public void Begin(ChartDataOrigin dataOrigin, ChartEditor.ChartData chartData, IChartEditorDataSetter dataSetter)
        {
            // 初期化
            Initialize();

            importingOrigin = dataOrigin;
            importingChartData = chartData;
            isImportSucceed = true;

            dataSetter.SetOffset(dataOrigin.OffsetMs);
        }

        /// <summary>
        /// 小節を1つ読み込む (小節・分線・ノーツを譜面に追加する)
        /// </summary>
        public void ImportBar(int barIndex)
        {
            var bar = importingOrigin.BarDatas[barIndex];
            var chartData = importingChartData;

            // 分線に最初に入れるBPM・スピード倍率
            // 先頭の分線の値を使う。分線データが無ければ直前の小節の最後の値を引き継ぐ
            SubdivisionConfig subConfig;
            if (bar.SubDivisionDatas.Count > 0)
            {
                subConfig = new SubdivisionConfig(bar.SubDivisionDatas[0].Bpm, bar.SubDivisionDatas[0].SpeedRatio);
            }
            else if (barIndex > 0 && chartData.BarDatas[barIndex - 1].SubDivisionDatas.Count > 0)
            {
                subConfig = chartData.BarDatas[barIndex - 1].SubDivisionDatas.Last().SubConfig;
            }
            else
            {
                subConfig = new SubdivisionConfig(DEFAULT_BPM, DEFAULT_SPEEDRATIO);
            }

            // 最初から正しい拍子・分割数で小節を作る
            // (既定の設定で作ってから変更すると、分線の作り直しと譜面全体の線の並べ直しが小節ごとに走り、非常に重い)
            chartData.AddBar(new BarConfig(bar.BeatCount, bar.BeatUnit, bar.DivisionNum), subConfig);

            if (!SetDataFromBarData(bar, chartData.BarDatas[barIndex], chartData.AddNote)) { isImportSucceed = false; }

            // 分線のBPMを直接書き換えたので、この小節の線の位置を更新させる
            chartData.NotifyBpmChanged(barIndex, 0);
        }

        /// <summary>
        /// 読み込みの終了
        /// </summary>
        /// <returns>すべてのノーツを読み込めたとき true</returns>
        public bool End()
        {
            if (isImportSucceed) { Debug.Log("【Converter】譜面データの変換成功"); }
            else { Debug.LogWarning("【Converter】譜面データの変換失敗。ログを確かめてください"); }

            importingOrigin = null;
            importingChartData = null;

            return isImportSucceed;
        }

        // 分線データが1つも無いときに使う値 (エディタで新規に小節を作るときの既定値と同じ)
        const float DEFAULT_BPM = 256f;
        const float DEFAULT_SPEEDRATIO = 1f;

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