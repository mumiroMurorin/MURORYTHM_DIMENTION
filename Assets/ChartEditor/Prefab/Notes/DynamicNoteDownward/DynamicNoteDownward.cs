using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class DynamicNoteDownward : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_DynamicDownward : IDeployableNoteData
    {
        public NoteData_DynamicDownward() { }

        public NoteData_DynamicDownward(NoteData_DynamicDownward data)
        {
            this.SetAddress(new AddressWithinRange(data.address));
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundDownward;

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
            return new NoteData_DynamicDownward(this);
        }
    }
}