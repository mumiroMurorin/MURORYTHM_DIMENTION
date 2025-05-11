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
        private List<IChainNoteConvertable> chainConverters = new List<IChainNoteConvertable>();
        private Dictionary<IGroundChainNoteData, int> nextHoldNoteToNumber = new Dictionary<IGroundChainNoteData, int>();

        public ChartDataOrigin Export(ChartEditor.ChartData chartData, float offset)
        {
            Initialize();

            // 最初に変換
            return ConvertChartDataOrigin(chartData, offset);

            // エクスポート
        }

        private void Initialize()
        {
            // ここに変換関数を記述していく
            unchainConverters = new List<IUnchainedNoteConvertable>()
            {
                new TouchNoteConverter(),
                new DynamicUpwardConverter(),
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),

            };

            chainConverters = new List<IChainNoteConvertable>
            {
                new HoldStartConverter(),
                new HoldRelayConverter(),
                new HoldEndConverter(),
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

            // 譜面から小節を一つずつ取り出す
            foreach (var bar in chartData.BarDatas)
            {
                BarDataOrigin barDataOrigin = new BarDataOrigin();
                if(!SetBarData(bar, barDataOrigin)) { isSucceed = false; }

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
                if(!SetSubDivisionData(subDivision, subDataOrigin)) { isSucceed = false; }

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
            // ノーツデータを一つずつ取り出して
            foreach (var noteData in dataInEditor.NoteDatas)
            {
                bool isSucceed_ = false;

                // 変換器で変換出来るか総当たり
                foreach (var converter in unchainConverters)
                {
                    isSucceed_ |= converter.AddDataForOrigin(noteData, dataOrigin);
                }

                foreach(var converter in chainConverters)
                {
                    isSucceed_ |= converter.AddDataForOrigin(noteData, dataOrigin, ref nextHoldNoteToNumber);
                }

                if (!isSucceed_) 
                { 
                    Debug.LogWarning($"【Converter】ノーツデータの変換に失敗しました: {noteData.NoteType}");
                    isSucceed = false;
                }

            }

            return isSucceed;
        }

    }

    /// <summary>
    /// ChartDataOrigin → ChartEditor (外部ファイルから譜面エディタにインポート)
    /// </summary>
    public class ChartImporterForChartEditor
    {
        private List<IUnchainedNoteConvertable> unchainConverters = new List<IUnchainedNoteConvertable>();
        private List<IChainNoteConvertable> chainConverters = new List<IChainNoteConvertable>();
        private Dictionary<int, IGroundChainNoteData> holdNumberToChainNote = new Dictionary<int, IGroundChainNoteData>();

        public bool Import(ChartDataOrigin dataOrigin, ref ChartEditor.ChartData chartData)
        {
            bool isSucceed = true;

            // 初期化
            Initialize();

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
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),
            };

            chainConverters = new List<IChainNoteConvertable>()
            {
                new HoldStartConverter(),
                new HoldRelayConverter(),
                new HoldEndConverter(),
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
            for (int i = 0;i < barDataOrigin.SubDivisionDatas.Count; i++)
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

            foreach (var converter in chainConverters)
            {
                isSucceed &= converter.AddDataForEditorData(dataOrigin, dataInChartEditor, ref holdNumberToChainNote);
            }

            return isSucceed;
        }
    }

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
            foreach(var bar in dataOrigin.BarDatas)
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
                new DynamicRightwardConverter(),
                new DynamicLeftwardConverter(),
                new HoldStartConverter(),
                new HoldRelayConverter(),
                new HoldEndConverter(),
                new HoldMeshConverter(),
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
            foreach(var sub in barDataOrigin.SubDivisionDatas)
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
            foreach(var converter in converters)
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

    /// <summary>
    /// 判定調整
    /// </summary>
    public class JudgementWindowAdjuster
    {
        /// <summary>
        /// 譜面データの中で判定枠を仕様に従って調整する
        /// </summary>
        /// <param name="chartData"></param>
        /// <param name="judgementWindows"></param>
        public void AdjustJudgementWindow(ChartData chartData, List<NoteTypeToJudgementWindow> judgementWindows)
        {
            List<IJudgableNoteData> judgableList = new List<IJudgableNoteData>();
            List<IClippedJudgableNote> clippedJudgableList = new List<IClippedJudgableNote>();

            // ノーツデータの中から判定持ちを取り出す
            foreach(var noteDataList in chartData.AllNoteDataLists)
            {
                // 判定持ちノーツだったら取り出す
                if(noteDataList.Count > 0 && noteDataList[0] is IJudgableNoteData)
                {
                    judgableList.AddRange(noteDataList.OfType<IJudgableNoteData>().ToList());
                }

                // 削り判定持ちノーツだったら取り出す
                else if(noteDataList.Count > 0 && noteDataList[0] is IClippedJudgableNote)
                {
                    clippedJudgableList.AddRange(noteDataList.OfType<IClippedJudgableNote>().ToList());
                }
            }

            // 判定持ちに判定枠を与える
            foreach(var judgableData in judgableList)
            {
                // 判定枠の取得
                var window = GetJudgementWindow(judgableData.NoteType, judgementWindows);
                if(window == null) { continue; }

                judgableData.JudgementWindow = window;
            }

            // 削り判定持ちに判定枠を与える
            // ソート
            clippedJudgableList.Sort((a, b) => a.Timing.CompareTo(b.Timing));
            for (int i = 0; i < clippedJudgableList.Count; i++)
            {
                var clippedData = clippedJudgableList[i];

                // 判定枠の取得
                var window = GetJudgementWindow(clippedData.NoteType, judgementWindows);
                if (window == null) { continue; }

                // ディープコピー
                clippedJudgableList[i].JudgementWindow = window.Copy();

                // 判定を削る
                ClipJudementWindow(clippedJudgableList, i);
            }
        }

        /// <summary>
        /// そのノーツの判定枠を返す
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="judgementWindows"></param>
        /// <returns></returns>
        private JudgementWindow GetJudgementWindow(NoteType targetType ,List<NoteTypeToJudgementWindow> judgementWindows)
        {
            // 判定枠の取得
            foreach (var windowType in judgementWindows)
            {
                var window = windowType.CheckAndGetJudgementWindow(targetType);
                if(window != null) { return window; }
            }

            Debug.LogWarning($"【System】{targetType}に該当する判定枠が見つかりませんでした");
            return null;
        }

        private void ClipJudementWindow(List<IClippedJudgableNote> sortedList, int index)
        {
            if (sortedList.Count <= index) { Debug.LogError($"【System】範囲外です: {index}"); }

            IClippedJudgableNote targetNote = sortedList[index];
            JudgementWindow targetWindow = targetNote.JudgementWindow;

            // 前判定端
            float startJudgement = targetNote.Timing - targetWindow.GoodWindowFaster;

            // 前判定を削る
            int i = index;
            while (true)
            {
                // 最初のノーツならおしまい
                if (--i < 0) { break; }
                
                IClippedJudgableNote previousNote = sortedList[i];

                // 判定枠が被らなくなったらおしまい
                float previousEndJudgement = previousNote.Timing + previousNote.JudgementWindow.GoodWindowLatter;
                float cover = previousEndJudgement - startJudgement;
                if (cover < 0) { break; }

                // レーンが被ってなかったら戻す
                if (!targetNote.Range.Intersect(previousNote.Range).Any()) { continue; }

                // このノーツの前判定を削り、前ノーツの後ろ判定も削る
                targetWindow.ClipWindow(cover / 2f, true);
                previousNote.JudgementWindow.ClipWindow(cover / 2f, false);
            }
        }
    }

    #region ノーツ変換関数

    /// <summary>
    /// ChartEditor.NoteDataを変換してSubDivisionDataOriginにぶち込む
    /// </summary>
    public interface IOriginDataToRhythmGameConvertable
    {
        bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing);
    }

    public interface IUnchainedNoteConvertable
    {
        bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);

        bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor);
    }

    public interface IChainNoteConvertable
    {
        bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, 
            ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber);

        bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,
            ref Dictionary<int, IGroundChainNoteData> numberToStartNote);
    }

    /// <summary>
    /// タッチノーツ
    /// </summary>
    public class TouchNoteConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.TouchNote;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if(noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if(dataOrigin.TouchNoteData == null) 
            { 
                dataOrigin.TouchNoteData = new List<NoteDataOrigin_Touch>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_Touch data = new NoteDataOrigin_Touch()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.TouchNoteData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if(dataOrigin.TouchNoteData == null) { return true; }

            foreach(var noteOrigin in dataOrigin.TouchNoteData)
            {
                NoteData_Touch noteData = new NoteData_Touch
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor)
        {
            if (dataOrigin.TouchNoteData == null) { return true; }

            foreach(var noteDataOrigin in dataOrigin.TouchNoteData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Touch();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ↑ダイナミック↑ノーツ
    /// </summary>
    public class DynamicUpwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundUpward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicUpwardData == null)
            {
                dataOrigin.DynamicUpwardData = new List<NoteDataOrigin_DynamicUpward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicUpward data = new NoteDataOrigin_DynamicUpward()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicUpwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicUpwardData)
            {
                NoteData_DynamicGroundUpward noteData = new NoteData_DynamicGroundUpward
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor)
        {
            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicUpwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicUpward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// →ダイナミック→ノーツ
    /// </summary>
    public class DynamicRightwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundRightward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicRightwardData == null)
            {
                dataOrigin.DynamicRightwardData = new List<NoteDataOrigin_DynamicRightward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicRightward data = new NoteDataOrigin_DynamicRightward()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicRightwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {

            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicRightwardData)
            {
                NoteData_DynamicGroundRightward noteData = new NoteData_DynamicGroundRightward
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor)
        {
            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicRightwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicRightward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ←ダイナミック←ノーツ
    /// </summary>
    public class DynamicLeftwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundLeftward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicLeftwardData == null)
            {
                dataOrigin.DynamicLeftwardData = new List<NoteDataOrigin_DynamicLeftward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicLeftward data = new NoteDataOrigin_DynamicLeftward()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicLeftwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicLeftwardData)
            {
                NoteData_DynamicGroundLeftward noteData = new NoteData_DynamicGroundLeftward
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor)
        {
            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicLeftwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicLeftward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドスタートノーツ
    /// </summary>
    public class HoldStartConverter : IOriginDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.Hold;

        int currentHoldNumber = 0;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != type) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }

            IGroundChainNoteData backNote = ((IGroundChainNoteData)noteDataInEditor).BackNote.Value;
            IGroundChainNoteData nextNote = ((IGroundChainNoteData)noteDataInEditor).NextNote.Value;
            if (backNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }
            if (backNote != null && nextNote != null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldStartData == null)
            {
                dataOrigin.HoldStartData = new List<NoteDataOrigin_HoldStart>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldStart data = new NoteDataOrigin_HoldStart()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = currentHoldNumber
            };

            dataOrigin.HoldStartData.Add(data);
            nextNoteToNumber.Add(nextNote, currentHoldNumber++);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                NoteData_HoldStart noteData = new NoteData_HoldStart
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldStartData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // ディクショナリーへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber,out var startNote))
                {
                    Debug.LogWarning($"【Converter】HoldStartの変換の際、既にHoldNumberが存在しました: {noteDataOrigin.HoldNumber}");
                    return false;
                }
                else
                {
                    numberToStartNote.Add(noteDataOrigin.HoldNumber, chainData);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールド中継ノーツ
    /// </summary>
    public class HoldRelayConverter : IOriginDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.Hold &&
                noteDataInEditor.NoteType != DeploymentNoteType.HoldHidden) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }
            if (noteDataInEditor is not ITypeChangableNoteData) { return false; }

            IGroundChainNoteData thisNote = (IGroundChainNoteData)noteDataInEditor;
            IGroundChainNoteData backNote = thisNote.BackNote.Value;
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】HoldRelayの変換の際、このノーツが見つかりませんでした: {number}");
                return false;
            }
            else
            {
                nextNoteToNumber.Remove(thisNote);
                nextNoteToNumber.Add(nextNote, number);
            }

            // 新たにインスタンス化
            if (dataOrigin.HoldRelayData == null) { dataOrigin.HoldRelayData = new List<NoteDataOrigin_HoldRelay>(); }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldRelay data = new NoteDataOrigin_HoldRelay()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                IsHidden = noteDataInEditor.NoteType == DeploymentNoteType.HoldHidden,
                HoldNumber = number
            };

            dataOrigin.HoldRelayData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldRelayData)
            {
                // 隠された中継ノートは除外
                if (noteOrigin.IsHidden) { continue; }

                NoteData_HoldRelay noteData = new NoteData_HoldRelay
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldRelayData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;
                if (noteDataOrigin.IsHidden) { ((ITypeChangableNoteData)noteData).ChangeNoteType(true); }

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData);
                }
                else
                {
                    Debug.LogWarning($"【Converter】HoldRelayの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドエンドノーツ
    /// </summary>
    public class HoldEndConverter : IOriginDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.Hold;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != type) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }

            IGroundChainNoteData thisNote = (IGroundChainNoteData)noteDataInEditor;
            IGroundChainNoteData backNote = thisNote.BackNote.Value;
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }
            if (backNote != null && nextNote != null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】HoldEndの変換の際、このノーツが見つかりませんでした: {number}");
                return false;
            }
            else
            {
                // 終点なのでAddしない
                nextNoteToNumber.Remove(thisNote);
            }

            // 新たにインスタンス化
            if (dataOrigin.HoldEndData == null)
            {
                dataOrigin.HoldEndData = new List<NoteDataOrigin_HoldEnd>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldEnd data = new NoteDataOrigin_HoldEnd()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = number
            };

            dataOrigin.HoldEndData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldEndData)
            {
                NoteData_HoldEnd noteData = new NoteData_HoldEnd
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldEndData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData);
                }
                else
                {
                    Debug.LogWarning($"【Converter】HoldEndの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドメッシュ
    /// </summary>
    public class HoldMeshConverter : IOriginDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToRange>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToRange>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldStartData == null || dataOrigin.HoldRelayData == null || dataOrigin.HoldEndData == null) { return true; }

            // 始点、メッシュデータ格納リストの作成
            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                // 一度ディクショナリーに格納
                List<TimeToRange> meshList; 
                // ディクショナリーに登録されていなければ新規作成
                if(numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber,out meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }
                
                meshList = new List<TimeToRange>();
                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
                numberToHoldMeshDataOrigin.Add(noteOrigin.HoldNumber, meshList);
            }

            // 中継点、メッシュデータ格納リストにデータを追加
            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }
                
                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
            }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }
                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });

                // 変換して譜面データに代入
                chartData.AddNoteData(GenerateNoteData_HoldMesh(numberToHoldMeshDataOrigin[noteOrigin.HoldNumber]));
            }

            return true;
        }

        /// <summary>
        /// List＜HoldMeshOriginAndTiming＞ → NoteData_HoldMesh
        /// </summary>
        /// <param name="meshDataList"></param>
        /// <returns></returns>
        private NoteData_HoldMesh GenerateNoteData_HoldMesh(List<TimeToRange> timeToRanges)
        {
            var noteData = new NoteData_HoldMesh();

            noteData.Timing = timeToRanges[0].Timing;
            noteData.TimeToRanges = timeToRanges;

            return noteData;
        }
    }

    #endregion
}
