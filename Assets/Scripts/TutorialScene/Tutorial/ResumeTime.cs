using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// ゲームを一時停止から再生する
    /// </summary>
    [System.Serializable]
    public class ResumeTime : TutorialActionNode
    {
        public override void Do()
        {
            Time.timeScale = 1f;
            next?.Do();
        }
    }
}