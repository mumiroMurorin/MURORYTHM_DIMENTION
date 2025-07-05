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
            this.Address = new AddressWithinRange(data.Address);
        }

        public DeploymentNoteType NoteType => DeploymentNoteType.DynamicGroundDownward;

        public AddressWithinRange Address { get; private set; }

        public void SetAddress(AddressWithinRange address)
        {
            if (Address == null) { Address = new AddressWithinRange(); }
            else
            {
                //Debug.Log($"yˆÚ“®z:\n #{address.BarIndex} - {address.SubDivisionIndex} - {address.SliderIndex}");
                Address.SetSameAddress(address);
            }
        }

        public IDeployableNoteData Copy()
        {
            return new NoteData_DynamicDownward(this);
        }
    }
}