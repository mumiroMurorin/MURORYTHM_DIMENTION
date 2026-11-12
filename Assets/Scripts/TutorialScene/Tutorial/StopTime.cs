using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// ゲームを一時停止する
    /// </summary>
    [System.Serializable]
    public class StopTime : TutorialActionNode
    {
        public override void Do()
        {
            Time.timeScale = 0f;
            next?.Do();
        }
    }
}