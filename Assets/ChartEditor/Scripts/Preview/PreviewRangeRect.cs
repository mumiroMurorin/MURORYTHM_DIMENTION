using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// プレビューの上で範囲選択をしている間、その範囲を表す枠を画面に重ねて描く
    /// (編集画面の範囲選択と同じ見た目: 半透明の青い面と、青いふち)
    /// 枠はシーンに置いてある (EditorOverlays の RangeRect。頂点のつまみよりも手前、読み込み中の覆いよりは奥)
    /// </summary>
    public class PreviewRangeRect
    {
        // ふちの太さ(ピクセル)
        const float BORDER_THICKNESS = 2f;

        /// <summary>
        /// 枠を表示する
        /// </summary>
        /// <param name="screenRect">画面上の範囲 (左下が原点)</param>
        public void Show(Rect screenRect)
        {
            var overlays = EditorOverlays.Instance;
            if (overlays == null || overlays.RangeRect == null) { return; }

            var fill = overlays.RangeFill;
            var borders = overlays.RangeBorders;
            if (fill == null || borders == null || borders.Length < 4) { return; }

            if (!overlays.RangeRect.activeSelf) { overlays.RangeRect.SetActive(true); }

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
            var overlays = EditorOverlays.Instance;
            if (overlays != null && overlays.RangeRect != null && overlays.RangeRect.activeSelf) { overlays.RangeRect.SetActive(false); }
        }

        /// <summary>
        /// 後片付け (枠はシーンのものなので、隠すだけ)
        /// </summary>
        public void Dispose()
        {
            Hide();
        }

        private static void Place(RectTransform rectTransform, float x, float y, float width, float height)
        {
            if (rectTransform == null) { return; }

            // 画面に直接重ねる土台の上では、位置がそのまま画面上のピクセルになる (枠の部品は、左下の角が基準)
            rectTransform.position = new Vector3(x, y, 0f);
            rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }
    }
}
