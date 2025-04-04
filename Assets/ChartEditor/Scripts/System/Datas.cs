using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    /// <summary>
    /// 配置ノーツデータ
    /// </summary>
    public class NoteData
    {
        /// <summary>
        /// 配置範囲 (基本0～15)
        /// </summary>
        ReactiveCollection<float> range = new ReactiveCollection<float>();

        /// <summary>
        /// ノーツの移動、拡大縮小の監視
        /// </summary>
        public IReadOnlyReactiveCollection<float> Range { get { return range; } }

        public void SetRange(List<float> range)
        {
            this.range = new ReactiveCollection<float>(range);
        }

        public void AddRange(bool isAddLast)
        {
            float value = isAddLast ? range.Last() + 1 : range[0] - 1;
            range.Insert(isAddLast ? range.Count : 0, value);
        }
    }

    /// <summary>
    /// 分線のデータ
    /// </summary>
    public class SubDivisionDataInBeat
    {
        public SubDivisionDataInBeat(float bpm)
        {
            SetBpm(bpm);
        }

        #region ノーツデータ

        /// <summary>
        /// その分線に配置されたノーツのデータ
        /// </summary>
        ReactiveCollection<NoteData> noteDatas = new ReactiveCollection<NoteData>();

        /// <summary>
        /// ノーツの追加、削除の監視用
        /// </summary>
        public IReadOnlyReactiveCollection<NoteData> NoteDatas => noteDatas;

        public void AddNote(NoteData noteData)
        {
            noteDatas.Add(noteData);
        }

        public bool RemoveNote(NoteData noteData)
        {
            return noteDatas.Remove(noteData);
        }

        #endregion

        #region BPM

        const float BPM_MIN = 1f;
        const float BPM_MAX = 1000f;

        /// <summary>
        /// BPM変化
        /// </summary>
        ReactiveProperty<float> bpm = new ReactiveProperty<float>();

        public IReadOnlyReactiveProperty<float> Bpm => bpm;

        public void SetBpm(float bpm)
        {
            this.bpm.Value = Mathf.Clamp(bpm, BPM_MIN, BPM_MAX);
        }
        
        #endregion
    }

    /// <summary>
    /// 小節のデータ
    /// </summary>
    public class BarDataInChart
    {
        public BarDataInChart(int beatCount, float beatUnit, int divNum, float bpm)
        {
            this.beatCount.Value = beatCount;
            this.beatUnit.Value = beatUnit;
            this.divisionNum.Value = divNum;
            UpdateSubDivisionData(beatCount, beatUnit, divNum, bpm);
        }

        /// <summary>
        /// 小節内の分線データ
        /// </summary>
        ReactiveCollection<SubDivisionDataInBeat> subDivisionDatas = new ReactiveCollection<SubDivisionDataInBeat>();

        /// <summary>
        /// 分線の代入、クリアの監視
        /// </summary>
        public IReadOnlyReactiveCollection<SubDivisionDataInBeat> SubDivisionDatas => subDivisionDatas;

        public void SetSubDivisionDatas(List<SubDivisionDataInBeat> subDivisionDatas)
        {
            this.subDivisionDatas.Clear();
            this.subDivisionDatas = new ReactiveCollection<SubDivisionDataInBeat>(subDivisionDatas); 
        }

        /// <summary>
        /// 分線の数の更新
        /// </summary>
        /// <param name="beatCount">n分の</param>
        /// <param name="beatUnit">m拍子</param>
        private void UpdateSubDivisionData(int beatCount, float beatUnit, int divNum, float bpm = -1)
        {
            if (beatCount <= 0) { return; }
            if (beatUnit <= 0) { return; }
            if (divNum <= 0) { return; }
            if (bpm == -1 && this.subDivisionDatas.Count == 0) { return; }

            // bpmは以前のやつを使う
            if (bpm == -1) { bpm = this.subDivisionDatas[0].Bpm.Value; }
            List<SubDivisionDataInBeat> subDivisionDatas = new List<SubDivisionDataInBeat>();

            // カウント数 * 分割数が分線の数
            for (int i = 0; i < beatCount * divNum; i++)
            {
                subDivisionDatas.Add(new SubDivisionDataInBeat(bpm));
            }

            // データセット
            SetSubDivisionDatas(subDivisionDatas);
        }

        #region その他データ

        /// <summary>
        /// n/m拍子のn
        /// </summary>
        ReactiveProperty<int> beatCount = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> BeatCount => beatCount;

        public void SetBeatCount(int beatCount) 
        {
            beatCount = (int)Mathf.Clamp(beatCount, 0, float.MaxValue);
            UpdateSubDivisionData(beatCount, this.beatUnit.Value, this.divisionNum.Value);
            this.beatCount.Value = beatCount;
        }

        /// <summary>
        /// n/m拍子のm
        /// </summary>
        ReactiveProperty<float> beatUnit = new ReactiveProperty<float>();

        public IReadOnlyReactiveProperty<float> BeatUnit => beatUnit;

        public void SetBeatUnit(float beatUnit)
        {
            beatUnit = Mathf.Clamp(beatUnit, 0, float.MaxValue);
            UpdateSubDivisionData(this.beatCount.Value, beatUnit, this.divisionNum.Value);
            this.beatUnit.Value = beatUnit;
        }

        /// <summary>
        /// 分割数 (〇分割)
        /// </summary>
        ReactiveProperty<int> divisionNum = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> DivisionNum => divisionNum;

        public void SetDivisionNum(int divisionNum)
        {
            divisionNum = (int)Mathf.Clamp(divisionNum, 0, float.MaxValue);
            UpdateSubDivisionData(this.beatCount.Value, this.beatUnit.Value, divisionNum);
            this.divisionNum.Value = divisionNum;
        }

        #endregion
    }

    /// <summary>
    /// エディタ用譜面データ
    /// </summary>
    public class ChartData
    {
        public ChartData(float musicLength, float bpm, int beatCount = 4, float beatUnit = 4, int divNum = 2)
        {
            // 全体の小節数 = 曲の長さ[min] / 小節数[回/min]
            //              = (曲の長さ[sec] / 60f) * (bpm[回/min] / 小節内のビート数)
            float beatNum = (musicLength / 60f) * (bpm / beatCount);

            for(int i = 0; i < beatNum; i++)
            {
                BarDataInChart barData = new BarDataInChart(beatCount, beatUnit, divNum, bpm);
                barDatas.Add(barData);
            }
        }

        ReactiveCollection<BarDataInChart> barDatas = new ReactiveCollection<BarDataInChart>();

        /// <summary>
        /// 譜面内の全小節データ
        /// </summary>
        public IReadOnlyReactiveCollection<BarDataInChart> BarDatas => barDatas;
    }

    /// <summary>
    /// エディットモード一覧
    /// </summary>
    public enum EditMode
    {
        None,
        Deploy,
        Destroy,
        Move,
        Scale,
    }

    /// <summary>
    /// 音楽再生モード
    /// </summary>
    public enum PlayMode
    {
        Stop,
        Play,
    }

    /// <summary>
    /// 配置ノーツ一覧
    /// </summary>
    public enum DeploymentNoteType
    {
        TouchNote,
        DynamicNoteUpward,
        DynamicNoteRightward,
        DynamicNoteLeftward,
    }
}
