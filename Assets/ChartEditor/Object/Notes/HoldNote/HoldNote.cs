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
    public class NoteData_Hold : IGroundChainNoteData
    {
        public DeploymentNoteType NoteType => DeploymentNoteType.Hold;

        public AddressInChart Address { get; private set; } = new AddressInChart();

        /// <summary>
        /// 配置範囲 (基本0～15)
        /// </summary>
        ReactiveCollection<float> range = new ReactiveCollection<float>() { 0 };

        /// <summary>
        /// ノーツの移動、拡大縮小の監視
        /// </summary>
        public IReadOnlyReactiveCollection<float> Range { get { return range; } }

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
            LogUI.Instance.Log($"【拡大】\n range:{string.Join(",", range)}");
        }

        public void SetAddress(AddressInChart address)
        {
            // 同じアドレスなら返す
            if (Address != null && Address.IsSameAddress(address)) { return; }

            if (Address == null) { Address = address.Copy(); }
            else
            {
                LogUI.Instance.Log($"【移動】:\n #{address.BarIndex} {address.SubDivisionIndex} {address.SliderIndex}");
                Address.SetSameAddress(address);
            }

            int startIndex = (int)address.SliderIndex;
            List<float> currentRange = range.ToList();
            List<float> shifted = currentRange.Select(i => i - currentRange[0] + startIndex).ToList();

            SetRange(shifted);
        }

        public void AddChainNote(IGroundChainNoteData addNote)
        {
            // 同じノートは追加できない
            if(addNote == this) { return; }
            if(addNote == this.NextNote.Value) { return; }
            if(addNote == this.BackNote.Value) { return; }

            // このノーツよりも前で且つ前ノーツが無い時新たに登録
            if(!this.Address.IsEarlierThan(addNote.Address) && backNote.Value == null)
            {
                // 前ノーツに次ノーツが登録されているとき
                if (addNote.NextNote.Value != null)
                {
                    // 次ノーツを追加ノートの次ノートにする
                    this.SetNextNote(addNote.NextNote.Value);
                    // 追加ノートの次ノートの前ノートをこのノートにする(ややこしすぎる)
                    addNote.NextNote.Value.SetBackNote(addNote);
                }

                SetBackNote(addNote);
                addNote.SetNextNote(this);
                return;
            }

            // このノーツよりも先で且つ次ノーツが無い時新たに登録
            if (this.Address.IsEarlierThan(addNote.Address) && nextNote.Value == null)
            {
                // 次ノーツに前ノーツが登録されているとき
                if (addNote.BackNote.Value != null)
                {
                    // 前ノーツを追加ノートの前ノートにする
                    this.SetBackNote(addNote.BackNote.Value);
                    // 追加ノートの前ノートの次ノートをこのノートにする(ややこしすぎる)
                    addNote.BackNote.Value.SetNextNote(this);
                }

                SetNextNote(addNote);
                addNote.SetBackNote(this);
                return;
            }

            // このノーツと次ノーツの間だった時登録
            if(this.Address.IsEarlierThan(addNote.Address) && !NextNote.Value.Address.IsEarlierThan(addNote.Address))
            {
                // 追加ノーツの次ノーツに以前の次ノーツをセット
                addNote.SetNextNote(this.NextNote.Value);
                // 追加ノーツの前ノーツにこのノーツをセット
                addNote.SetBackNote(this);
                // 次ノーツの前ノーツに追加ノーツをセット
                this.NextNote.Value.SetBackNote(addNote);
                // 最後に、このノーツの次ノーツに追加ノーツをセット
                SetNextNote(addNote);

                return;
            }

            // 前ノーツ以前だった時前ノーツへ託す
            if (!this.Address.IsEarlierThan(addNote.Address))
            {
                backNote.Value?.AddChainNote(addNote);
                return;
            }

            // 次のノーツ以降だった時次ノーツに託す
            if (this.Address.IsEarlierThan(addNote.Address))
            {
                nextNote.Value?.AddChainNote(addNote);
                return;
            }

            Debug.Log($"【System】何故ここに来た？ {addNote} {this}");
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

        /// <summary>
        /// コピー
        /// </summary>
        /// <returns></returns>
        public IGroundNoteData Copy()
        {
            var data = new NoteData_Hold
            {
                Address = this.Address.Copy()   
            };

            data.SetRange(this.range.ToList());
            return data;
        }

        public IConnectableObject NoteObject { get; private set; }

        public void SetNoteObject(IConnectableObject noteObject)
        {
            NoteObject = noteObject;
        }
    }

}