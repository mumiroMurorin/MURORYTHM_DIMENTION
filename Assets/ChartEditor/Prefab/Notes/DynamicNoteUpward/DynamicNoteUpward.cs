using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class DynamicNoteUpward : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_DynamicUpward : IDeployableNoteData
    {
        public NoteData_DynamicUpward() { }

        public NoteData_DynamicUpward(NoteData_DynamicUpward data)
        {
            this.Address = new AddressWithinRange(data.Address);
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundUpward;

        public AddressWithinRange Address { get; private set; }

        public void SetAddress(AddressWithinRange address)
        {
            if (Address == null) { Address = new AddressWithinRange(address); }
            else
            {
                //Debug.Log($"yˆÚ“®z:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
                Address.SetSameAddress(address);
            }
        }

        public IDeployableNoteData Copy()
        {
            return new NoteData_DynamicUpward(this);
        }
    }
}