using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 3D編集の向きの印 (SpaceHold3DViewGizmo) の、押せる丸 (シーンに置いてある。カーソルを乗せると明るくなる)
    /// </summary>
    public class GizmoClickTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("丸の絵")]
        [SerializeField] Image image;
        [Tooltip("ふだんの色 (カーソルを乗せていないとき)")]
        [SerializeField] Color color = Color.white;

        /// <summary>押されたとき</summary>
        public Action OnClick { get; set; }

        public Image Image => image;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            OnClick?.Invoke();
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
}
