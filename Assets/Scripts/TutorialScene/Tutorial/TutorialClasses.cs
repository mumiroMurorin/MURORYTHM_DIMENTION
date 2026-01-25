using UnityEngine;


namespace Tutorial
{
    public enum TutorialActionType
    {
        StopTime,
        ResumeTime,
        Wait,
        Speech,
    }

    [System.Serializable]
    public abstract class TutorialActionNode
    {
        protected TutorialActionNode next;
        public void SetNextNode(TutorialActionNode node) { next = node; }

        public abstract void Do();
    }
}