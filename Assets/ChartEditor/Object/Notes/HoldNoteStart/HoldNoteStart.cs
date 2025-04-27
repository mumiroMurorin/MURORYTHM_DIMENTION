using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class HoldNoteStart : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_HoldStart : IGroundChainNoteData
    {
        public DeploymentNoteType NoteType => DeploymentNoteType.HoldStart;

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
        ReactiveProperty<IGroundChainNoteData> backNote = null;
        public IReadOnlyReactiveProperty<IGroundChainNoteData> BackNote => backNote;
        public void SetBackNote(IGroundChainNoteData backNote)
        {
            // 始点に前ノーツは存在しないので返す
            return;

            //this.backNote.Value = backNote;
        }

        /// <summary>
        /// コピー
        /// </summary>
        /// <returns></returns>
        public IGroundNoteData Copy()
        {
            var data = new NoteData_HoldStart
            {
                Address = this.Address.Copy()   
            };

            data.SetRange(this.range.ToList());
            return data;
        }
    }

}