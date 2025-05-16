using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class HoldNote : NoteObject
    {

    }

    [System.Serializable]
    public class NoteData_Hold : IGroundChainNoteData, ITypeChangableNoteData
    {
        public NoteData_Hold() { }

        public NoteData_Hold(NoteData_Hold data)
        {
            this.Address = new AddressInChart(data.Address);
            this.SetRange(data.Range.ToList());
        }

        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.Hold);
        public DeploymentNoteType NoteType {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;

        public AddressInChart Address { get; private set; } = new AddressInChart();

        /// <summary>
        /// 配置範囲 (基本0～15)
        /// </summary>
        ReactiveCollection<float> range = new ReactiveCollection<float>() { 0 };

        /// <summary>
        /// ノーツの移動、拡大縮小の監視
        /// </summary>
        public IReadOnlyReactiveCollection<float> Range { get { return range; } }

        public void ChangeNoteType(bool isCompulsion)
        {
            // 可視 → 不可視
            if(NoteType == DeploymentNoteType.Hold)
            {
                if (!isCompulsion && (NextNote.Value == null || BackNote.Value == null)) { return; }
                NoteType = DeploymentNoteType.HoldHidden;
            }
            // 不可視 → 可視
            else if (NoteType == DeploymentNoteType.HoldHidden)
            {
                NoteType = DeploymentNoteType.Hold;
            }
        }

        public void SetRange(List<float> range)
        {
            this.range.Clear();

            foreach (float index in range)
            {
                this.range.Add(index);
            }

            Address.SetSliderIndex(this.range.First());
        }

        public void ChangeRange(float index, bool isRightAnchored)
        {
            List<float> shifted = new List<float>();
            float min = range.First();
            float max = range.Last();

            // 右固定で左側とindexが一緒のとき返す
            if (isRightAnchored && (int)min == index) { return; }
            // 左固定で右側とindexが一緒のとき返す
            if (!isRightAnchored && (int)max == index) { return; }

            // 右固定で左側に伸ばす
            if (isRightAnchored && index <= max)
            {
                for (float i = index; i <= max; i++) { shifted.Add(i); }
            }
            // 左固定で右側に伸ばす
            else if (!isRightAnchored && index >= min)
            {
                for (float i = min; i <= index; i++) { shifted.Add(i); }
            }
            else
            {
                return;
            }

            SetRange(shifted);
            Debug.Log($"【拡大】\n {range.First()} ～ {range.Last()}");
        }

        public void SetAddress(AddressInChart address)
        {
            // 同じアドレスなら返す
            if (Address != null && Address.IsSameAddress(address)) { return; }

            if (Address == null) { Address = new AddressInChart(address); }
            else
            {
                Debug.Log($"【移動】:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
                Address.SetSameAddress(address);
            }

            int startIndex = (int)address.SliderIndex;
            List<float> currentRange = range.ToList();
            List<float> shifted = currentRange.Select(i => i - currentRange[0] + startIndex).ToList();

            SetRange(shifted);
        }

        public void AddChainNote(IGroundChainNoteData addNote)
        {
            List<IGroundChainNoteData> chains = new List<IGroundChainNoteData>();

            // ノーツを追加
            chains.Add(this);
            chains.Add(addNote);

            // このノーツを遡って全部リストに追加
            IGroundChainNoteData backNote = this.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // このノーツを進んで全部リストに追加
            IGroundChainNoteData nextNote = this.NextNote.Value;
            while (nextNote != null)
            {
                chains.Add(nextNote);
                nextNote = nextNote.NextNote.Value;
            }

            // 追加ノーツを遡って全部リストに追加
            backNote = addNote.BackNote.Value;
            while (backNote != null)
            {
                chains.Add(backNote);
                backNote = backNote.BackNote.Value;
            }

            // 追加ノーツを進んで全部リストに追加
            nextNote = addNote.NextNote.Value;
            while (nextNote != null)
            {
                chains.Add(nextNote);
                nextNote = nextNote.NextNote.Value;
            }

            // 重複項目を削除
            chains = chains.Distinct().ToList();
            // ソート
            chains.Sort((a,b) => { 
                if (a.Address.IsEarlierThan(b.Address)) { return -1; }
                else { return 1; }
            });

            // それぞれのノーツをつなげる
            for (int i = 0; i < chains.Count; i++) 
            {
                // 中継点
                if (i > 0) { chains[i].SetBackNote(chains[i - 1]); }
                // 始点
                else { chains[i].SetBackNote(null); }

                // 中継点
                if (i < chains.Count - 1) { chains[i].SetNextNote(chains[i + 1]); }
                // 終点
                else { chains[i].SetNextNote(null); }
            }
        }

        public void RemoveNote()
        {
            // 前ノーツ、次ノーツに前後のノーツをセット
            NextNote.Value?.SetBackNote(BackNote.Value);
            BackNote.Value?.SetNextNote(NextNote.Value);
        }

        /// <summary>
        /// 次のノーツ
        /// </summary>
        ReactiveProperty<IGroundChainNoteData> nextNote = new ReactiveProperty<IGroundChainNoteData>();
        public IReadOnlyReactiveProperty<IGroundChainNoteData> NextNote => nextNote;
        public void SetNextNote(IGroundChainNoteData nextNote)
        {
            this.nextNote.Value = nextNote;
        }

        /// <summary>
        /// 前のノーツ
        /// </summary>
        ReactiveProperty<IGroundChainNoteData> backNote = new ReactiveProperty<IGroundChainNoteData>();
        public IReadOnlyReactiveProperty<IGroundChainNoteData> BackNote => backNote;
        public void SetBackNote(IGroundChainNoteData backNote)
        {
            this.backNote.Value = backNote;
        }

        public IConnectableObject NoteObject { get; private set; }

        public void SetNoteObject(IConnectableObject noteObject)
        {
            NoteObject = noteObject;
        }

        public IGroundNoteData Copy()
        {
            return new NoteData_Hold(this);
        }
    }

}