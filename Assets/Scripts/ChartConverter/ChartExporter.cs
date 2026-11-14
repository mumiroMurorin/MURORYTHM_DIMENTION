using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    /// <summary>
    /// ChartEditor → ChartDataOrigin (外部ファイルにエクスポート)
    /// </summary>
    public class ChartExporter
    {
        private List<IUnchainedNoteConvertable> unchainConverters = new List<IUnchainedNoteConvertable>();
        private List<IChainNoteConvertable> holdConverters = new List<IChainNoteConvertable>();
        private List<IChainNoteConvertable> spaceHoldConverters = new List<IChainNoteConvertable>();

        public ChartDataOrigin Export(ChartEditor.ChartData chartData, float offset)
        {
            Initialize();

            // 最初に変換
            return ConvertChartDataOrigin(chartData, offset);
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

            holdConverters = new List<IChainNoteConvertable>
            {
                new HoldStartConverter(),
                new DivineHoldStartConverter(),
                new HoldRelayConverter(),
                new HoldEndConverter(),
                new HoldEndUnjudgeConverter(),
                new HoldMeshRelayConverter(),
            };

            spaceHoldConverters = new List<IChainNoteConvertable>
            {
                new SpaceHoldStartConverter(),
                new SpaceHoldRelayConverter(),
                new SpaceHoldEndConverter(),
                new SpaceHoldMeshRelayConverter(),
            };
        }

        /// <summary>
        /// ChartEditor.ChartData => ChartDataOrigin
        /// </summary>
        /// <param name="chartData"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private ChartDataOrigin ConvertChartDataOrigin(ChartEditor.ChartData chartData, float offset)
        {
            bool isSucceed = true;
            ChartDataOrigin chartDataOrigin = new ChartDataOrigin();

            // オフセット
            chartDataOrigin.OffsetMs = offset;
            chartDataOrigin.BarDatas = new List<BarDataOrigin>();

            if (chartData == null) { return chartDataOrigin; }
            if (chartData.BarDatas == null) { return chartDataOrigin; }

            // 譜面から小節を一つずつ取り出す
            foreach (var bar in chartData.BarDatas)
            {
                BarDataOrigin barDataOrigin = new BarDataOrigin();
                if (!SetBarData(bar, barDataOrigin)) { isSucceed = false; }

                // データセット
                chartDataOrigin.BarDatas.Add(barDataOrigin);
            }

            if (isSucceed) { Debug.Log("【Converter】譜面データの変換成功"); }
            else { Debug.LogWarning("【Converter】譜面データの変換失敗。ログを確かめてください"); }

            return chartDataOrigin;
        }

        /// <summary>
        /// 拍子データを変換して代入する
        /// </summary>
        /// <param name="barDataInEditor"></param>
        /// <param name="barDataOrigin"></param>
        private bool SetBarData(BarDataInChart barDataInEditor, BarDataOrigin barDataOrigin)
        {
            bool isSucceed = true;

            // 拍子
            barDataOrigin.BeatCount = barDataInEditor.BeatCount.Value;
            barDataOrigin.BeatUnit = barDataInEditor.BeatUnit.Value;

            // 分割数
            barDataOrigin.DivisionNum = barDataInEditor.DivisionNum.Value;

            barDataOrigin.SubDivisionDatas = new List<SubDivisionDataOrigin>();
            // 拍子データから分節を一つずつ取り出す
            foreach (var subDivision in barDataInEditor.SubDivisionDatas)
            {
                SubDivisionDataOrigin subDataOrigin = new SubDivisionDataOrigin();
                if (!SetSubDivisionData(subDivision, subDataOrigin)) { isSucceed = false; }

                // データ追加
                barDataOrigin.SubDivisionDatas.Add(subDataOrigin);
            }

            return isSucceed;
        }

        /// <summary>
        /// 分線のノーツを、保存する順に並べて返す
        ///
        /// ふつうは置かれた順のまま。同じ分線に、つながるノーツ(ホールド・スペースホールド)が2つ以上あるときだけ、
        /// 同じ接続の中でつながっている順に並べる。
        /// (保存先は種類ごとの一覧なので、同じ種類のノーツが、つながっている順に書かれるようになる。
        ///  順番の番号を読まない古い読み込み処理でも、同じ種類同士なら正しい順で読める)
        /// </summary>
        private IEnumerable<IDeployableNoteData> GetNotesInExportOrder(ISubDivisionDataGetter dataInEditor)
        {
            var notes = dataInEditor.NoteDatas;

            int chainNoteCount = 0;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] is IChainNoteData) { chainNoteCount++; }
            }

            if (chainNoteCount < 2) { return notes; }

            // OrderBy は、同じ順位のものの並びを変えない
            return notes
                .OrderBy(note => note is IChainNoteData chainNote ? ChainOrderResolver.GetIndexInSubdivision(chainNote) : 0)
                .ToList();
        }

        /// <summary>
        /// 分節データを変換して代入する
        /// </summary>
        /// <param name="dataInEditor"></param>
        /// <param name="dataOrigin"></param>
        private bool SetSubDivisionData(ISubDivisionDataGetter dataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            // BPM、スピード倍率のセット
            dataOrigin.Bpm = dataInEditor.Bpm.Value;
            dataOrigin.SpeedRatio = dataInEditor.SpeedRatio.Value;

            // 変換に成功したかの判定
            bool isSucceed = true;

            // ノーツデータを一つずつ取り出して
            foreach (var noteData in GetNotesInExportOrder(dataInEditor))
            {
                if (noteData == null) { continue; }
                bool isSucceedLocal = false;

                // 変換関数で変換出来るか総当たり(普通のノーツ)
                foreach (var converter in unchainConverters)
                {
                    isSucceedLocal |= converter.AddDataForOrigin(noteData, dataOrigin);
                }

                // 変換関数で変換出来るか総当たり(ホールドノーツ)
                foreach (var converter in holdConverters)
                {
                    isSucceedLocal |= converter.AddDataForOrigin(noteData, dataOrigin);
                }

                // 変換関数で変換出来るか総当たり(スぺースホールドノーツ)
                foreach (var converter in spaceHoldConverters)
                {
                    isSucceedLocal |= converter.AddDataForOrigin(noteData, dataOrigin);
                }

                // 変換成功判定
                if (!isSucceedLocal)
                {
                    Debug.LogWarning($"【Converter】ノーツデータの変換に失敗しました: {noteData.Address}, {noteData.NoteType}");
                    isSucceed = false;
                }
            }

            return isSucceed;
        }
    }
}
