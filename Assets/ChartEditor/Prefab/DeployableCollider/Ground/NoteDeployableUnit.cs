using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableUnit : MonoBehaviour
    {
        [SerializeField] int index;

        [SerializeField] float maxHeight = 0.075f;
        [SerializeField] float minHeight = 0.05f;
        [SerializeField] float minZ = 0.1f;
        [SerializeField] float maxZ = 50f;

        [SerializeField] BoxCollider boxCollider;

        public AddressInChart Address { get; private set; }

        public void ChangeSize(float z)
        {
            float height = Mathf.Lerp(minHeight, maxHeight, 1f - (Mathf.Clamp(z, minZ, maxZ) - minZ) / (maxZ - minZ));
            boxCollider.size = new Vector3(boxCollider.size.x, height, z);
        }

        public void SetAddress(int barIndex, int subIndex)
        {
            Address = new AddressInChart(barIndex, subIndex, index);
        }
    }

}