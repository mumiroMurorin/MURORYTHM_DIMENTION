using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using System;

namespace ChartEditor
{
    /// <summary>
    /// 分線のデータ
    /// </summary>
    public class SubDivisionDataInBeat : ISubDivisionDataGetter
    {
        const int SPACE_LOCATION_INDEX = 100;

        public SubDivisionDataInBeat(SubdivisionConfig config, IBarDataGetter barData, int subIndex)
        {
            SetBpm(config.Bpm);
            SetSpeedRatio(config.SpeedRatio);
            this.barData = barData;
            SubDivisionIndex = subIndex;
        }

        IBarDataGetter barData;
        public IBarDataGetter BarData { get { return barData; } }

        public int SubDivisionIndex { get; }
        public SubdivisionConfig SubConfig { get { return new SubdivisionConfig(this.bpm.Value, this.speedRatio.Value); } }

        // Ground配置場所
        Transform[] placementLocation;
        public void SetPlacementLocation(Transform[] locates)
        {
            placementLocation = locates;
        }

        // 宙配置場所
        Transform spaceLocation;
        public void SetSpaceLocation(Transform locate)
        {
            spaceLocation = locate;
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
                return spaceLocation;
            }

            // グラウンド配置場所
            if (address.SliderIndex > 15)
            {
                Debug.LogError($"【System】値が15を超えています: {address.SliderIndex}");
                return null;
            }

            return placementLocation[(int)address.SliderIndex];
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


        #region SpeedRatio

        // (以前は -100 〜 100。もっと速く流したい、という要望で、-200 〜 200 にした)
        const float SPEEDRATIO_MIN = -200f;
        const float SPEEDRATIO_MAX = 200f;

        /// <summary>
        /// スピード倍率変化
        /// </summary>
        ReactiveProperty<float> speedRatio = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> SpeedRatio => speedRatio;
        public void SetSpeedRatio(float speedRatio)
        {
            this.speedRatio.Value = Mathf.Clamp(speedRatio, SPEEDRATIO_MIN, SPEEDRATIO_MAX);
        }

        #endregion
    }

    /// <summary>
    /// 小節のデータ
    /// </summary>
    public class BarDataInChart : IBarDataGetter
    {
        public BarDataInChart(BarConfig barConfig, SubdivisionConfig subConfig, int barIndex)
        {
            this.beatCount.Value = barConfig.BeatCount;
            this.beatUnit.Value = barConfig.BeatUnit;
            this.divisionNum.Value = barConfig.DivisionNum;
            this.BarIndex = barIndex;

            UpdateSubDivisionData(barConfig.BeatCount, barConfig.DivisionNum, barConfig.DivisionNum, subConfig);
        }

        public int BarIndex { get; set; }

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
            for (int i = this.subDivisionDatas.Count - 1; i >= 0; i--)
            {
                this.subDivisionDatas.RemoveAt(this.subDivisionDatas.Count - 1);
            }

            foreach (var sub in subDivisionDatas)
            {
                this.subDivisionDatas.Add(sub);
            }
        }
        public void RemoveSubDivisionData(int index)
        {
            subDivisionDatas.RemoveAt(index);
        }

        /// <summary>
        /// 分線の数の更新
        /// </summary>
        /// <param name="beatCount">n分の</param>
        /// <param name="beatUnit">m拍子</param>
        /// <param name="previousDivNum">作り直す前の分割数 (作り直す前の分線が、拍のどの位置にあったかを知るのに使う)</param>
        private void UpdateSubDivisionData(int beatCount, int divNum, int previousDivNum, SubdivisionConfig subConfigOrigin = null)
        {
            if (beatCount <= 0) { return; }
            if (divNum <= 0) { return; }
            if (subConfigOrigin == null && this.subDivisionDatas.Count == 0) { return; }

            // 作り直す前の、分線ごとの設定 (BPM・スピード倍率)
            var previousConfigs = new List<SubdivisionConfig>(this.subDivisionDatas.Count);
            foreach (var sub in this.subDivisionDatas) { previousConfigs.Add(sub.SubConfig); }

            // 新規セット
            // カウント数 * 分割数が分線の数
            var subDivisionDatas = new List<SubDivisionDataInBeat>();
            for (int i = 0; i < beatCount * divNum; i++)
            {
                var subConfig = new SubdivisionConfig(GetConfigForNewSubDivision(i, divNum, previousDivNum, previousConfigs, subConfigOrigin));
                subDivisionDatas.Add(new SubDivisionDataInBeat(subConfig, this, i));
            }
            SetSubDivisionDatas(subDivisionDatas);
        }

