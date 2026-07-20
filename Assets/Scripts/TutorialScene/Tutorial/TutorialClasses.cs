using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
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

        public virtual void Initialize(TutorialRuntimeContext context) { }

        public abstract void Do();
    }

    public class TutorialRuntimeContext
    {
        readonly Dictionary<string, GameObject> sceneObjects = new Dictionary<string, GameObject>();

        public TutorialRuntimeContext(SpeechBubbleTutorial speechBubble, IDisposer disposer, TutorialSceneObjectReference[] objectReferences, TableReference textTableReference)
        {
            SpeechBubble = speechBubble;
            Disposer = disposer;
            TextTableReference = textTableReference;

            if (objectReferences == null) { return; }

            foreach (TutorialSceneObjectReference reference in objectReferences)
            {
                if (reference == null || string.IsNullOrWhiteSpace(reference.Key) || reference.Target == null) { continue; }
                sceneObjects[reference.Key] = reference.Target;
            }
        }

        public SpeechBubbleTutorial SpeechBubble { get; }
        public IDisposer Disposer { get; }
        public TableReference TextTableReference { get; }

        public void SetActive(string key, bool active)
        {
            if (string.IsNullOrWhiteSpace(key)) { return; }

            if (!sceneObjects.TryGetValue(key, out GameObject target) || target == null)
            {
                Debug.LogWarning($"[TutorialRuntimeContext] Scene object key was not found: {key}");
                return;
            }

            target.SetActive(active);
        }
    }

    [Serializable]
    public class TutorialSceneObjectReference
    {
        [SerializeField] string key;
        [SerializeField] GameObject target;

        public string Key => key;
        public GameObject Target => target;
    }
}
