using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class TimeCounter : MonoBehaviour, ITimeGetter, ITimeController
    {
        float time;
        bool isCounting;

        public float Time { get { return time; } }

        public void StartTimer()
        {
            time = 0;
            isCounting = true;
        }

        public void StopTimer()
        {
            isCounting = false;
        }

        private void Update()
        {
            // 仮
            if (Input.GetKeyDown(KeyCode.Return)) { StartTimer(); }

            if (isCounting) { time += UnityEngine.Time.deltaTime; }
        }
    }

    /// <summary>
    /// タイマースタート、ストップする
    /// </summary>
    public interface ITimeController
    {
        void StartTimer();

        void StopTimer();
    }

    public interface ITimeGetter
    {
        float Time { get; }
    }

}
