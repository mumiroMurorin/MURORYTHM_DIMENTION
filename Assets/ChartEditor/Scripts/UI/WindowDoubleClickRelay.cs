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

        readonly DoubleClickDetector detector = new DoubleClickDetector();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            // 押したことを、欄の中の部品 (スクロールバーなど) が受け取っていたときは数えない
            if (eventData.pointerPress != null && eventData.pointerPress != gameObject) { return; }

            // 画面の大きさに合わせて、ずれてよい距離を変える (高さ 1080 のときの値が基準)
            float scale = Screen.height > 0 ? Screen.height / 1080f : 1f;
            if (detector.Register(Time.unscaledTime, eventData.position, scale)) { OnDoubleClicked?.Invoke(); }
        }
    }

    /// <summary>
    /// クリックが続けて2回あったか (ダブルクリック) を見分ける
    /// </summary>
    public class DoubleClickDetector
    {
        /// <summary>2回のクリックの間の、いちばん長い時間 (秒)</summary>
        public const float MAX_INTERVAL = 0.35f;
        /// <summary>2回のクリックの位置の、いちばん大きいずれ (画面の高さ 1080 のときのピクセル数)</summary>
        public const float MAX_DISTANCE = 8f;

        bool hasFirstClick;
        float firstClickTime;
        Vector2 firstClickPosition;

        /// <summary>
        /// クリックを知らせる
        /// </summary>
        /// <param name="time">クリックした時刻 (秒)</param>
        /// <param name="position">クリックした位置 (画面のピクセル)</param>
        /// <param name="scale">画面の大きさの倍率 (高さ 1080 のとき 1)</param>
        /// <returns>このクリックで、ダブルクリックになったとき true (3回目のクリックは、また1回目として数える)</returns>
        public bool Register(float time, Vector2 position, float scale = 1f)
        {
            if (hasFirstClick &&
                time - firstClickTime <= MAX_INTERVAL &&
                time >= firstClickTime &&
                Vector2.Distance(position, firstClickPosition) <= MAX_DISTANCE * scale)
            {
                hasFirstClick = false;
                return true;
            }

            hasFirstClick = true;
            firstClickTime = time;
            firstClickPosition = position;
            return false;
        }
    }
}
