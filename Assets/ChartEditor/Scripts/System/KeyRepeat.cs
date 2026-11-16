using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// キーの長押しで、同じ操作をくり返す (録音 20261117_024144: ↑ / ↓ で節を移るのを、連打しなくて済むように)
    ///
    /// 押した瞬間に1回。押したまま FIRST_DELAY 秒たったら、INTERVAL 秒ごとに、くり返す。
    /// 毎フレーム Poll を呼ぶこと (呼ばないフレームに離されると、次に押したときまで、くり返しの時刻が残る)。
    /// </summary>
    public class KeyRepeat
    {
        /// <summary>長押しで、くり返しが始まるまでの時間 (秒)</summary>
        public const float FIRST_DELAY = 0.75f;
        /// <summary>くり返しの間隔 (秒)</summary>
        public const float INTERVAL = 0.05f;

        readonly KeyCode key;
        float nextTime = -1f;

        public KeyRepeat(KeyCode key)
        {
            this.key = key;
        }

        /// <summary>
        /// このフレームに、操作を1回するか (押した瞬間と、長押しのくり返し)
        /// </summary>
        public bool Poll()
        {
            return Step(Input.GetKeyDown(key), Input.GetKey(key), Time.unscaledTime);
        }

        /// <summary>
        /// Poll の中身 (Unity の外でも確かめられるように、入力と時刻を渡す)
        /// </summary>
        /// <param name="isDown">このフレームに押したか</param>
        /// <param name="isHeld">押しているか</param>
        /// <param name="now">今の時刻 (秒)</param>
        public bool Step(bool isDown, bool isHeld, float now)
        {
            if (isDown)
            {
                nextTime = now + FIRST_DELAY;
                return true;
            }

            if (!isHeld)
            {
                nextTime = -1f;
                return false;
            }

            if (nextTime < 0f || now < nextTime) { return false; }

            // 1フレームが長くても、まとめて何回も進めない (次は、今から間隔を空ける)
            nextTime += INTERVAL;
            if (nextTime <= now) { nextTime = now + INTERVAL; }
            return true;
        }
    }
}
