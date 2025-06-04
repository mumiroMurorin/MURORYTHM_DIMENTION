using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace ChartEditor
{
    public class VertexObject : MonoBehaviour
    {
        SpaceHoldVertex vertexData;
        Action onChangePositionListener;
        Func<Vector2, Vector2> calcPositionOnChartGround;

        public void Initialize(SpaceHoldVertex vertexData, Action onChangePositionListener, Func<Vector2, Vector2> calcPositionOnChartGround)
        {
            this.vertexData = vertexData;
            this.onChangePositionListener = onChangePositionListener;
            this.calcPositionOnChartGround = calcPositionOnChartGround;
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
            Vector2 converted = calcPositionOnChartGround(pos);

            this.gameObject.transform.localPosition
                = new Vector3(converted.x, converted.y, this.gameObject.transform.localPosition.z);

            onChangePositionListener?.Invoke();
        }

        public void Destroy()
        {
            Destroy(this.gameObject);
        }
    }
}