        /// <summary>
        /// 作り直した後の分線に入れる設定 (BPM・スピード倍率) を決める
        ///
        /// 小節の途中でBPMやスピード倍率が変わっていることがあるので、作り直す前に「同じ拍の同じ位置」にあった分線
        /// (ちょうどの位置に無ければ、その直前の分線) の値を引き継ぐ。
        /// 全部の分線に先頭の分線の値を入れると、小節の途中の変わり目が消えてしまう。
        /// 拍数が増えて、前には無かった位置にできた分線は、最後の分線の値を引き継ぐ。
        /// </summary>
        /// <param name="index">作り直した後の、分線の番号</param>
        /// <param name="configOrigin">小節を最初に作るときの設定。作り直しのときは null</param>
        private static SubdivisionConfig GetConfigForNewSubDivision(int index, int divNum, int previousDivNum, List<SubdivisionConfig> previousConfigs, SubdivisionConfig configOrigin)
        {
            // 小節を最初に作るとき
            if (configOrigin != null || previousConfigs.Count == 0) { return configOrigin; }

            int previousIndex = previousDivNum > 0 ? (int)((long)index * previousDivNum / divNum) : index;
            previousIndex = Mathf.Clamp(previousIndex, 0, previousConfigs.Count - 1);

            return previousConfigs[previousIndex];
        }

        public Transform GetPlacementLocation(AddressInChart address)
        {
            if (address.SubDivisionIndex >= subDivisionDatas.Count)
            {
                return null;
            }

            return subDivisionDatas[address.SubDivisionIndex].GetPlacementLocation(address);
        }

        #region その他データ

        public BarConfig BarConfig { get { return new BarConfig(this.beatCount.Value, this.beatUnit.Value, this.divisionNum.Value); } }

        /// <summary>
        /// n/m拍子のn
        /// </summary>
        ReactiveProperty<int> beatCount = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> BeatCount => beatCount;
        public void SetBeatCount(int beatCount)
        {
            // 1未満は分線が作れないので受け付けない
            if (beatCount < 1) { return; }
            if (this.beatCount.Value == beatCount) { return; }

            this.beatCount.Value = beatCount;
            UpdateSubDivisionData(beatCount, this.divisionNum.Value, this.divisionNum.Value);
        }

        /// <summary>
        /// n/m拍子のm
        /// </summary>
        ReactiveProperty<float> beatUnit = new ReactiveProperty<float>();
        public IReadOnlyReactiveProperty<float> BeatUnit => beatUnit;
        public void SetBeatUnit(float beatUnit)
        {
            // 0以下は分線の長さが計算できない(0除算になる)ので受け付けない
            if (!(beatUnit > 0f) || float.IsInfinity(beatUnit)) { return; }
            if (this.beatUnit.Value == beatUnit) { return; }

            this.beatUnit.Value = beatUnit;
        }

