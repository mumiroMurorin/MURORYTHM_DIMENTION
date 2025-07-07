using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class DynamicNoteLeftward : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_DynamicLeftward : IDeployableNoteData
    {
        public NoteData_DynamicLeftward() { }

        public NoteData_DynamicLeftward(NoteData_DynamicLeftward data)
        {
            this.SetAddress(new AddressWithinRange(data.Address));
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundLeftward;

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
            return new NoteData_DynamicLeftward(this);
        }
    }

}