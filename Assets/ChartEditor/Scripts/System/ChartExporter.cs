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
                float timing = calcTiming.CalcNextTiming(beatUnit, divNum, bpm);

                if (!SetDataFromSubDivisionData(sub, chartData, timing)) 
                {
                    isSucceed = false;
                }
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

            public CalcTimingClass(float musicOffsetMs, float optionOffsetMs)
            {
                timePassedSec = -(musicOffsetMs + optionOffsetMs) / 1000f;
            }

            public float CalcNextTiming(float beatUnit, float divNum, float bpm)
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


    #region ノーツ変換関数

    /// <summary>
    /// ChartEditor.NoteDataを変換してSubDivisionDataOriginにぶち込む
    /// </summary>
    public interface INoteDataConvertable
    {
        bool CheckAndAddDataForOrigin(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);

        bool CheckAndAddDataFromOrigin(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing);
    }

    /// <summary>
    /// タッチノーツ
    /// </summary>
    public class TouchNoteConverter : INoteDataConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.TouchNote;

        public bool CheckAndAddDataForOrigin(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
            if(chartData.noteData_Touches == null)
            {
                chartData.noteData_Touches = new List<NoteData_Touch>();
            }

            if(dataOrigin.TouchNoteData == null) { return true; }

            foreach(var noteOrigin in dataOrigin.TouchNoteData)
            {
                NoteData_Touch noteData = new NoteData_Touch
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.noteData_Touches.Add(noteData);
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

        public bool CheckAndAddDataForOrigin(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
            if (chartData.noteData_DynamicGroundUpwards == null)
            {
                chartData.noteData_DynamicGroundUpwards = new List<NoteData_DynamicGroundUpward>();
            }

            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicUpwardData)
            {
                NoteData_DynamicGroundUpward noteData = new NoteData_DynamicGroundUpward
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.noteData_DynamicGroundUpwards.Add(noteData);
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

        public bool CheckAndAddDataForOrigin(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
            if (chartData.noteData_DynamicGroundRightwards == null)
            {
                chartData.noteData_DynamicGroundRightwards = new List<NoteData_DynamicGroundRightward>();
            }

            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicRightwardData)
            {
                NoteData_DynamicGroundRightward noteData = new NoteData_DynamicGroundRightward
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.noteData_DynamicGroundRightwards.Add(noteData);
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

        public bool CheckAndAddDataForOrigin(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
            if (chartData.noteData_DynamicGroundLeftwards == null)
            {
                chartData.noteData_DynamicGroundLeftwards = new List<NoteData_DynamicGroundLeftward>();
            }

            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicLeftwardData)
            {
                NoteData_DynamicGroundLeftward noteData = new NoteData_DynamicGroundLeftward
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.noteData_DynamicGroundLeftwards.Add(noteData);
            }

            return true;
        }
    }

    #endregion
}
