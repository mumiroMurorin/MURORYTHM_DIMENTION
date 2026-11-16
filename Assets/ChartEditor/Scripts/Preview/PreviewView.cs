using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// プレビューを映している1つの画面
    ///
    /// ・編集画面のミニプレビュー: カメラの映像を画面部品(RawImage)に映している
    /// ・全画面プレビュー: カメラが画面全体に直接映している
    /// どちらでも「画面上の位置 ⇔ プレビュー世界の位置」を同じ呼び方で行き来できるようにする。
    /// </summary>
    public class PreviewView
    {
        // 映像を映している画面部品 (全画面プレビューでは無い)
        readonly RawImage image;
        readonly Camera camera;
        // 画面部品の四隅を求めるときの作業用
        readonly Vector3[] corners = new Vector3[4];

        /// <summary>
        /// 画面全体に直接映しているか (全画面プレビュー)
        /// </summary>
        public bool IsFullScreen { get; }

        /// <summary>
        /// 編集画面のミニプレビュー
        /// </summary>
        public PreviewView(RawImage image, Camera camera)
        {
            this.image = image;
            this.camera = camera;
            IsFullScreen = false;
        }

        /// <summary>
        /// 全画面プレビュー
        /// </summary>
        public PreviewView(Camera camera)
        {
            this.camera = camera;
            IsFullScreen = true;
        }

        /// <summary>
        /// 今、画面に出ているか
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (camera == null || !camera.isActiveAndEnabled) { return false; }
                if (IsFullScreen) { return camera.targetTexture == null; }

                if (image == null || !image.isActiveAndEnabled) { return false; }

                // ミニプレビューの窓をしまっている (透明にしてある) 間は、出ていないものとする
                // (見えない映像の上で、ノーツを選んだり、頂点のつまみを出したりしないように)
                image.GetComponentsInParent(false, canvasGroups);
                foreach (var group in canvasGroups)
                {
                    if (group.alpha <= 0f) { return false; }
                    if (group.ignoreParentGroups) { break; }
                }
                return true;
            }
        }

        // 窓が透明かどうかを調べるときの作業用
        readonly List<CanvasGroup> canvasGroups = new List<CanvasGroup>();

        /// <summary>
        /// 画面上の位置で、このプレビューを操作できるか
        /// (ボタンやパネルが上に重なっている場所は、そちらへの操作として扱う)
        /// </summary>
        /// <param name="raycastResults">作業用のリスト (毎回確保しないよう使い回す)</param>
        public bool IsPointerOn(Vector2 screenPosition, List<RaycastResult> raycastResults)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) { return true; }

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            raycastResults.Clear();
            eventSystem.RaycastAll(pointerData, raycastResults);

            bool isOn;
            if (IsFullScreen)
            {
                // 全画面プレビュー: ボタン・スライダー・入力欄・スクロール欄と、メニューのキャンバスの画面部品
                // (メニューバー、開いた一覧、譜面情報の欄、クイックメニュー) の上でなければ操作できる
                // (背景の板など、操作できない画面部品は無視する)
                isOn = true;
                foreach (var result in raycastResults)
                {
                    if (result.gameObject == null) { continue; }
                    if (result.gameObject.GetComponentInParent<Selectable>() != null ||
                        result.gameObject.GetComponentInParent<ScrollRect>() != null ||
                        EditorMenuBar.IsOnMenuCanvas(result.gameObject))
                    {
                        isOn = false;
                        break;
                    }
                }
            }
            else
            {
                // ミニプレビュー: 映像がいちばん手前の画面部品であること
                isOn = raycastResults.Count > 0 && image != null && raycastResults[0].gameObject == image.gameObject;
            }

            raycastResults.Clear();
            return isOn;
        }

        /// <summary>
        /// 画面上の位置を通る、プレビュー世界の光線を返す
        /// </summary>
        /// <param name="isInsideOnly">true のとき、映像の外の位置なら false を返す</param>
        public bool TryGetRay(Vector2 screenPosition, bool isInsideOnly, out Ray ray)
        {
            ray = default;
            if (camera == null) { return false; }

            if (IsFullScreen)
            {
                if (isInsideOnly && !camera.pixelRect.Contains(screenPosition)) { return false; }

                ray = camera.ScreenPointToRay(screenPosition);
                return true;
            }

            if (image == null) { return false; }

            // 画面上の位置 → 画面部品の中での位置
            RectTransform rectTransform = image.rectTransform;
            Camera uiCamera = GetUICamera();

            if (isInsideOnly && !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, uiCamera)) { return false; }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPosition)) { return false; }

            // 画面部品の中での位置 → 映像の中での位置 (左下が0、右上が1)
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) { return false; }

            float u = (localPosition.x - rect.xMin) / rect.width;
            float v = (localPosition.y - rect.yMin) / rect.height;

            Rect uvRect = image.uvRect;
            u = uvRect.x + u * uvRect.width;
            v = uvRect.y + v * uvRect.height;

            ray = camera.ViewportPointToRay(new Vector3(u, v, 0f));
            return true;
        }

        /// <summary>
        /// プレビュー世界の位置が、画面上のどこに映っているかを返す (TryGetRay の逆)
        /// </summary>
        /// <returns>視点の後ろにあるとき、映像の外にあるときは false</returns>
        public bool TryGetScreenPoint(Vector3 worldPosition, out Vector2 screenPosition)
        {
            screenPosition = default;
            if (camera == null) { return false; }

            // 映像の中での位置 (左下が0、右上が1。z は視点からの奥行き)
            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);
            if (viewportPoint.z <= camera.nearClipPlane) { return false; }
            if (viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f) { return false; }

            if (IsFullScreen)
            {
                Rect pixelRect = camera.pixelRect;
                screenPosition = new Vector2(
                    pixelRect.x + viewportPoint.x * pixelRect.width,
                    pixelRect.y + viewportPoint.y * pixelRect.height);
                return true;
            }

            if (image == null) { return false; }

            // 映像の中での位置 → 画面部品の中での位置
            Rect uvRect = image.uvRect;
            if (uvRect.width == 0f || uvRect.height == 0f) { return false; }

            float u = (viewportPoint.x - uvRect.x) / uvRect.width;
            float v = (viewportPoint.y - uvRect.y) / uvRect.height;
            if (u < 0f || u > 1f || v < 0f || v > 1f) { return false; }

            RectTransform rectTransform = image.rectTransform;
            Rect rect = rectTransform.rect;
            Vector3 localPosition = new Vector3(rect.xMin + u * rect.width, rect.yMin + v * rect.height, 0f);

            // 画面部品の中での位置 → 画面上の位置
            screenPosition = RectTransformUtility.WorldToScreenPoint(GetUICamera(), rectTransform.TransformPoint(localPosition));
            return true;
        }

        /// <summary>
        /// このプレビューの映像が、画面上で占めている範囲を返す (左下が原点)
        /// </summary>
        public bool TryGetScreenRect(out Rect screenRect)
        {
            screenRect = default;
            if (camera == null) { return false; }

            if (IsFullScreen)
            {
                screenRect = camera.pixelRect;
                return true;
            }

            if (image == null) { return false; }

            // 画面部品の四隅が、画面上のどこにあるか
            Camera uiCamera = GetUICamera();
            image.rectTransform.GetWorldCorners(corners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return screenRect.width > 0f && screenRect.height > 0f;
        }

        /// <summary>
        /// 画面部品を映しているカメラ (画面に直接重ねるキャンバスでは null)
        /// </summary>
        private Camera GetUICamera()
        {
            Canvas canvas = image != null ? image.canvas : null;
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }
    }
}
