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
            this.Address = new AddressWithinRange(data.Address);
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundRightward;

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
            return new NoteData_DynamicRightward(this);
        }
    }
}