using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// ƒQ[ƒ€‚ğˆê’â~‚©‚çÄ¶‚·‚é
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