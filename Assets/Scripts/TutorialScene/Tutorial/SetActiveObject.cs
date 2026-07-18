using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// ゲームを一時停止する
    /// </summary>
    [System.Serializable]
    public class SetActiveObject : TutorialActionNode
    {
        [SerializeField] string[] activateObjectKeys;
        [SerializeField] string[] deactivateObjectKeys;

        TutorialRuntimeContext context;

        public override void Initialize(TutorialRuntimeContext context)
        {
            this.context = context;
        }

        public override void Do()
        {
            foreach (string key in activateObjectKeys ?? System.Array.Empty<string>())
            {
                context?.SetActive(key, true);
            }

            foreach (string key in deactivateObjectKeys ?? System.Array.Empty<string>())
            {
                context?.SetActive(key, false);
            }

            next?.Do();
        }
    }
}
