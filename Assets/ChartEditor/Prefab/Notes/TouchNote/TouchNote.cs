using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;

namespace ChartEditor
{
    public class TouchNote : NoteObject
    {
        
    }

    [System.Serializable]
    public class NoteData_Touch : IDeployableNoteData
    {
        public NoteData_Touch() { }

        public NoteData_Touch(NoteData_Touch data)
        {
            SetAddress(data.address);
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.TouchNote;

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
            return new NoteData_Touch(this);
        }
    }

}