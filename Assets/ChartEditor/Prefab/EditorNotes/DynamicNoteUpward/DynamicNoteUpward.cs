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
           SetAddress(data.address);
        }


        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.DynamicGroundUpward);
        public DeploymentNoteType NoteType
        {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;


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
            return new NoteData_DynamicUpward(this);
        }
    }
}