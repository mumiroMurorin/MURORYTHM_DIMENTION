using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ホールドを曲げる制御点 (ひし形の印と、印から両側の節へ伸びる細い線) の表示
    ///
    /// 印と線は、シーン(HoldCurveCanvas)に最初から置いてある。制御点は、隣り合った2つの節の間に1つずつ。
    /// 表示するのは、選んだ節と次の節の間のものと、前の節と選んだ節の間のものの、最大2つ。
    /// ここでは、渡された画面上の位置に合わせて、置き直すだけを行う。
    /// どの節の間に制御点を出すか、印を掴んだかどうかは、HoldCurveEditor が決める。
    ///
    /// 制御点はレーンの外へも動かせるので、印は、ほかのどのパネルよりも手前に描く (専用のキャンバス)。
    /// 印の四角い範囲は、クリックを受け取る (印の下にあるボタンが、一緒に押されてしまわないように)。
    /// 線は、クリックを受け取らない。
    /// </summary>
    public class HoldCurveHandlesView : MonoBehaviour
    {
        [Header("選んだ節と、次の節の間の制御点")]
        [SerializeField] RectTransform markerToNext;
        [SerializeField] Graphic markerFillToNext;
        [SerializeField] RectTransform startLineToNext;
        [SerializeField] RectTransform endLineToNext;
        [Header("前の節と、選んだ節の間の制御点")]
        [SerializeField] RectTransform markerToBack;
        [SerializeField] Graphic markerFillToBack;
        [SerializeField] RectTransform startLineToBack;
        [SerializeField] RectTransform endLineToBack;

        static readonly Color normalColor = new Color(1f, 0.82f, 0.24f, 1f);
        static readonly Color highlightColor = new Color(1f, 1f, 1f, 1f);
        // 線は、ノーツが見えるように、少し透かす
        const float LINE_ALPHA = 0.65f;

        // カーソルの下にあるものを調べるときの作業用
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        PointerEventData pointerData;
        EventSystem pointerEventSystem;

        /// <summary>
        /// 印の半径 (画面のピクセル数)。印を掴める範囲に使う
        /// </summary>
        public float MarkerRadius
        {
            get
            {
                if (markerToNext == null) { return 0f; }
                return markerToNext.rect.width * 0.5f * markerToNext.lossyScale.x;
            }
        }

        /// <summary>
        /// 画面のその位置で、いちばん手前にある画面部品が、印か
        /// (印よりも手前に、ほかのボタンやパネルがあるときは false。画面部品が何も無いときは true)
        /// </summary>
        public bool IsMarkerInFront(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) { return true; }

            if (pointerData == null || pointerEventSystem != eventSystem)
            {
                pointerData = new PointerEventData(eventSystem);
                pointerEventSystem = eventSystem;
            }
            pointerData.position = screenPosition;

            raycastResults.Clear();
            eventSystem.RaycastAll(pointerData, raycastResults);

            // 結果は、手前にあるものから順に並んでいる。最初に見つかった画面部品を調べる
            foreach (var result in raycastResults)
            {
                if (!(result.module is GraphicRaycaster)) { continue; }

                GameObject hit = result.gameObject;
                return (markerToNext != null && hit == markerToNext.gameObject) || (markerToBack != null && hit == markerToBack.gameObject);
            }
            return true;
        }

        /// <summary>
        /// 制御点を1つ、表示する
        /// </summary>
        /// <param name="isToNext">選んだ節と次の節の間のものか (false のときは、前の節と選んだ節の間のもの)</param>
        /// <param name="startPosition">始まりの節 (時間が早い方) の中心の、画面上の位置</param>
        /// <param name="endPosition">終わりの節の中心の、画面上の位置</param>
        /// <param name="markerPosition">印の、画面上の位置</param>
        /// <param name="isHighlighted">印にカーソルが乗っている、または掴んでいる</param>
        public void Show(bool isToNext, Vector2 startPosition, Vector2 endPosition, Vector2 markerPosition, bool isHighlighted)
        {
            RectTransform marker = isToNext ? markerToNext : markerToBack;
            Graphic fill = isToNext ? markerFillToNext : markerFillToBack;
            RectTransform startLine = isToNext ? startLineToNext : startLineToBack;
            RectTransform endLine = isToNext ? endLineToNext : endLineToBack;
            if (marker == null) { return; }

            Color color = isHighlighted ? highlightColor : normalColor;
            Color lineColor = new Color(color.r, color.g, color.b, LINE_ALPHA);

            // このキャンバスは画面にそのまま重なっているので、position が画面上の位置になる
            if (!marker.gameObject.activeSelf) { marker.gameObject.SetActive(true); }
            marker.position = markerPosition;
            if (fill != null && fill.color != color) { fill.color = color; }

            PlaceLine(startLine, markerPosition, startPosition, lineColor);
            PlaceLine(endLine, markerPosition, endPosition, lineColor);
        }

        /// <summary>
        /// 制御点を1つ、隠す
        /// </summary>
        public void Hide(bool isToNext)
        {
            SetActive(isToNext ? markerToNext : markerToBack, false);
            SetActive(isToNext ? startLineToNext : startLineToBack, false);
            SetActive(isToNext ? endLineToNext : endLineToBack, false);
        }

        /// <summary>
        /// 線を、from から to まで引く (線の左端が回転の中心になっている)
        /// </summary>
        private static void PlaceLine(RectTransform line, Vector2 from, Vector2 to, Color color)
        {
            if (line == null) { return; }
            SetActive(line, true);

            Vector2 direction = to - from;
            line.position = from;
            line.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            float scale = line.lossyScale.x;
            line.sizeDelta = new Vector2(scale > 0f ? direction.magnitude / scale : 0f, line.sizeDelta.y);

            if (line.TryGetComponent(out Graphic graphic) && graphic.color != color) { graphic.color = color; }
        }

        private static void SetActive(RectTransform target, bool isActive)
        {
            if (target != null && target.gameObject.activeSelf != isActive) { target.gameObject.SetActive(isActive); }
        }
    }
}
