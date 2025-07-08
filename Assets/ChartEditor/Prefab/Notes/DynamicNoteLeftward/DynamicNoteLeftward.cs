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
            this.SetAddress(new AddressWithinRange(data.address));
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundLeftward;

        AddressWithinRange address;
        public IReadOnlyAddressWithinRange Address => address;

        public void SetAddress(IReadOnlyAddressWithinRange address)
        {
            if (Address == null) { this.address = new AddressWithinRange(address); }
            else
            {
                this.address.SetSameAddress(address);
            }
        }

        public IDeployableNoteData Copy()
        {
            return new NoteData_DynamicLeftward(this);
        }
    }

}