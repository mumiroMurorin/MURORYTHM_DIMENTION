using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集の、右上の向きの印 (Unity のシーンビューの右上と同じもの)
    ///
    /// 今のカメラの向きに合わせて、X (赤)・Y (緑)・Z (青) の軸の先を描く。反対向きの先は、灰色の小さな丸。
    /// ・軸の先を押す: その向きから見る (X: 横から、Y: 上から、Z: 奥から。反対の丸は、その反対から)
    /// ・まんなかを押す: 透視と平行を切り替える
    /// 奥を向いている軸ほど先に描き、手前の軸が上に重なるようにする。
    /// 線と丸は、シーンに置いてある (視点のウィンドウの中。色もシーンに書いてある: X 赤、Y 緑、Z 青)。
    /// ここでは、フレームごとに位置と重なり順を変えるだけ。
    /// </summary>
    public class SpaceHold3DViewGizmo : MonoBehaviour
    {
        const float RADIUS = 44f;
        const float LINE_WIDTH = 3f;

        [Tooltip("軸の線 (X・Y・Z の順。まんなかから、軸の先まで伸ばす)")]
        [SerializeField] Image[] lines;
        [Tooltip("軸の先の丸 (X・Y・Z の順)")]
        [SerializeField] GizmoClickTarget[] positiveDots;
        [Tooltip("反対向きの先の丸 (-X・-Y・-Z の順)")]
        [SerializeField] GizmoClickTarget[] negativeDots;
        [Tooltip("まんなかの丸 (透視と平行の切り替え)")]
        [SerializeField] GizmoClickTarget center;

        static readonly Vector3[] positiveDirections = { Vector3.right, Vector3.up, Vector3.forward };
        static readonly Vector3[] negativeDirections = { Vector3.left, Vector3.down, Vector3.back };

        /// <summary>軸の先が押されたとき (押した先の向き。例: +X なら Vector3.right)</summary>
        public Action<Vector3> OnAxisClicked { get; set; }

        /// <summary>まんなかが押されたとき</summary>
        public Action OnCenterClicked { get; set; }

        class Axis
        {
            public Vector3 Direction;
            public Image Line;
            public Image Dot;
            public float Depth;
        }

        readonly List<Axis> axes = new List<Axis>();
        bool isConnected;

        private void Awake()
        {
            Connect();
        }

        /// <summary>
        /// シーンの丸に、押されたときの処理をつなぐ (1回だけ)
        /// </summary>
        private void Connect()
        {
            if (isConnected) { return; }
            isConnected = true;

            AddAxes(positiveDots, positiveDirections, true);
            AddAxes(negativeDots, negativeDirections, false);
            if (center != null) { center.OnClick = () => OnCenterClicked?.Invoke(); }
        }

        private void AddAxes(GizmoClickTarget[] dots, Vector3[] directions, bool isPositive)
        {
            if (dots == null) { return; }

            for (int i = 0; i < dots.Length && i < directions.Length; i++)
            {
                if (dots[i] == null || dots[i].Image == null) { continue; }

                Vector3 direction = directions[i];
                dots[i].OnClick = () => OnAxisClicked?.Invoke(direction);
                axes.Add(new Axis
                {
                    Direction = direction,
                    Line = isPositive && lines != null && i < lines.Length ? lines[i] : null,
                    Dot = dots[i].Image,
                });
            }
        }

        /// <summary>
        /// 今のカメラの向きに合わせて、軸の先を置き直す
        /// </summary>
        public void Refresh(Quaternion cameraRotation)
        {
            Connect();

            Quaternion toView = Quaternion.Inverse(cameraRotation);
            foreach (var axis in axes)
            {
                Vector3 view = toView * axis.Direction;
                Vector2 position = new Vector2(view.x, view.y) * RADIUS;
                axis.Depth = view.z;
                axis.Dot.rectTransform.anchoredPosition = position;

                if (axis.Line != null)
                {
                    float length = position.magnitude;
                    axis.Line.rectTransform.anchoredPosition = Vector2.zero;
                    axis.Line.rectTransform.sizeDelta = new Vector2(length, LINE_WIDTH);
                    axis.Line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg);
                    axis.Line.enabled = length > 1f;
                }
            }

            // 奥を向いているもの (Depth が大きい) から先に描く
            axes.Sort((a, b) => b.Depth.CompareTo(a.Depth));
            int index = 0;
            foreach (var axis in axes)
            {
                if (axis.Depth > 0f && axis.Line != null) { axis.Line.transform.SetSiblingIndex(index++); }
            }
            if (center != null) { center.transform.SetSiblingIndex(index++); }
            foreach (var axis in axes)
            {
                if (axis.Depth <= 0f && axis.Line != null) { axis.Line.transform.SetSiblingIndex(index++); }
                axis.Dot.transform.SetSiblingIndex(index++);
            }
        }
    }
}
