using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// </summary>
    public class SpaceHold3DViewGizmo
    {
        const float RADIUS = 44f;
        const float POSITIVE_SIZE = 22f;
        const float NEGATIVE_SIZE = 13f;
        const float CENTER_SIZE = 16f;
        const float LINE_WIDTH = 3f;

        static readonly Color negativeColor = new Color(0.62f, 0.62f, 0.66f, 0.9f);
        static readonly Color centerColor = new Color(0.82f, 0.82f, 0.86f, 1f);

        /// <summary>軸の先が押されたとき (押した先の向き。例: +X なら Vector3.right)</summary>
        public Action<Vector3> OnAxisClicked { get; set; }

        /// <summary>まんなかが押されたとき</summary>
        public Action OnCenterClicked { get; set; }

        class Axis
        {
            public Vector3 Direction;
            public bool IsPositive;
            public Image Line;
            public Image Dot;
            public float Depth;
        }

        readonly RectTransform root;
        readonly List<Axis> axes = new List<Axis>();
        readonly Image center;

        /// <param name="parent">印を置く場所 (この部品のまんなかに描く)</param>
        public SpaceHold3DViewGizmo(RectTransform parent)
        {
            root = EditorRuntimeUi.CreateRect("ViewGizmo", parent);
            EditorRuntimeUi.Stretch(root);

            AddAxis(Vector3.right, true, SpaceHold3DColors.AxisX, "X");
            AddAxis(Vector3.up, true, SpaceHold3DColors.AxisY, "Y");
            AddAxis(Vector3.forward, true, SpaceHold3DColors.AxisZ, "Z");
            AddAxis(Vector3.left, false, negativeColor, null);
            AddAxis(Vector3.down, false, negativeColor, null);
            AddAxis(Vector3.back, false, negativeColor, null);

            center = EditorRuntimeUi.CreateImage("Center", root, centerColor, true);
            center.sprite = EditorRuntimeUi.CircleSprite;
            Place(center.rectTransform, Vector2.zero, CENTER_SIZE);
            AddClick(center.gameObject, () => OnCenterClicked?.Invoke(), centerColor);
        }

        private void AddAxis(Vector3 direction, bool isPositive, Color color, string label)
        {
            var axis = new Axis { Direction = direction, IsPositive = isPositive };

            if (isPositive)
            {
                axis.Line = EditorRuntimeUi.CreateImage("Line", root, color);
                var lineRect = axis.Line.rectTransform;
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0f, 0.5f);
            }

            axis.Dot = EditorRuntimeUi.CreateImage(isPositive ? label : "Negative", root, color, true);
            axis.Dot.sprite = EditorRuntimeUi.CircleSprite;
            Place(axis.Dot.rectTransform, Vector2.zero, isPositive ? POSITIVE_SIZE : NEGATIVE_SIZE);

            if (!string.IsNullOrEmpty(label))
            {
                var text = EditorRuntimeUi.CreateLabel("Label", axis.Dot.rectTransform, label, 13f, TextAlignmentOptions.Center, new Color(0.08f, 0.08f, 0.1f, 1f));
                text.fontStyle = FontStyles.Bold;
                EditorRuntimeUi.Stretch(text.rectTransform);
            }

            Vector3 clickedDirection = direction;
            AddClick(axis.Dot.gameObject, () => OnAxisClicked?.Invoke(clickedDirection), color);
            axes.Add(axis);
        }

        private static void Place(RectTransform rect, Vector2 position, float size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
        }

        private static void AddClick(GameObject target, Action onClick, Color color)
        {
            var click = target.AddComponent<GizmoClickTarget>();
            click.Initialize(target.GetComponent<Image>(), color, onClick);
        }

        /// <summary>
        /// 今のカメラの向きに合わせて、軸の先を置き直す
        /// </summary>
        public void Refresh(Quaternion cameraRotation)
        {
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
            center.transform.SetSiblingIndex(index++);
            foreach (var axis in axes)
            {
                if (axis.Depth <= 0f && axis.Line != null) { axis.Line.transform.SetSiblingIndex(index++); }
                axis.Dot.transform.SetSiblingIndex(index++);
            }
        }
    }

    /// <summary>
    /// 向きの印の、押せる丸 (カーソルを乗せると明るくなる)
    /// </summary>
    public class GizmoClickTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        Image image;
        Color color;
        Action onClick;

        public void Initialize(Image image, Color color, Action onClick)
        {
            this.image = image;
            this.color = color;
            this.onClick = onClick;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            onClick?.Invoke();
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (image != null) { image.color = Color.Lerp(color, Color.white, 0.45f); }
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (image != null) { image.color = color; }
        }

        private void OnDisable()
        {
            if (image != null) { image.color = color; }
        }
    }

    /// <summary>
    /// SpaceHold の3D編集で使う、軸の色 (横: 赤、縦: 緑、時間: 青)
    /// </summary>
    public static class SpaceHold3DColors
    {
        public static readonly Color AxisX = new Color(1f, 0.32f, 0.3f, 1f);
        public static readonly Color AxisY = new Color(0.4f, 1f, 0.45f, 1f);
        public static readonly Color AxisZ = new Color(0.35f, 0.62f, 1f, 1f);
    }
}
