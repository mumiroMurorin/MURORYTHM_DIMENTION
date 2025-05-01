using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using JsonUtil;
using System;

namespace ChartConvert
{
    public class ChartExporter
    {
        // ここに変換関数を記述していく
        private List<INoteDataConvertable> converters = new List<INoteDataConvertable>()
        {
            new TouchNoteConverter(),
            new DynamicUpwardConverter(),
            new DynamicRightwardConverter(),
            new DynamicLeftwardConverter(),
            new HoldConverter(),
        };

        public void Export(ChartEditor.ChartData chartData, float offset)
        {
            // 最初に変換
            ChartDataOrigin chartDataOrigin = ConvertChartDataOrigin(chartData, offset);

            // エクスポート
            JsonConverter.TrySaveToJsonFileDialog(chartDataOrigin);
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
                foreach (var converter in converters)
                {
                    // 変換出来たらこのループを出る
                    if (converter.CheckAndAddDataForOrigin(noteData, dataOrigin)) 
                    {
                        isSucceed_ = true;
                        break;
                    }
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

    public class ChartImporter
    {
        // ここに変換関数を記述していく
        private List<INoteDataConvertable> converters = new List<INoteDataConvertable>()
        {
            new TouchNoteConverter(),
            new DynamicUpwardConverter(),
            new DynamicRightwardConverter(),
            new DynamicLeftwardConverter(),
        };

        public ChartData Import(ChartDataOrigin dataOrigin, INoteSpawnDataOptionHolder optionHolder)
        {
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
                if (!converter.CheckAndAddDataFromOrigin(dataOrigin, chartData, secondsPassed))
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
    public interface INoteDataConvertable
    {
        bool CheckAndAddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);

        bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing);
    }

    /// <summary>
    /// タッチノーツ
    /// </summary>
    public class TouchNoteConverter : INoteDataConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.TouchNote;

        public bool CheckAndAddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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

        public bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
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
    }

    /// <summary>
    /// ↑ダイナミック↑ノーツ
    /// </summary>
    public class DynamicUpwardConverter : INoteDataConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundUpward;

        public bool CheckAndAddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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

        public bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
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
    }

    /// <summary>
    /// →ダイナミック→ノーツ
    /// </summary>
    public class DynamicRightwardConverter : INoteDataConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundRightward;

        public bool CheckAndAddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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

        public bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
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
    }

    /// <summary>
    /// ←ダイナミック←ノーツ
    /// </summary>
    public class DynamicLeftwardConverter : INoteDataConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundLeftward;

        public bool CheckAndAddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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

        public bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
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
    }

    /// <summary>
    /// ホールドノーツ
    /// </summary>
    public class HoldConverter : INoteDataConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.Hold;

        public bool CheckAndAddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldData == null)
            {
                dataOrigin.HoldData = new List<NoteDataOrigin_Hold>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_Hold data = new NoteDataOrigin_Hold()
            {
                // ここにNumber処理を記述;
                //HoldNumber = ,
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.HoldData.Add(data);
            return true;
        }

        public bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldData == null) { return true; }

            //foreach (var noteOrigin in dataOrigin.HoldData)
            //{
            //    NoteData_Hold noteData = new NoteData_DynamicGroundLeftward
            //    {
            //        Range = (int[])noteOrigin.Range.Clone(),
            //        Timing = timing
            //    };

            //    chartData.AddNoteData(noteData);
            //}

            return true;
        }
    }

    #endregion
}
