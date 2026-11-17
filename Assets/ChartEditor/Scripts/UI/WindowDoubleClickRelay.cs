using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    /// <summary>
    /// 欄のダブルクリックを知らせる (MinimizableWindow が、起動したときに欄へ付ける)
    ///
    /// クリックは、押した所から親へ向かって、最初にクリックを受け取る部品に届く。
    /// ボタン・トグル・入力欄の上のクリックは、それらが受け取るので、ここには届かない (背景や文字の上のクリックだけが届く)。
    /// スクロールバーやスライダーは、押したことは受け取るが、クリックは受け取らないので、クリックがここまで届く。
    /// そういうクリックは数えない (押したことを、この欄ではないものが受け取ったクリック)。
    /// </summary>
    public class WindowDoubleClickRelay : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>ダブルクリックされたとき</summary>
        public event Action OnDoubleClicked;

        /// <summary>ここの上 (その子も含む) のクリックは数えない (ミニプレビューの映像など)</summary>
        public RectTransform[] IgnoredAreas { get; set; }

        // (ダブルクリックを見分ける部品は System/DoubleClickDetector.cs。ウィンドウでは、決まった値 (0.35 秒、高さ 1080 で 8 ピクセル) を使う)
        readonly DoubleClickDetector detector = new DoubleClickDetector();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            // 押したことを、欄の中の部品 (スクロールバーなど) が受け取っていたときは数えない
            if (eventData.pointerPress != null && eventData.pointerPress != gameObject) { return; }
            // 数えない所 (ミニプレビューの映像など) の上のクリック
            if (IsInIgnoredArea(eventData.pointerPressRaycast.gameObject)) { return; }

            // 画面の大きさに合わせて、ずれてよい距離を変える (高さ 1080 のときの値が基準)
            float scale = Screen.height > 0 ? Screen.height / 1080f : 1f;
            if (detector.Register(Time.unscaledTime, eventData.position, scale)) { OnDoubleClicked?.Invoke(); }
        }

        private bool IsInIgnoredArea(GameObject pressed)
        {
            if (pressed == null || IgnoredAreas == null) { return false; }

            foreach (var area in IgnoredAreas)
            {
                if (area != null && pressed.transform.IsChildOf(area)) { return true; }
            }
            return false;
        }
    }
}
