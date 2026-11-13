using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// プレビューの上で範囲選択をしている間、その範囲を表す枠を画面に重ねて描く
    /// (編集画面の範囲選択と同じ見た目: 半透明の青い面と、青いふち)
    /// </summary>
    public class PreviewRangeRect
    {
        // ほかの画面部品よりも手前に出す (頂点のつまみよりも手前、読み込み中の覆いよりは奥)
        const int SORTING_ORDER = 20001;
        // ふちの太さ(ピクセル)
        const float BORDER_THICKNESS = 2f;

        static readonly Color fillColor = new Color(0f, 0.5f, 1f, 0.25f);
        static readonly Color borderColor = Color.blue;

        GameObject canvasObject;
        RectTransform fill;
        // 下・上・左・右のふち
        readonly RectTransform[] borders = new RectTransform[4];

        /// <summary>
        /// 枠を表示する
        /// </summary>
        /// <param name="screenRect">画面上の範囲 (左下が原点)</param>
        public void Show(Rect screenRect)
        {
            EnsureCanvas();
            if (!canvasObject.activeSelf) { canvasObject.SetActive(true); }

            float thickness = Mathf.Min(BORDER_THICKNESS, Mathf.Min(screenRect.width, screenRect.height));

            Place(fill, screenRect.xMin, screenRect.yMin, screenRect.width, screenRect.height);
            Place(borders[0], screenRect.xMin, screenRect.yMin, screenRect.width, thickness);
            Place(borders[1], screenRect.xMin, screenRect.yMax - thickness, screenRect.width, thickness);
            Place(borders[2], screenRect.xMin, screenRect.yMin, thickness, screenRect.height);
            Place(borders[3], screenRect.xMax - thickness, screenRect.yMin, thickness, screenRect.height);
        }

        /// <summary>
        /// 枠を隠す
        /// </summary>
        public void Hide()
        {
            if (canvasObject != null && canvasObject.activeSelf) { canvasObject.SetActive(false); }
        }

        /// <summary>
        /// 後片付け
        /// </summary>
        public void Dispose()
        {
            if (canvasObject != null) { Object.Destroy(canvasObject); }

            canvasObject = null;
            fill = null;
        }

        private static void Place(RectTransform rectTransform, float x, float y, float width, float height)
        {
            // 画面に直接重ねる土台の上では、位置がそのまま画面上のピクセルになる
            rectTransform.position = new Vector3(x, y, 0f);
            rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        /// <summary>
        /// 枠を描くための、画面に重ねる土台を作る (最初の1回だけ)
        /// </summary>
        private void EnsureCanvas()
        {
            if (canvasObject != null) { return; }

            canvasObject = new GameObject("PreviewRangeRect");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;
            // クリックを受け取る部品(GraphicRaycaster)は付けない。枠は、下にある画面部品へのクリックを遮らない

            fill = CreateImage("Fill", fillColor);
            for (int i = 0; i < borders.Length; i++)
            {
                borders[i] = CreateImage("Border", borderColor);
            }
        }

        private RectTransform CreateImage(string name, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(canvasObject.transform, false);

            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            // 左下の角を基準に置く
            var rectTransform = image.rectTransform;
            rectTransform.pivot = Vector2.zero;
            return rectTransform;
        }
    }
}
