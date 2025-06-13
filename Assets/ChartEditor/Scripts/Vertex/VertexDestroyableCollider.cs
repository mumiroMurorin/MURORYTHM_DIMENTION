using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class VertexDestroyableCollider : MonoBehaviour, IDestroyableVertexCollider
    {
        [SerializeField] SerializeInterface<IDestroyableVertex> vertex;

        public EditMode EditMode => EditMode.VertexDestroy;

        public IDestroyableVertex Vertex => vertex.Value;
    }
}