using Mediapipe;
using UnityEngine;

namespace Mediapipe.Unity.Tutorial
{
    public static class MediaPipeResourceManagerProvider
    {
        private static ResourceManager resourceManager;

        public static ResourceManager Instance
        {
            get
            {
                if (resourceManager == null)
                {
                    resourceManager = new StreamingAssetsResourceManager();
                }

                return resourceManager;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            resourceManager = null;
        }
    }
}
