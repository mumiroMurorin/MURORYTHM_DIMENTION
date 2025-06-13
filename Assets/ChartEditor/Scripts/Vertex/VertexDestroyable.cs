using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(VertexObject))]
    public class VertexDestroyable : MonoBehaviour, IDestroyableVertex
    {
        VertexObject vertex;
        public VertexObject Vertex => vertex;

        private void Start()
        {
            vertex = GetComponent<VertexObject>();
        }

        void IDestroyableVertex.OnDestroy()
        {
            vertex.VertexData = null;
            vertex.Destroy();
        }
    }

}