using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using JsonUtil;

namespace ChartConvert
{
    public class ChartExporter
    {
        // ここに変換関数を記述していく
        private List<INoteDataConvertableToOrigin> converters = new List<INoteDataConvertableToOrigin>()
        {
            new TouchNoteConverter(),
            new DynamicUpwardConverter(),
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
                    if (converter.CheckAndAddData(noteData, dataOrigin)) 
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


    #region ノーツ変換関数

    /// <summary>
    /// ChartEditor.NoteDataを変換してSubDivisionDataOriginにぶち込む
    /// </summary>
    public interface INoteDataConvertableToOrigin
    {
        bool CheckAndAddData(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);
    }

    /// <summary>
    /// タッチノーツ
    /// </summary>
    public class TouchNoteConverter : INoteDataConvertableToOrigin
    {
        DeploymentNoteType type = DeploymentNoteType.TouchNote;

        public bool CheckAndAddData(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
    }

    /// <summary>
    /// ↑ダイナミック↑ノーツ
    /// </summary>
    public class DynamicUpwardConverter : INoteDataConvertableToOrigin
    {
        DeploymentNoteType type = DeploymentNoteType.DynamicGroundUpward;

        public bool CheckAndAddData(NoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
    }

    #endregion
}
