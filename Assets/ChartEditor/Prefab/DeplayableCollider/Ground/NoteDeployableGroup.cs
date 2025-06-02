using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableGroup : MonoBehaviour
    {
        [SerializeField] NoteDeployableUnit[] units;

        public void SetAddress(int barIndex, int subIndex)
        {
            foreach (var unit in units)
            {
                unit.SetAddress(barIndex, subIndex);
            }
        }

        public Transform[] GetNoteDeployableUnitTransforms()
        {
            List<Transform> transforms = new List<Transform>();

            foreach(var unit in units)
            {
                transforms.Add(unit.transform);
            }

            return transforms.ToArray();
        }
    }
}