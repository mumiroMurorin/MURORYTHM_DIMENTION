using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class VertexObject : MonoBehaviour
    {
        SpaceHoldVertex vertexData;

        public void SetVertexData(SpaceHoldVertex vertexData)
        {
            this.vertexData = vertexData;
            Bind(vertexData);
        }

        private void Bind(SpaceHoldVertex vertexData)
        {
            vertexData.Position
                .Subscribe(OnChangePosition)
                .AddTo(this.gameObject);
        }

        private void OnChangePosition(Vector2 pos)
        {
            this.gameObject.transform.position
                = new Vector3(pos.x, pos.y, this.gameObject.transform.position.z);
        }
    }
}

