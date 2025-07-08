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
            SetAddress(data.address);
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundRightward;

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
            return new NoteData_DynamicRightward(this);
        }
    }
}