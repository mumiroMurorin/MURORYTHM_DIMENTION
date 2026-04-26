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
    public class NoteData_Touch : IDeployableNoteData, ITypeChangableNoteData
    {
        public NoteData_Touch() { }

        public NoteData_Touch(NoteData_Touch data)
        {
            SetAddress(data.address);
        }

        // ÉmÅ[ÉgÉ^ÉCÉv
        ReactiveProperty<DeploymentNoteType> noteType = new ReactiveProperty<DeploymentNoteType>(DeploymentNoteType.Touch);
        public DeploymentNoteType NoteType
        {
            get { return noteType.Value; }
            private set { noteType.Value = value; }
        }
        public IReadOnlyReactiveProperty<DeploymentNoteType> NoteTypeRP => noteType;
        public void SetNoteType(DeploymentNoteType noteType)
        {
            if (noteType != DeploymentNoteType.Touch &&
                noteType != DeploymentNoteType.DivineTouch)
            {
                Debug.LogWarning($"ÅyNoteÅzTouchNoteÇÕ {noteType} Ç…ëŒâûÇµÇƒÇ¢Ç‹ÇπÇÒ");
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
            return new NoteData_Touch(this);
        }
    }

}