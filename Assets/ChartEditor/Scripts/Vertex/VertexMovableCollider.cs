using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class VertexMovableCollider : MonoBehaviour, IPointMovableCollider
    {
        [SerializeField] SerializeInterface<IPointMovableObject> vertex;

        public EditMode EditMode => EditMode.None;

        public IPointMovableObject Vertex => vertex.Value;

    }
}