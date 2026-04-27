using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(VertexObject))]
    public class VertexDestroyable : MonoBehaviour, IDestroyableVertex
    {
        [SerializeField] VertexObject vertex;
        public VertexObject Vertex => vertex;

        void IDestroyableVertex.OnDestroy()
        {
            vertex.Destroy();
        }
    }

}