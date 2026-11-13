using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 配置ノーツの「見た目の板」と「カーソルで掴める範囲」を、時間方向(画面では縦)に縮める
    ///
    /// ホールドの中継点のように、短い間隔でいくつも並ぶノーツを細長くして、
    /// 前後のノーツや、その間の帯を見やすく・掴みやすくするために使う。
    /// 横幅(レーン数)はほかの処理が変えるので、ここでは時間方向の大きさだけを書き換える。
    /// </summary>
    public class NoteDepthScaler
    {
        private struct Target
        {
            public Transform Transform;
            // 時間方向が、その部品のどの軸にあたるか (0: X、1: Y、2: Z)
            public int DepthAxis;
            // 縮める前の、時間方向の大きさ
            public float BaseDepth;
        }

        readonly List<Target> targets = new List<Target>();

        /// <param name="noteObject">ノーツ</param>
        /// <param name="noteMeshRenderer">ノーツの見た目の板</param>
        public NoteDepthScaler(NoteObject noteObject, MeshRenderer noteMeshRenderer)
        {
            if (noteObject == null) { return; }

            Transform noteTransform = noteObject.transform;

            // 見た目の板
            if (noteMeshRenderer != null) { Add(noteMeshRenderer.transform, noteTransform); }

            // カーソルで掴める範囲 (移動・幅の変更・接続・種類変更などの当たり判定)
            // 見た目だけを縮めると、何も無いように見える場所でノーツを掴んでしまうので、一緒に縮める
            foreach (var collider in noteObject.GetComponentsInChildren<IInteractableCollider>(true))
            {
                if (collider is Component component) { Add(component.transform, noteTransform); }
            }
        }

        private void Add(Transform transform, Transform noteTransform)
        {
            // ノーツそのものは縮めない (つながる帯の端の位置など、ほかの部品までまとめて縮んでしまう)
            if (transform == null || transform == noteTransform) { return; }

            foreach (var target in targets)
            {
                if (target.Transform == transform) { return; }
            }

            // 時間方向(ノーツの前後の向き)が、この部品のどの軸にあたるかを調べる
            // (見た目の板は寝かせて置いてあるので、板の縦が時間方向になる。当たり判定の箱は奥行きが時間方向になる)
            Vector3 depthDirection = transform.InverseTransformDirection(noteTransform.forward);
            float x = Mathf.Abs(depthDirection.x);
            float y = Mathf.Abs(depthDirection.y);
            float z = Mathf.Abs(depthDirection.z);
            int depthAxis = (x >= y && x >= z) ? 0 : (y >= z ? 1 : 2);

            targets.Add(new Target
            {
                Transform = transform,
                DepthAxis = depthAxis,
                BaseDepth = transform.localScale[depthAxis],
            });
        }

        /// <summary>
        /// 時間方向の大きさを変える
        /// </summary>
        /// <param name="scale">縮める割合 (1 で元の大きさ)</param>
        public void SetScale(float scale)
        {
            foreach (var target in targets)
            {
                if (target.Transform == null) { continue; }

                // 横幅など、ほかの向きの大きさは今の値のまま残す
                Vector3 localScale = target.Transform.localScale;
                localScale[target.DepthAxis] = target.BaseDepth * scale;
                target.Transform.localScale = localScale;
            }
        }
    }
}
