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
        const int SPACE_LOCATION_INDEX = 100;

        public SubDivisionDataInBeat(float bpm, int barIndex, int subIndex)
        {
            SetBpm(bpm);
            BarIndex = barIndex;
            SubDivisionIndex = subIndex;
        }

        public int BarIndex { get; }
        public int SubDivisionIndex { get; }

        // Ground配置場所
        public Transform[] PlacementLocation { private get; set; }
        public void SetPlacementLocation(Transform[] locates)
        {
            PlacementLocation = locates;
        }

        // 宙配置場所
        public Transform SpaceLocation { private get; set; }
        public void SetSpaceLocation(Transform locate)
        {
            SpaceLocation = locate;
        }

        #region ノーツデータ

        /// <summary>
        /// その分線に配置されたノーツのデータ
        /// </summary>
        ReactiveCollection<IDeployableNoteData> noteDatas = new ReactiveCollection<IDeployableNoteData>();

        /// <summary>
        /// ノーツの追加、削除の監視用
        /// </summary>
        public IReadOnlyReactiveCollection<IDeployableNoteData> NoteDatas => noteDatas;

        public void AddNote(IDeployableNoteData noteData)
        {
            noteDatas.Add(noteData);
        }

        public bool RemoveNote(IDeployableNoteData noteData)
        {
            return noteDatas.Remove(noteData);
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            // 宙配置場所
            if (address.SliderIndex == SPACE_LOCATION_INDEX)
            {
                return SpaceLocation;
            }

            // グラウンド配置場所
            if (address.SliderIndex > 15)
            {
                Debug.LogError($"【System】値が15を超えています: {address.SliderIndex}");
                return null;
            }

            return PlacementLocation[(int)address.SliderIndex];
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

        public int barIndex;

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
            if (address.SubDivisionIndex >= subDivisionDatas.Count)
            {
                // Debug.LogError($"【System】値が分線の数を超えています: {address.SubDivisionIndex}/{subDivisionDatas.Count}");
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
            if (this.beatCount.Value == beatCount) { return; }

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
        const int DEFAULT_BEAT_COUNT = 4;
        const float DEFAULT_BEAT_UNIT = 4;
        const int DEFAULT_DIVISION_NUM = 4;
        const float DEFAULT_BPM = 256;

        public ChartData(int barNum)
        {
            for (int i = 0; i < barNum; i++)
            {
                BarDataInChart barData = new BarDataInChart(DEFAULT_BEAT_COUNT, DEFAULT_BEAT_UNIT, DEFAULT_DIVISION_NUM, DEFAULT_BPM, i);
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
            if (barDatas == null) { return; }

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
        /// 小節線の追加
        /// </summary>
        /// <param name="length"></param>
        public void AddBar(int length)
        {
            float bpm = DEFAULT_BPM;
            int barDataCount = BarDatas.Count;

            if (BarDatas.Count > 0) { bpm = BarDatas.Last().SubDivisionDatas.Last().Bpm.Value; }

            for (int i = 0; i < length; i++)
            {
                BarDataInChart barData = new BarDataInChart(DEFAULT_BEAT_COUNT, DEFAULT_BEAT_UNIT, DEFAULT_DIVISION_NUM, bpm, barDataCount + i);
                barDatas.Add(barData);
            }
        }

        /// <summary>
        /// 小節線の削除
        /// </summary>
        /// <param name="length"></param>
        public void RemoveBar(int length)
        {
            for (int i = 0; i < Mathf.Min(length, BarDatas.Count); i++)
            {
                barDatas.RemoveAt(barDatas.Count - 1);
            }
        }

        /// <summary>
        /// ノーツを追加する
        /// </summary>
        public void AddNote(IDeployableNoteData noteData)
        {
            // 新たな場所に追加
            var address = noteData.Address;
            var newSubDivision = BarDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            Debug.Log($"【配置】:\n #{address.BarIndex} - {address.SubDivisionIndex} - ({address.Range[0]}~{address.Range[^1]})");
        }

        /// <summary>
        /// ノーツを削除する
        /// </summary>
        public void RemoveNote(IDeployableNoteData noteData)
        {
            var address = new AddressInChart(noteData.Address);
            var subDivision = barDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];

            if (!subDivision.RemoveNote(noteData))
            {
                Debug.LogError($"【削除】削除に失敗しました:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
            }
            else
            {
                Debug.Log($"【削除】:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
            }
        }

        /// <summary>
        /// ノーツの場所を移動させる
        /// </summary>
        /// <param name="noteData"></param>
        /// <param name="newAddress"></param>
        /// <returns></returns>
        public bool ChangeNoteAddress(IDeployableNoteData noteData, AddressInChart oldAddress, AddressInChart newAddress)
        {
            // 古い場所から削除
            Debug.Log(oldAddress + ", " + newAddress);
            var oldSubDivision = BarDatas[oldAddress.BarIndex].SubDivisionDatas[oldAddress.SubDivisionIndex];
            if (!oldSubDivision.RemoveNote(noteData))
            {
                Debug.LogError($"【移動】削除に失敗しました\n 該当するノーツが見つかりません");
                return false;
            }

            var newSubDivision = BarDatas[newAddress.BarIndex].SubDivisionDatas[newAddress.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            return true;
        }

        /// <summary>
        /// アドレス間の距離(分線の数)を返す
        /// </summary>
        /// <param name="addressA"></param>
        /// <param name="AddressB"></param>
        /// <returns></returns>
        public int GetAddressDelta(AddressInChart targetAddress, AddressInChart baseAddress)
        {
            AddressInChart former = new AddressInChart(targetAddress.IsEarlierThan(baseAddress) ? targetAddress : baseAddress);
            AddressInChart latter = new AddressInChart(targetAddress.IsEarlierThan(baseAddress) ? baseAddress : targetAddress);

            int delta = 0;
            for (int i = former.BarIndex; i < BarDatas.Count; i++)
            {
                var barData = BarDatas[i];

                if (i != latter.BarIndex) 
                { 
                    delta += barData.SubDivisionDatas.Count - former.SubDivisionIndex;
                    former.SetSubDivisionIndex(0);
                }
                else 
                {
                    delta += latter.SubDivisionIndex - former.SubDivisionIndex;
                    break;
                }
            }

            return targetAddress.IsEarlierThan(baseAddress) ? -delta : delta;
        }

        /// <summary>
        /// アドレスに分線数を足す
        /// </summary>
        /// <param name="address"></param>
        /// <param name="delta"></param>
        public AddressInChart AddressAddition(AddressInChart address, int delta)
        {
            var copy = new AddressInChart(address);

            if(delta >= 0)
            {
                for (int i = copy.BarIndex; i < BarDatas.Count; i++)
                {
                    var barData = BarDatas[i];

                    // 0になったら終わり
                    if (delta == 0) { break; }

                    // 小節1個越えるとき
                    if (barData.SubDivisionDatas.Count <= copy.SubDivisionIndex + delta)
                    {
                        delta -= barData.SubDivisionDatas.Count - copy.SubDivisionIndex;

                        copy.SetBarIndex(copy.BarIndex + 1);
                        copy.SetSubDivisionIndex(0);
                    }
                    // この小節にあるとき
                    else
                    {
                        copy.SetSubDivisionIndex(copy.SubDivisionIndex + delta);
                        break;
                    }
                }
            }
            else
            {
                for (int i = copy.BarIndex; i < BarDatas.Count; i--)
                {
                    var barData = BarDatas[i];
                    var barDaraBack = BarDatas[i - 1];

                    // 0になったら終わり
                    if (delta == 0) { break; }

                    // 小節1個越えるとき
                    if (copy.SubDivisionIndex + delta < 0)
                    {
                        delta += copy.SubDivisionIndex;

                        copy.SetBarIndex(copy.BarIndex - 1);
                        copy.SetSubDivisionIndex(barDaraBack.SubDivisionDatas.Count - 1);
                    }
                    // この小節にあるとき
                    else
                    {
                        copy.SetSubDivisionIndex(copy.SubDivisionIndex + delta);
                        break;
                    }
                }

            }
            
            return copy;
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if (address.BarIndex >= barDatas.Count)
            {
                // Debug.LogError($"【System】値が小節線の数を超えています: {address.BarIndex}");
                return null;
            }

            return barDatas[address.BarIndex].GetPlacementLocation(address);
        }
    }
}