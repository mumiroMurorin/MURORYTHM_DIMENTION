using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    /// <summary>
    /// 分線のデータ
    /// </summary>
    public class SubDivisionDataInBeat
    {
        public SubDivisionDataInBeat(float bpm, int barIndex, int subIndex)
        {
            SetBpm(bpm);
            BarIndex = barIndex;
            SubDivisionIndex = subIndex;
        }

        public int BarIndex { get; }
        public int SubDivisionIndex { get; }

        public Transform[] PlacementLocation { private get; set; }

        #region ノーツデータ

        /// <summary>
        /// その分線に配置されたノーツのデータ
        /// </summary>
        ReactiveCollection<IGroundNoteData> noteDatas = new ReactiveCollection<IGroundNoteData>();

        /// <summary>
        /// ノーツの追加、削除の監視用
        /// </summary>
        public IReadOnlyReactiveCollection<IGroundNoteData> NoteDatas => noteDatas;

        public void AddNote(IGroundNoteData noteData)
        {
            noteDatas.Add(noteData);
        }

        public bool RemoveNote(IGroundNoteData noteData)
        {
            return noteDatas.Remove(noteData);
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if(address.SliderIndex > 15) {
                Debug.LogError($"【System】値が15を超えています: {address.SliderIndex}");
                return null; 
            }

            return PlacementLocation[(int)address.SliderIndex];
        }

        public void SetPlacementLocation(Transform[] locates)
        {
            PlacementLocation = locates;
        }

        #endregion

        #region BPM

        const float BPM_MIN = 1f;
        const float BPM_MAX = 500f;

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
        public BarDataInChart(int beatCount, float beatUnit, int divNum, float bpm, int barIndex)
        {
            this.beatCount.Value = beatCount;
            this.beatUnit.Value = beatUnit;
            this.divisionNum.Value = divNum;
            this.barIndex = barIndex;

            UpdateSubDivisionData(beatCount, beatUnit, divNum, bpm);
        }

        int barIndex;

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
            foreach (var sub in subDivisionDatas)
            {
                this.subDivisionDatas.Add(sub);
            }
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
                subDivisionDatas.Add(new SubDivisionDataInBeat(bpm, barIndex, i));
            }

            // データセット
            SetSubDivisionDatas(subDivisionDatas);
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if (address.SubDivisionIndex > subDivisionDatas.Count)
            {
                Debug.LogError($"【System】値が分線の数を超えています: {address.SubDivisionIndex}");
                return null;
            }

            return subDivisionDatas[address.SubDivisionIndex].GetPlacementLocation(address);
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
            if(this.beatCount.Value == beatCount) { return; }

            this.beatCount.Value = beatCount;
            UpdateSubDivisionData(beatCount, this.beatUnit.Value, this.divisionNum.Value);
        }

        /// <summary>
        /// n/m拍子のm
        /// </summary>
        ReactiveProperty<float> beatUnit = new ReactiveProperty<float>();

        public IReadOnlyReactiveProperty<float> BeatUnit => beatUnit;

        public void SetBeatUnit(float beatUnit)
        {
            beatUnit = Mathf.Clamp(beatUnit, 0, float.MaxValue);
            if (this.beatUnit.Value == beatUnit) { return; }

            this.beatUnit.Value = beatUnit;
            //UpdateSubDivisionData(this.beatCount.Value, beatUnit, this.divisionNum.Value);
        }

        /// <summary>
        /// 分割数 (〇分割)
        /// </summary>
        ReactiveProperty<int> divisionNum = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> DivisionNum => divisionNum;

        public void SetDivisionNum(int divisionNum)
        {
            divisionNum = (int)Mathf.Clamp(divisionNum, 0, float.MaxValue);
            if (this.divisionNum.Value == divisionNum) { return; }

            this.divisionNum.Value = divisionNum;
            UpdateSubDivisionData(this.beatCount.Value, this.beatUnit.Value, divisionNum);
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
                BarDataInChart barData = new BarDataInChart(beatCount, beatUnit, divNum, bpm, i);
                barDatas.Add(barData);
            }
        }

        ReactiveCollection<BarDataInChart> barDatas = new ReactiveCollection<BarDataInChart>();

        /// <summary>
        /// 譜面内の全小節データ
        /// </summary>
        public IReadOnlyReactiveCollection<BarDataInChart> BarDatas => barDatas;

        /// <summary>
        /// 特定の分線から後のBPMを一括で変更する
        /// </summary>
        /// <param name="subDivisionData"></param>
        public void SetBPMFromSubDivisionUnit(SubDivisionDataInBeat findData, float bpm)
        {
            if(barDatas == null) { return; }

            bool isFound = false;

            // 特定のデータをしらみつぶしに探す
            foreach (var bar in barDatas)
            {
                foreach (var sub in bar.SubDivisionDatas)
                {
                    // 見つかったらフラグオン
                    if (sub == findData) { isFound = true; }

                    // フラグオンならBPM変更
                    if (isFound) { sub.SetBpm(bpm); }
                }
            }
        }

        /// <summary>
        /// ノーツを追加する
        /// </summary>
        public void AddNote(IGroundNoteData noteData, AddressInChart address)
        {
            // 新たな場所に追加
            SubDivisionDataInBeat newSubDivision = BarDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            noteData.SetAddress(address);
            LogUI.Instance.Log($"【配置】:\n #{address.BarIndex} {address.SubDivisionIndex} {address.SliderIndex}");
        }

        /// <summary>
        /// ノーツを削除する
        /// </summary>
        public void RemoveNote(IGroundNoteData noteData)
        {
            AddressInChart address = noteData.Address;
            SubDivisionDataInBeat subDivision = barDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];

            if (!subDivision.RemoveNote(noteData))
            {
                LogUI.Instance.LogError($"【削除】削除に失敗しました:\n #{address.BarIndex} {address.SubDivisionIndex} {address.SliderIndex}");
            }
            else
            {
                LogUI.Instance.Log($"【削除】:\n #{address.BarIndex} {address.SubDivisionIndex} {address.SliderIndex}");
            }
        }

        /// <summary>
        /// ノーツの場所を移動させる
        /// </summary>
        /// <param name="noteData"></param>
        /// <param name="newAddress"></param>
        /// <returns></returns>
        public bool ChangeNoteAddress(IGroundNoteData noteData, AddressInChart newAddress)
        {
            AddressInChart oldAddress = noteData.Address;

            // 古い場所から削除
            SubDivisionDataInBeat oldSubDivision = BarDatas[oldAddress.BarIndex].SubDivisionDatas[oldAddress.SubDivisionIndex];
            if (!oldSubDivision.RemoveNote(noteData)) 
            {
                LogUI.Instance.LogError($"【移動】削除に失敗しました\n 該当するノーツが見つかりません");
                return false; 
            }

            // 新たな場所に追加
            SubDivisionDataInBeat newSubDivision = BarDatas[newAddress.BarIndex].SubDivisionDatas[newAddress.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            noteData.SetAddress(newAddress);
            return true;
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if (address.BarIndex > barDatas.Count)
            {
                Debug.LogError($"【System】値が小節線の数を超えています: {address.BarIndex}");
                return null;
            }

            return barDatas[address.BarIndex].GetPlacementLocation(address);
        }
    }

    /// <summary>
    /// 譜面中の「小節番号」「分節番号」「スライダーインデックス」をまとめたクラス
    /// </summary>
    public class AddressInChart
    {
        public AddressInChart(int barIndex = 0, int subDivisionIndex = 0, float sliderIndex = 0)
        {
            this.barIndex = new ReactiveProperty<int>(barIndex);
            this.subDivisionIndex = new ReactiveProperty<int>(subDivisionIndex);
            this.sliderIndex = new ReactiveProperty<float>(sliderIndex);
        }


        /// <summary>
        /// 小節線番号
        /// </summary>
        ReactiveProperty<int> barIndex;
        public int BarIndex { get { return barIndex.Value; } }
        public IReadOnlyReactiveProperty<int> BarIndexRP { get { return barIndex; } }
        public void SetBarIndex(int index)
        {
            barIndex.Value = index;
        }


        /// <summary>
        /// 分節番号
        /// </summary>
        ReactiveProperty<int> subDivisionIndex;
        public int SubDivisionIndex { get { return subDivisionIndex.Value; } }
        public IReadOnlyReactiveProperty<int> SubDivisionIndexRP { get { return subDivisionIndex; } }
        public void SetSubDivisionIndex(int index)
        {
            subDivisionIndex.Value = index;
        }


        /// <summary>
        /// スライダー番号
        /// </summary>
        ReactiveProperty<float> sliderIndex;
        public float SliderIndex { get { return sliderIndex.Value; } }
        public IReadOnlyReactiveProperty<float> SliderIndexRP { get { return sliderIndex; } }
        public void SetSliderIndex(float index)
        {
            sliderIndex.Value = index;
        }

        public AddressInChart Copy()
        {
            return new AddressInChart(this.barIndex.Value, this.subDivisionIndex.Value, this.sliderIndex.Value);
        }

        public void SetSameAddress(AddressInChart address)
        {
            this.barIndex.Value = address.BarIndex;
            this.subDivisionIndex.Value = address.SubDivisionIndex;
            this.sliderIndex.Value = address.SliderIndex;
        }
        
        /// <summary>
        /// どちらが先のアドレスか返す
        /// 引数のほうが遅ければTrue
        /// </summary>
        /// <param name="address"></param>
        public bool IsEarlierThan(AddressInChart address)
        {
            // 違う小節番号の場合
            if(this.barIndex.Value < address.barIndex.Value) { return true; }
            else if(this.barIndex.Value > address.barIndex.Value) { return false; }

            // 同じ小節番号の場合、分節番号で判断
            if (this.subDivisionIndex.Value < address.subDivisionIndex.Value) { return true; }
            else if (this.subDivisionIndex.Value > address.subDivisionIndex.Value) { return false; }

            // 全く同じ場合falseを返す
            return false;
        }

        /// <summary>
        /// 同じアドレスか調べる
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool IsSameAddress(AddressInChart address)
        {
            return address.BarIndex == this.BarIndex && address.SubDivisionIndex == this.SubDivisionIndex && address.SliderIndex == this.SliderIndex;
        }
    }

    /// <summary>
    /// エディットモード一覧
    /// </summary>
    public enum EditMode
    {
        None,
        EditingConfig,
        Deploy,
        Destroy,
        Move,
        Scale,
        Connect,
        Connecting,
        ChangeType,
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
        DynamicGroundUpward,
        DynamicGroundRightward,
        DynamicGroundLeftward,
        Hold,
        HoldHidden,
    }
}
