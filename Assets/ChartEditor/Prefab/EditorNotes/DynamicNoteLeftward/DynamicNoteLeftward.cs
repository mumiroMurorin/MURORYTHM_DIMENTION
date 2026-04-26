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
    public class NoteData_DynamicLeftward : IDeployableNoteData, ITypeChangableNoteData, IMirrorTypeChangableNoteData
    {
        public NoteData_DynamicLeftward() { }

        public NoteData_DynamicLeftward(NoteData_DynamicLeftward data)
        {
            this.SetAddress(new AddressWithinRange(data.address));
        }

        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.DynamicGroundLeftward);
        public DeploymentNoteType NoteType
        {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (noteType != DeploymentNoteType.DynamicGroundLeftward &&
                noteType != DeploymentNoteType.DynamicGroundRightward)
            {
                Debug.LogWarning($"yNotezDynamicNote‚Í {noteType} ‚É‘Î‰ž‚µ‚Ä‚¢‚Ü‚¹‚ñ");
                return;
            }

            NoteType = noteType;
        }
        public void ChangeNoteType()
        {
            NoteType = NoteTypeCycle.NextNoteType(NoteType);
        }


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