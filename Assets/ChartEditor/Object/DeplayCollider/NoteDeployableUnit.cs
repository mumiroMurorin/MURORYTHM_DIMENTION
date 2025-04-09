using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableUnit : MonoBehaviour
    {
        [SerializeField] int index;

        public AddressInChart Address { get; private set; }

        public void SetAddress(int barIndex,int subIndex)
        {
            Address = new AddressInChart() { BarIndex = barIndex, SubDivisionIndex = subIndex, SliderIndex = index };
        }
    }

}