        /// <summary>
        /// 分割数 (〇分割)
        /// </summary>
        ReactiveProperty<int> divisionNum = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> DivisionNum => divisionNum;
        public void SetDivisionNum(int divisionNum)
        {
            // 1未満は分線が作れないので受け付けない
            if (divisionNum < 1) { return; }
            if (this.divisionNum.Value == divisionNum) { return; }

            int previousDivNum = this.divisionNum.Value;
            this.divisionNum.Value = divisionNum;
            UpdateSubDivisionData(this.beatCount.Value, divisionNum, previousDivNum);
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
        const float DEFAULT_SPEEDRATIO = 1f;

        public Action<IDeployableNoteData> OnAddNoteListener { get; set; }
        public Action<IDeployableNoteData> OnRemoveNoteListener { get; set; }

        public ChartData(int barNum)
        {
            for (int i = 0; i < barNum; i++)
            {
                var barConfig = new BarConfig(DEFAULT_BEAT_COUNT, DEFAULT_BEAT_UNIT, DEFAULT_DIVISION_NUM);
                var subConfig = new SubdivisionConfig(DEFAULT_BPM, DEFAULT_SPEEDRATIO);
                BarDataInChart barData = new BarDataInChart(barConfig, subConfig, i);
                barDatas.Add(barData);
            }
        }

        ReactiveCollection<BarDataInChart> barDatas = new ReactiveCollection<BarDataInChart>();

        /// <summary>
        /// BPMが変更された時 (引数は小節Indexと分線Index)
        /// </summary>
        Subject<(int,int)> OnChangeBPM = new Subject<(int, int)>();
        public IObservable<(int, int)> OnChangeBPMListener => OnChangeBPM;

        /// <summary>
        /// 小節データが追加された時
        /// </summary>
        Subject<int> OnAddBarData = new Subject<int>();
        public IObservable<int> OnAddBarDataListener => OnAddBarData;


        /// <summary>
        /// 譜面内の全小節データ
        /// </summary>
        public IReadOnlyReactiveCollection<BarDataInChart> BarDatas => barDatas;

        public void SetSubDivisionConfig(int barIndex, int subIndex, SubdivisionConfig subConfig)
        {
            if (barDatas == null) { return; }

            SetBpm(barIndex, subIndex, subConfig.Bpm);
            SetSpeedRatio(barIndex, subIndex, subConfig.SpeedRatio);
        }

        /// <summary>
        /// 特定の分線から後のBPMを一括で変更する
        /// </summary>
        /// <param name="subDivisionData"></param>
        private void SetBpm(int barIndex, int subIndex, float bpm)
        {
            if (barDatas == null) { return; }

            float previousBpm = barDatas[barIndex].SubDivisionDatas[subIndex].Bpm.Value;
            bool isBreak = false;
            for (int i = barIndex; i < barDatas.Count; i++)
            {
                var barData = barDatas[i];
                for (int j = 0; j < barData.SubDivisionDatas.Count; j++)
                {
                    if (i == barIndex && j < subIndex) { continue; }

                    var subData = barData.SubDivisionDatas[j];

                    // 違うBPMになったら切る
                    if (previousBpm != subData.Bpm.Value) { isBreak = true; break; }

                    subData.SetBpm(bpm);
                }

                if (isBreak) { break; }
            }

            OnChangeBPM.OnNext((barIndex, subIndex));
        }

        /// <summary>
        /// 分線のBPMを直接書き換えた後に呼ぶ。指定した分線以降の線の位置を更新させる
        /// </summary>
        public void NotifyBpmChanged(int barIndex, int subIndex)
        {
            OnChangeBPM.OnNext((barIndex, subIndex));
        }

        /// <summary>
        /// 指定した分線から譜面の最後までの、分線ごとのBPM・スピード倍率を写し取る (元に戻す・やり直す用)
        ///
        /// BPM・スピード倍率の変更は「同じ値が続くかぎり、後ろの分線もまとめて書き換える」ので、
        /// 元に戻すときに同じ変更を逆向きにかけると、変更の前とは違う範囲が書き換わることがある
        /// (変更によって後ろの区間と同じ値になった場合、その区間まで巻き込んでしまう)。
        /// 変更の前後の値をそのまま写しておき、元に戻す・やり直すときは写しを入れ直す。
        /// </summary>
        /// <param name="bpms">BPMが、指定した分線から順に入る</param>
        /// <param name="speedRatios">スピード倍率が、指定した分線から順に入る</param>
        public void CopySubDivisionConfigsFrom(int barIndex, int subIndex, List<float> bpms, List<float> speedRatios)
        {
            bpms.Clear();
            speedRatios.Clear();

            for (int i = Mathf.Max(barIndex, 0); i < barDatas.Count; i++)
            {
                var subDivisions = barDatas[i].SubDivisionDatas;
                for (int j = i == barIndex ? Mathf.Max(subIndex, 0) : 0; j < subDivisions.Count; j++)
                {
                    bpms.Add(subDivisions[j].Bpm.Value);
                    speedRatios.Add(subDivisions[j].SpeedRatio.Value);
                }
            }
        }

        /// <summary>
        /// 写し取っておいたBPM・スピード倍率を、指定した分線から順に入れ直す
        /// (写しの数より後ろの分線には触らない。譜面の方が短いときは、ある分だけ入れる)
        /// </summary>
        public void RestoreSubDivisionConfigsFrom(int barIndex, int subIndex, List<float> bpms, List<float> speedRatios)
        {
            int count = Mathf.Min(bpms.Count, speedRatios.Count);
            int index = 0;

            for (int i = Mathf.Max(barIndex, 0); i < barDatas.Count && index < count; i++)
            {
                var subDivisions = barDatas[i].SubDivisionDatas;
                for (int j = i == barIndex ? Mathf.Max(subIndex, 0) : 0; j < subDivisions.Count && index < count; j++)
                {
                    subDivisions[j].SetBpm(bpms[index]);
                    subDivisions[j].SetSpeedRatio(speedRatios[index]);
                    index++;
                }
            }

            // BPMを直接書き換えたので、線の位置を更新させる
            OnChangeBPM.OnNext((barIndex, subIndex));
        }

        /// <summary>
        /// 特定の分線から後のスピード倍率を一括で変更する
        /// </summary>
        /// <param name="subDivisionData"></param>
        private void SetSpeedRatio(int barIndex, int subIndex, float speedRatio)
        {
            if (barDatas == null) { return; }

            float previousRatio = barDatas[barIndex].SubDivisionDatas[subIndex].SpeedRatio.Value;
            for (int i = barIndex; i < barDatas.Count; i++)
            {
                var barData = barDatas[i];
                for (int j = 0; j < barData.SubDivisionDatas.Count; j++)
                {
                    if (i == barIndex && j < subIndex) { continue; }

                    var subData = barData.SubDivisionDatas[j];

                    // 違うスピード倍率になったら切る
                    if (previousRatio != subData.SpeedRatio.Value) { return; }

                    subData.SetSpeedRatio(speedRatio);
                }
            }
        }

        /// <summary>
        /// 小節線の設定変更
        /// </summary>
        /// <param name="barIndex"></param>
        /// <param name="beatCount"></param>
        /// <param name="beatUnit"></param>
        /// <param name="divNum"></param>
        public void SetBarDataConfig(int barIndex, BarConfig config)
        {
            BarDataInChart barData = barDatas[barIndex];

            // 値が変わらなかった場合は何もしない
            if(barData.BeatCount.Value == config.BeatCount && barData.BeatUnit.Value == config.BeatUnit && barData.DivisionNum.Value == config.DivisionNum) { return; }

            // ノーツを削除
            for (int i = 0; i < barData.SubDivisionDatas.Count; i++)
            {
                var subData = barData.SubDivisionDatas[i];
                int noteCount = subData.NoteDatas.Count;

                for (int j = 0; j < noteCount; j++) 
                {
                    var note = subData.NoteDatas[0];
                    RemoveNote(note);
                }
            }

            // データをセットして小節線内のデータをアップデート
            barData.SetBeatCount(config.BeatCount);
            barData.SetBeatUnit(config.BeatUnit);
            barData.SetDivisionNum(config.DivisionNum);
        }

        /// <summary>
        /// 小節線の追加
        /// </summary>
        /// <param name="length"></param>
        public void AddBar(int length)
        {
            float bpm = DEFAULT_BPM;
            float speedRatio = DEFAULT_SPEEDRATIO;
            int barDataCount = BarDatas.Count;

            // 初期値の上書き
            if (BarDatas.Count > 0) 
            { 
                bpm = BarDatas.Last().SubDivisionDatas.Last().Bpm.Value; 
                speedRatio = BarDatas.Last().SubDivisionDatas.Last().SpeedRatio.Value;
            }

            for (int i = 0; i < length; i++)
            {
                var barConfig = new BarConfig(DEFAULT_BEAT_COUNT, DEFAULT_BEAT_UNIT, DEFAULT_DIVISION_NUM);
                var subConfig = new SubdivisionConfig(bpm, speedRatio);
                BarDataInChart barData = new BarDataInChart(barConfig, subConfig, barDataCount + i);
                barDatas.Add(barData);
            }

            OnAddBarData.OnNext(BarDatas.Count - length);
        }

        /// <summary>
        /// 設定を指定して、小節を末尾に1つ追加する (譜面の読み込み用)
        ///
        /// 既定の設定で小節を作ってから拍子・分割数を変えると、そのたびに分線を作り直すことになり、
        /// 譜面全体の線の位置を何度も計算し直してしまう。最初から正しい設定で作ればその無駄が無い。
        /// </summary>
        /// <param name="subConfig">小節内の全分線に最初に入れるBPM・スピード倍率</param>
        public void AddBar(BarConfig barConfig, SubdivisionConfig subConfig)
        {
            int barIndex = BarDatas.Count;
            barDatas.Add(new BarDataInChart(barConfig, subConfig, barIndex));

            OnAddBarData.OnNext(barIndex);
        }

        /// <summary>
        /// 拍子・分割数を指定して、小節を末尾にまとめて追加する (譜面の延長用)
        /// BPM とスピード倍率は、今の最後の分線の値を引き継ぐ
        /// </summary>
        /// <param name="length">追加する小節の数</param>
        /// <param name="barConfig">追加する小節の拍子・分割数</param>
        public void AddBars(int length, BarConfig barConfig)
        {
            if (length <= 0 || barConfig == null || !barConfig.IsValid) { return; }

            float bpm = DEFAULT_BPM;
            float speedRatio = DEFAULT_SPEEDRATIO;

            // 最後の分線の値を引き継ぐ
            if (BarDatas.Count > 0 && BarDatas[BarDatas.Count - 1].SubDivisionDatas.Count > 0)
            {
                var lastBar = BarDatas[BarDatas.Count - 1];
                var lastSubDivision = lastBar.SubDivisionDatas[lastBar.SubDivisionDatas.Count - 1];
                bpm = lastSubDivision.Bpm.Value;
                speedRatio = lastSubDivision.SpeedRatio.Value;
            }

            int firstIndex = BarDatas.Count;
            for (int i = 0; i < length; i++)
            {
                var config = new BarConfig(barConfig.BeatCount, barConfig.BeatUnit, barConfig.DivisionNum);
                var subConfig = new SubdivisionConfig(bpm, speedRatio);
                barDatas.Add(new BarDataInChart(config, subConfig, firstIndex + i));
            }

            OnAddBarData.OnNext(firstIndex);
        }

        /// <summary>
        /// 小節線の削除
        /// </summary>
        /// <param name="length"></param>
        /// <param name="isLogEnabled">false のとき、削除したノーツのログを出さない (譜面を丸ごと入れ替えるとき用)</param>
        public void RemoveBar(int length, bool isLogEnabled = true)
        {
            int barCount = BarDatas.Count;
            for (int i = barCount - 1; i >= Mathf.Max(barCount - length, 0); i--)
            {
                // 分線を全部消す
                var barData = barDatas[i];
                for (int j = barData.SubDivisionDatas.Count - 1; j >= 0 ; j--)
                {
                    // ノートも全部消す
                    var subData = barData.SubDivisionDatas[j];
                    int noteCount = subData.NoteDatas.Count;
                    for (int k = 0; k < noteCount; k++)
                    {
                        var note = subData.NoteDatas[0];
                        RemoveNote(note, isLogEnabled);
                    }

                    barData.RemoveSubDivisionData(barDatas[i].SubDivisionDatas.Count - 1);
                }
             
                barDatas.RemoveAt(i);
            }
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


        #region NotesOperation

        /// <summary>
        /// ノーツを追加する
        /// </summary>
        public void AddNote(IDeployableNoteData noteData)
        {
            // 新たな場所に追加
            var address = noteData.Address;

            if (!IsExistAddressInChart(new AddressInChart(address))) { return; }

            var newSubDivision = BarDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];
            newSubDivision.AddNote(noteData);

            OnAddNoteListener?.Invoke(noteData);
        }

        /// <summary>
        /// ノーツを削除する
        /// </summary>
        /// <param name="isLogEnabled">false のとき、削除のログを出さない (大量のノーツをまとめて消すとき用)</param>
        public void RemoveNote(IDeployableNoteData noteData, bool isLogEnabled = true)
        {
            var address = new AddressInChart(noteData.Address);

            if (!IsExistAddressInChart(new AddressInChart(address))) { return; }

            var subDivision = barDatas[address.BarIndex].SubDivisionDatas[address.SubDivisionIndex];

            if (!subDivision.RemoveNote(noteData))
            {
                Debug.LogError($"【削除】削除に失敗しました:\n{address}");
            }
            else
            {
                OnRemoveNoteListener?.Invoke(noteData);

                if (isLogEnabled) { Debug.Log($"【削除】:\n{address}"); }
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

        #endregion


        #region Address

        public bool IsExistAddressInChart(AddressInChart address)
        {
            if (address.BarIndex >= BarDatas.Count) { return false; }
            if (address.BarIndex < 0) { return false; }
            if (address.SubDivisionIndex >= BarDatas[address.BarIndex].SubDivisionDatas.Count) { return false; }
            if (address.SubDivisionIndex < 0) { return false; }
            return true;
        }

        /// <summary>
        /// アドレス間の距離(分線の数)を返す
        /// </summary>
        /// <param name="addressA"></param>
        /// <param name="AddressB"></param>
        /// <returns></returns>
        public int GetSubdivisionDelta(IReadOnlyAddressInChart targetAddress, IReadOnlyAddressInChart baseAddress)
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
        public AddressInChart AddressAddition(AddressInChart address, int subdivisionDelta)
        {
            var copy = new AddressInChart(address);

            if (subdivisionDelta >= 0)
            {
                for (int i = copy.BarIndex; i < BarDatas.Count; i++)
                {
                    var barData = BarDatas[i];

                    // 0になったら終わり
                    if (subdivisionDelta == 0) { break; }

                    // 小節1個越えるとき
                    if (barData.SubDivisionDatas.Count <= copy.SubDivisionIndex + subdivisionDelta)
                    {
                        // 端を超えるとき
                        if (i + 1 >= BarDatas.Count)
                        {
                            copy.SetSubDivisionIndex(barData.SubDivisionDatas.Count - 1);
                            break;
                        }

                        subdivisionDelta -= barData.SubDivisionDatas.Count - copy.SubDivisionIndex;

                        copy.SetBarIndex(copy.BarIndex + 1);
                        copy.SetSubDivisionIndex(0);
                    }
                    // この小節にあるとき
                    else
                    {
                        copy.SetSubDivisionIndex(copy.SubDivisionIndex + subdivisionDelta);
                        break;
                    }
                }
            }
            else
            {
                for (int i = copy.BarIndex; i < BarDatas.Count; i--)
                {
                    // 0になったら終わり
                    if (subdivisionDelta == 0) { break; }

                    // 小節1個越えるとき
                    if (copy.SubDivisionIndex + subdivisionDelta < 0)
                    {
                        // 端を超えるとき
                        if (i - 1 < 0)
                        {
                            copy.SetSubDivisionIndex(0);
                            break;
                        }

                        var barDataBack = BarDatas[i - 1];
                        subdivisionDelta += copy.SubDivisionIndex + 1;

                        copy.SetBarIndex(copy.BarIndex - 1);
                        copy.SetSubDivisionIndex(barDataBack.SubDivisionDatas.Count - 1);
                    }
                    // この小節にあるとき
                    else
                    {
                        copy.SetSubDivisionIndex(copy.SubDivisionIndex + subdivisionDelta);
                        break;
                    }
                }

            }

            return copy;
        }

        #endregion
    }


    public interface ISubDivisionDataGetter
    {
        IBarDataGetter BarData { get; }

        int SubDivisionIndex { get; }

        SubdivisionConfig SubConfig { get; }

        IReadOnlyReactiveCollection<IDeployableNoteData> NoteDatas { get; }

        IReadOnlyReactiveProperty<float> Bpm { get; }

        IReadOnlyReactiveProperty<float> SpeedRatio { get; }
    }

    public interface IBarDataGetter
    {
        int BarIndex { get; }

        BarConfig BarConfig { get; }

        IReadOnlyReactiveProperty<int> BeatCount { get; }

        IReadOnlyReactiveProperty<float> BeatUnit { get; }

        IReadOnlyReactiveProperty<int> DivisionNum { get; }

        IReadOnlyReactiveCollection<SubDivisionDataInBeat> SubDivisionDatas { get; }
    }
}