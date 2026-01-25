using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// ƒQ[ƒ€‚ğˆê’â~‚·‚é
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