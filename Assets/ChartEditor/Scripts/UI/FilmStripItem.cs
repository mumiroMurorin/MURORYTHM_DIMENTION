using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// フィルムストリップのサムネイル1つぶん (押された・ドラッグされたを知らせる)
    /// シーンのひな形 (SpaceHoldFilmStripView の itemTemplate) を、節の数だけ複製して使う。
    /// </summary>
    public class FilmStripItem : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("サムネイルの背景 (今編集している節は青、カーソルを乗せると明るく)")]
        [SerializeField] Image background;
        [Tooltip("節の形の絵")]
        [SerializeField] SectionShapeGraphic shape;
        [Tooltip("下の文字 (節の種類。ドラッグ中は、動かす先の分線)")]
        [SerializeField] TextMeshProUGUI label;

        Vector2[] shownVertices;
        Color shownColor;
        bool isDragging;

        public int Index { get; set; }
        public bool IsHovered { get; private set; }

        public Action<int> OnClicked { get; set; }
        public Action<int, Vector2> OnBeginDragged { get; set; }
        public Action<int, Vector2> OnDragged { get; set; }
        public Action<int> OnEndDragged { get; set; }

        /// <summary>
        /// 形を描き直す (形も色も変わっていなければ、何もしない)
        /// </summary>
        public void SetShape(Vector2[] vertices, Color color)
        {
            if (IsSame(vertices, shownVertices) && color == shownColor) { return; }

            shownVertices = vertices != null ? (Vector2[])vertices.Clone() : null;
            shownColor = color;
            shape.SetShape(shownVertices, color);
        }

        public void SetBackground(Color color)
        {
            if (background.color != color) { background.color = color; }
        }

        public void SetLabel(string text, Color color)
        {
            if (label.text != text) { label.text = text; }
            if (label.color != color) { label.color = color; }
        }

        private static bool IsSame(Vector2[] a, Vector2[] b)
        {
            if (a == null || b == null) { return a == b; }
            if (a.Length != b.Length) { return false; }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) { return false; }
            }
            return true;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || isDragging) { return; }
            OnClicked?.Invoke(Index);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            isDragging = true;
            OnBeginDragged?.Invoke(Index, eventData.pressPosition);
            OnDragged?.Invoke(Index, eventData.position);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!isDragging) { return; }
            OnDragged?.Invoke(Index, eventData.position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) { return; }
            isDragging = false;
            OnEndDragged?.Invoke(Index);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) { IsHovered = true; }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) { IsHovered = false; }

        private void OnDisable()
        {
            IsHovered = false;
            isDragging = false;
        }
    }
}
