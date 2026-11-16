using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 断面の形 (頂点の並び) を、画面部品の中に線で描く (フィルムストリップのサムネイル用)
    ///
    /// ・単位円 (頂点を置ける範囲) を薄い線で描き、その中に、断面の形を描く
    /// ・0番の頂点に、小さな四角を描く (頂点の並びの始まり。ねじれの向きを見るため)
    /// 部品の短い方の辺が、単位円の直径になる。クリックは受け取らない。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class SectionShapeGraphic : MaskableGraphic
    {
        const int CIRCLE_SEGMENTS = 48;

        Vector2[] vertices;
        Color shapeColor = Color.white;
        Color circleColor = new Color(1f, 1f, 1f, 0.16f);
        float lineWidth = 2.5f;
        float padding = 6f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void SetShape(Vector2[] vertices, Color shapeColor)
        {
            this.vertices = vertices;
            this.shapeColor = shapeColor;
            SetVerticesDirty();
        }

        public void SetStyle(float lineWidth, float padding)
        {
            this.lineWidth = lineWidth;
            this.padding = padding;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            float radius = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - padding - lineWidth * 0.5f);
            Vector2 center = rect.center;

            // 頂点を置ける範囲 (単位円)
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float a0 = i / (float)CIRCLE_SEGMENTS * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)CIRCLE_SEGMENTS * Mathf.PI * 2f;
                AddLine(vh, center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius, center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius, circleColor, 1f);
            }

            if (vertices == null || vertices.Length < 2) { return; }

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 from = center + vertices[i] * radius;
                Vector2 to = center + vertices[(i + 1) % vertices.Length] * radius;
                AddLine(vh, from, to, shapeColor, lineWidth);
            }

            // 0番の頂点
            Vector2 first = center + vertices[0] * radius;
            float half = lineWidth * 1.4f;
            AddQuad(vh, first + new Vector2(-half, -half), first + new Vector2(-half, half), first + new Vector2(half, half), first + new Vector2(half, -half), shapeColor);
        }

        static void AddLine(VertexHelper vh, Vector2 from, Vector2 to, Color32 color, float width)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude <= Mathf.Epsilon) { return; }

            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
            // 角が欠けないよう、線の両端を少しだけ伸ばす
            Vector2 extend = delta.normalized * (width * 0.5f);
            AddQuad(vh, from - extend - normal, from - extend + normal, to + extend + normal, to + extend - normal, color);
        }

        static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start + 0);
        }
    }
}
