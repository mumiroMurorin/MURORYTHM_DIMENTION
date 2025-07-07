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
        private Dictionary<IChainNoteData, int> nextHoldNoteToNumber = new Dictionary<IChainNoteData, int>();
        private Dictionary<IChainNoteData, int> nextSpaceHoldNoteToNumber = new Dictionary<IChainNoteData, int>();

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
                new DynamicUpwardConverter(),
                new DynamicDownwardConverter(),
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),
            };

            holdConverters = new List<IChainNoteConvertable>
            {
                new HoldStartConverter(),
                new HoldRelayConverter(),
                new HoldEndConverter(),
                new HoldEndUnjudgeConverter(),
                new HoldMeshRelayConverter(),
                //new HoldHiddenJudgedRelay(),
            };

            spaceHoldConverters = new List<IChainNoteConvertable>
            {
                new SpaceHoldStartConverter(),
                new SpaceHoldRelayConverter(),
                new SpaceHoldEndConverter(),
                new SpaceHoldMeshRelayConverter(),
                //new SpaceHoldHiddenJudgedRelay(),
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
        /// 分節データを変換して代入する
        /// </summary>
        /// <param name="dataInEditor"></param>
        /// <param name="dataOrigin"></param>
        private bool SetSubDivisionData(SubDivisionDataInBeat dataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            // BPMのセット
            dataOrigin.Bpm = dataInEditor.Bpm.Value;

            // 変換に成功したかの判定
            bool isSucceed = true;

            // 同じ分節にchainノーツがあった時順番を変える
            ReorderChainedNotesInSubdivision(dataInEditor);

            // ノーツデータを一つずつ取り出して
            foreach (var noteData in dataInEditor.NoteDatas)
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
                    isSucceedLocal |= converter.AddDataForOrigin(noteData, dataOrigin, nextHoldNoteToNumber);
                }

                // 変換関数で変換出来るか総当たり(スぺースホールドノーツ)
                foreach (var converter in spaceHoldConverters)
                {
                    isSucceedLocal |= converter.AddDataForOrigin(noteData, dataOrigin, nextSpaceHoldNoteToNumber);
                }

                // 変換成功判定
                if (!isSucceedLocal)
                {
                    Debug.LogWarning($"【Converter】ノーツデータの変換に失敗しました: {noteData.NoteType}");
                    isSucceed = false;
                }
            }

            return isSucceed;
        }

        /// <summary>
        /// 分節データ内のノーツデータを並び替え
        /// </summary>
        /// <param name="dataInEditor"></param>
        private void ReorderChainedNotesInSubdivision(SubDivisionDataInBeat dataInEditor)
        {
            // 同じ分節に配置されているChainノーツ(始点)をあぶりだす
            var chainNoteStarts = FindChainNotesInSubdivision(dataInEditor);

            // 同分節のチェインノーツがなければ返す
            if (chainNoteStarts.Count == 0) { return; }

            foreach (var note in chainNoteStarts)
            {
                // 順番に(削除してから)代入する
                var chainNote = note;
                while (chainNote != null)
                {
                    dataInEditor.RemoveNote(chainNote);
                    dataInEditor.AddNote(chainNote);

                    if (!IsInSameSubdivision(chainNote, chainNote.NextNote.Value)) { break; }
                    chainNote = chainNote.NextNote.Value;
                }
            }
        }

        /// <summary>
        /// 同じ分節に配置されているChainノーツをあぶりだす
        /// </summary>
        /// <param name="dataInEditor"></param>
        /// <returns></returns>
        private List<IChainNoteData> FindChainNotesInSubdivision(SubDivisionDataInBeat dataInEditor)
        {
            List<IChainNoteData> chainNotes = new List<IChainNoteData>();
            foreach (var note in dataInEditor.NoteDatas)
            {
                // Chainノーツである場合のみ
                if (note is not IChainNoteData) { continue; }
                var thisNote = (IChainNoteData)note;
                var nextNote = thisNote.NextNote.Value;
                var backNote = thisNote.BackNote.Value;

                // 次ノーツが同じ分節に配置されていた場合
                if (nextNote != null && IsInSameSubdivision(thisNote, nextNote))
                {
                    // 一番最初のチェインノーツをあぶりだす
                    IChainNoteData chainNote = GetFirstChainNoteInSameSubdivision(thisNote);
                    chainNotes.Add(chainNote);
                }
                // 前ノーツが同じ分節に配置されていた場合
                else if (backNote != null && IsInSameSubdivision(thisNote, backNote))
                {
                    // 一番最初のチェインノーツをあぶりだす
                    IChainNoteData chainNote = GetFirstChainNoteInSameSubdivision(thisNote);
                    chainNotes.Add(chainNote);
                }
            }

            chainNotes = chainNotes.Distinct().ToList();

            return chainNotes;
        }

        /// <summary>
        /// 同分節内の一番最初のチェインノーツをあぶりだす
        /// </summary>
        /// <param name="chainNote"></param>
        /// <returns></returns>
        private IChainNoteData GetFirstChainNoteInSameSubdivision(IChainNoteData chainNote)
        {
            while (chainNote != null && IsInSameSubdivision(chainNote, chainNote.BackNote.Value))
            {
                chainNote = chainNote.BackNote.Value;
            }

            return chainNote;
        }

        /// <summary>
        /// ノーツが同じ分節内にあるか返す
        /// </summary>
        /// <param name="note1"></param>
        /// <param name="note2"></param>
        /// <returns></returns>
        private bool IsInSameSubdivision(IDeployableNoteData note1, IDeployableNoteData note2)
        {
            if (note1 == null) { return false; }
            if (note2 == null) { return false; }
            if (note1.Address.BarIndex != note2.Address.BarIndex) { return false; }
            if (note1.Address.SubDivisionIndex != note2.Address.SubDivisionIndex) { return false; }

            return true;
        }
    }
}
