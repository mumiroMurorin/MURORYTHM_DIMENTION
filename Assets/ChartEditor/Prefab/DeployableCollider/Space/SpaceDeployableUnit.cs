using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SpaceDeployableUnit : MonoBehaviour
    {
        int index = 100;

        public AddressInChart Address { get; private set; }

        public void SetAddress(int barIndex, int subIndex)
        {
            Address = new AddressInChart(barIndex, subIndex, index);
        }

        public Transform GetNoteDeployableUnitTransforms()
        {
            return this.transform;
        }
    }

}