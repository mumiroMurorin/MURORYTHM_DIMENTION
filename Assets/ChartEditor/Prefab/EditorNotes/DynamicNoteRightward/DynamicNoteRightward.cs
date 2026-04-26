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
    public class NoteData_DynamicRightward : IDeployableNoteData, ITypeChangableNoteData, IMirrorTypeChangableNoteData
    {
        public NoteData_DynamicRightward() { }

        public NoteData_DynamicRightward(NoteData_DynamicRightward data)
        {
            SetAddress(data.address);
        }


        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.DynamicGroundRightward);
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
            return new NoteData_DynamicRightward(this);
        }
    }
}