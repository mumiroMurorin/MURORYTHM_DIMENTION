using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// ÉQÅ[ÉÄÇàÍéûí‚é~Ç∑ÇÈ
    /// </summary>
    [System.Serializable]
    public class SetActiveObject : TutorialActionNode
    {
        [SerializeField] GameObject[] activateObjects;
        [SerializeField] GameObject[] deactivateObjects;

        public override void Do()
        {
            foreach(var obj in activateObjects)
            {
                obj?.SetActive(true);
            }

            foreach (var obj in deactivateObjects)
            {
                obj?.SetActive(false);
            }

            next?.Do();
        }
    }
}