using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// クリックが続けて2回あったか (ダブルクリック) を見分ける
    ///
    /// 使う側が、数えるクリックのたびに Click (または Register) を呼ぶ。
    /// 数えないクリック (受け付けない状態のとき、ノーツを押したときなど) のあとは、Reset を呼んで、1回目から数え直す。
    /// ダブルクリックになったときは、次のクリックを1回目として数え直す (3回続けて押しても、2回目と3回目でもう一度ダブルクリックにならない)。
    ///
    /// 使っている所: ウィンドウの最小化 (WindowDoubleClickRelay)、接続・タイプ変更の道具から編集の道具に戻る (InputHandler)、
    /// ミニプレビューから全画面プレビューにする (PreviewNotePicker)、ホールドの単独編集 (HoldIsolation)
    /// </summary>
    public class DoubleClickDetector
    {
        /// <summary>2回のクリックの間の、いちばん長い時間 (秒)</summary>
        public const float MAX_INTERVAL = 0.35f;
        /// <summary>2回のクリックの位置の、いちばん大きいずれ (画面の高さ 1080 のときのピクセル数。ウィンドウの最小化)</summary>
        public const float MAX_DISTANCE = 8f;
        /// <summary>譜面やプレビューの上のダブルクリックで使う、位置のいちばん大きいずれ (ピクセル)</summary>
        public const float POINTER_MAX_DISTANCE = 12f;

        readonly float maxInterval;
        readonly float maxDistance;

        bool hasFirstClick;
        float firstClickTime;
        Vector2 firstClickPosition;

        public DoubleClickDetector() : this(MAX_INTERVAL, MAX_DISTANCE)
        {
        }

        /// <param name="maxInterval">2回のクリックの間の、いちばん長い時間 (秒)</param>
        /// <param name="maxDistance">2回のクリックの位置の、いちばん大きいずれ (ピクセル)</param>
        public DoubleClickDetector(float maxInterval, float maxDistance)
        {
            this.maxInterval = maxInterval;
            this.maxDistance = maxDistance;
        }

        /// <summary>
        /// クリックを知らせる
        /// </summary>
        /// <param name="time">クリックした時刻 (秒)</param>
        /// <param name="position">クリックした位置 (画面のピクセル)</param>
        /// <param name="scale">画面の大きさの倍率 (高さ 1080 のとき 1)。ずれてよい距離に掛ける</param>
        /// <param name="isSameTarget">前のクリックと同じものを押したか (違うものを押したときは、ダブルクリックにしない)</param>
        /// <returns>このクリックで、ダブルクリックになったとき true</returns>
        public bool Register(float time, Vector2 position, float scale = 1f, bool isSameTarget = true)
        {
            if (isSameTarget &&
                hasFirstClick &&
                time - firstClickTime <= maxInterval &&
                time >= firstClickTime &&
                Vector2.Distance(position, firstClickPosition) <= maxDistance * scale)
            {
                hasFirstClick = false;
                return true;
            }

            hasFirstClick = true;
            firstClickTime = time;
            firstClickPosition = position;
            return false;
        }

        /// <summary>
        /// 今のクリックを知らせる (時刻は Time.unscaledTime。ずれてよい距離は、画面の大きさで変えない)
        /// </summary>
        /// <param name="position">クリックした位置 (画面のピクセル)</param>
        /// <param name="isSameTarget">前のクリックと同じものを押したか (違うものを押したときは、ダブルクリックにしない)</param>
        /// <returns>このクリックで、ダブルクリックになったとき true</returns>
        public bool Click(Vector2 position, bool isSameTarget = true)
        {
            return Register(Time.unscaledTime, position, 1f, isSameTarget);
        }

        /// <summary>
        /// 次のクリックを、1回目として数え直す
        /// </summary>
        public void Reset()
        {
            hasFirstClick = false;
        }
    }
}
