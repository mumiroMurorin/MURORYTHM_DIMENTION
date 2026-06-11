using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace ChartEditor
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class VerticesShapePreviewGraphic : MaskableGraphic
    {
        [SerializeField] float lineWidth = 6f;
        [SerializeField] float padding = 10f;
        [ReadOnly][SerializeField] Vector2[] vertices;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void SetVertices(Vector2[] vertices)
        {
            if (vertices == null)
            {
                this.vertices = null;
            }
            else
            {
                this.vertices = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    this.vertices[i] = vertices[i];
                }
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (vertices == null || vertices.Length < 2)
            {
                return;
            }

            var rect = rectTransform.rect;
            float radius = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - padding - lineWidth * 0.5f);
            Vector2 center = rect.center;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 from = center + vertices[i] * radius;
                Vector2 to = center + vertices[(i + 1) % vertices.Length] * radius;
                AddLine(vh, from, to, color, lineWidth);
            }
        }

        static void AddLine(VertexHelper vh, Vector2 from, Vector2 to, Color32 color, float width)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
            int startIndex = vh.currentVertCount;

            vh.AddVert(from - normal, color, Vector2.zero);
            vh.AddVert(from + normal, color, Vector2.zero);
            vh.AddVert(to + normal, color, Vector2.zero);
            vh.AddVert(to - normal, color, Vector2.zero);

            vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
        }
    }
}
