using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class HoldNoteEnd : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_HoldEnd : IGroundChainNoteData
    {
        public DeploymentNoteType NoteType => DeploymentNoteType.HoldEnd;

        public AddressInChart Address { get; private set; } = new AddressInChart();

        /// <summary>
        /// ”z’u”ÍˆÍ (Šî–{0`15)
        /// </summary>
        ReactiveCollection<float> range = new ReactiveCollection<float>() { 0 };

        /// <summary>
        /// ƒm[ƒc‚ÌˆÚ“®AŠg‘åk¬‚ÌŠÄ‹
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

            // ‰EŒÅ’è‚Å¶‘¤‚Æindex‚ªˆê‚Ì‚Æ‚«•Ô‚·
            if (isRightAnchored && (int)min == index) { return; }
            // ¶ŒÅ’è‚Å‰E‘¤‚Æindex‚ªˆê‚Ì‚Æ‚«•Ô‚·
            if (!isRightAnchored && (int)max == index) { return; }

            // ‰EŒÅ’è‚Å¶‘¤‚ÉL‚Î‚·
            if (isRightAnchored && index <= max)
            {
                for (float i = index; i <= max; i++) { shifted.Add(i); }
            }
            // ¶ŒÅ’è‚Å‰E‘¤‚ÉL‚Î‚·
            else if (!isRightAnchored && index >= min)
            {
                for (float i = min; i <= index; i++) { shifted.Add(i); }
            }
            else
            {
                return;
            }

            SetRange(shifted);
            LogUI.Instance.Log($"yŠg‘åz\n range:{string.Join(",", range)}");
        }

        public void SetAddress(AddressInChart address)
        {
            // “¯‚¶ƒAƒhƒŒƒX‚È‚ç•Ô‚·
            if (Address != null && Address.IsSameAddress(address)) { return; }

            if (Address == null) { Address = address.Copy(); }
            else
            {
                LogUI.Instance.Log($"yˆÚ“®z:\n #{address.BarIndex} {address.SubDivisionIndex} {address.SliderIndex}");
                Address.SetSameAddress(address);
            }

            int startIndex = (int)address.SliderIndex;
            List<float> currentRange = range.ToList();
            List<float> shifted = currentRange.Select(i => i - currentRange[0] + startIndex).ToList();

            SetRange(shifted);
        }


        /// <summary>
        /// Ÿ‚Ìƒm[ƒc
        /// </summary>
        ReactiveProperty<IGroundChainNoteData> nextNote = null;
        public IReadOnlyReactiveProperty<IGroundChainNoteData> NextNote => nextNote;
        public void SetNextNote(IGroundChainNoteData nextNote)
        {
            // I“_‚ÉŸ‚Ìƒm[ƒc‚Í‚È‚¢
            return;
            //this.nextNote.Value = nextNote;
        }

        /// <summary>
        /// ‘O‚Ìƒm[ƒc
        /// </summary>
        ReactiveProperty<IGroundChainNoteData> backNote = new ReactiveProperty<IGroundChainNoteData>();
        public IReadOnlyReactiveProperty<IGroundChainNoteData> BackNote => backNote;

        public void SetBackNote(IGroundChainNoteData backNote)
        {
            this.backNote.Value = backNote;
        }


        public IGroundNoteData Copy()
        {
            var data = new NoteData_HoldEnd
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