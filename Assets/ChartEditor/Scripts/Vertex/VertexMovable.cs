using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(VertexObject))]
    public class VertexMovable : MonoBehaviour, IPointMovableObject
    {
        [SerializeField] VertexObject vertexObject;
        [Tooltip("移動時のアウトライン色")]
        [SerializeField] ColorSetting outlineColorOnMove;
        [Tooltip("移動時浮く高さ")]
        [SerializeField] float addHeightOnMove = 1f;

        VertexObject IPointMovableObject.Vertex => vertexObject;

        Vector3 addPos;
        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => vertexObject.VertexData != null, cancellationToken: token);

            vertexObject.VertexData.Position
                .Subscribe(OnChangePosition)
                .AddTo(this.gameObject);
        }

        private void OnChangePosition(Vector2 pos)
        {
            Vector2 converted = vertexObject.CalcPositionOnChartGround(pos);

            this.gameObject.transform.localPosition
                = new Vector3(converted.x, converted.y, this.gameObject.transform.localPosition.z);

            vertexObject.OnChangePositionListener?.Invoke();
        }

        void IPointMovableObject.OnMoveStart()
        {
            vertexObject.OutlineColors.Add(outlineColorOnMove);
            vertexObject.SetCollidersActive(false);

            // 追加するベクトルを保存
            addPos = Vector3.back * addHeightOnMove;
            this.transform.position += addPos;
        }

        void IPointMovableObject.OnMove()
        {

        }

        void IPointMovableObject.OnMoveEnd()
        {
            vertexObject.OutlineColors.Remove(outlineColorOnMove);
            vertexObject.SetCollidersActive(true);

            // 追加したベクトル分元に戻す
            this.transform.position -= addPos;
            addPos = Vector3.zero;
        }

        private void OnDestroy()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }

}
