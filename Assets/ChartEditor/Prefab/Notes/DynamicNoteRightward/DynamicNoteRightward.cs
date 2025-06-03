using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class DynamicNoteRightward : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_DynamicRightward : IDeployableNoteData
    {
        public NoteData_DynamicRightward() { }

        public NoteData_DynamicRightward(NoteData_DynamicRightward data)
        {
            this.Address = new AddressInChart(data.Address);
            this.SetRange(data.Range.ToList());
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundRightward;

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
            Debug.Log($"yŠg‘åz\n {range.First()} ` {range.Last()}");
        }

        public void SetAddress(AddressInChart address)
        {
            // “¯‚¶ƒAƒhƒŒƒX‚È‚ç•Ô‚·
            if (Address != null && Address.IsSameAddress(address)) { return; }

            if (Address == null) { Address = new AddressInChart(address); }
            else
            {
                Debug.Log($"yˆÚ“®z:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
                Address.SetSameAddress(address);
            }

            int startIndex = (int)address.SliderIndex;
            List<float> currentRange = range.ToList();
            List<float> shifted = currentRange.Select(i => i - currentRange[0] + startIndex).ToList();

            SetRange(shifted);
        }

        public IDeployableNoteData Copy()
        {
            return new NoteData_DynamicRightward(this);
        }
    }
}