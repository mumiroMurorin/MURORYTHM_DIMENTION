using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面の「全体マップ」(譜面全体の縮小図。シーンに置いてある画面部品)
    ///
    /// 縦長の縮小図に、今の再生位置の印を重ねて表示する (下が譜面の先頭、上が末尾)。
    /// 縮小図の上をクリック・ドラッグすると、その場所へ移動するよう知らせる。
    ///
    /// 位置・大きさ・色はシーン上で調整できる。縮小図の絵と、表示する値は ChartOverviewPresenter から受け取る。
    /// </summary>
    public class ChartOverviewView : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Tooltip("縮小図を映す画像。この大きさに合わせて、縮小図の絵が作られる")]
        [SerializeField] RawImage mapImage;
        [Tooltip("今の再生位置の印。縮小図の子にして、縮小図の中を上下に動かす")]
        [SerializeField] RectTransform marker;
        [Tooltip("今いる小節の番号を出す文字")]
        [SerializeField] TextMeshProUGUI barTmp;

        // 今の印の位置 (同じ位置のときは、動かし直さない)
        float markerProgress = -1f;

        /// <summary>
        /// 縮小図の上がクリック・ドラッグされたとき。引数は、譜面のどのあたりか (0 = 先頭、1 = 末尾)
        /// </summary>
        public event Action<float> OnSeekRequestedListener;

        /// <summary>
        /// 縮小図を映す場所の大きさ (この大きさで縮小図の絵を作ると、画面の高さ 1080 のとき、絵の1点が画面の1点になる)
        /// </summary>
        public Vector2Int MapPixelSize
        {
            get
            {
                if (mapImage == null) { return Vector2Int.zero; }

                Rect rect = mapImage.rectTransform.rect;
                return new Vector2Int(Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));
            }
        }

        /// <summary>
        /// 全体マップを出す・隠す
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible) { gameObject.SetActive(isVisible); }
        }

        /// <summary>
        /// 縮小図の絵を表示する
        /// </summary>
        public void SetMapTexture(Texture texture)
        {
            if (mapImage == null) { return; }

            mapImage.texture = texture;
            // シーン上では、絵が無いあいだ白い板が見えないよう、透明にしてある
            mapImage.color = Color.white;
        }

        /// <summary>
        /// 今の再生位置の印を動かす
        /// </summary>
        /// <param name="progress">譜面のどのあたりか (0 = 先頭、1 = 末尾)</param>
        public void SetMarker(float progress)
        {
            if (marker == null) { return; }

            progress = Mathf.Clamp01(progress);
            if (progress == markerProgress) { return; }
            markerProgress = progress;

            // 縮小図の下の端から上の端までの間で、高さを合わせる (横は縮小図いっぱい)
            marker.anchorMin = new Vector2(0f, progress);
            marker.anchorMax = new Vector2(1f, progress);
            marker.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 今いる小節の番号を表示する
        /// </summary>
        public void SetBarText(string text)
        {
            if (barTmp != null) { barTmp.text = text; }
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Seek(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            Seek(eventData);
        }

        /// <summary>
        /// カーソルのある高さが、縮小図の下から何割のところかを求めて、そこへの移動を知らせる
        /// </summary>
        private void Seek(PointerEventData eventData)
        {
            // 左ボタンだけ
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            if (mapImage == null) { return; }

            RectTransform mapRect = mapImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) { return; }

            Rect rect = mapRect.rect;
            if (rect.height <= 0f) { return; }

            // 縮小図の外(上下の余白)を押したときは、端として扱う
            float progress = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);
            OnSeekRequestedListener?.Invoke(progress);
        }
    }
}
