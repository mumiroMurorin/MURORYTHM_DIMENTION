using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class CircleController : MonoBehaviour
    {
        [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;
        [SerializeField] Transform circleParent;

        GameObject currentCircleObject;
        ResultCircleView currentCircleView;

        public void OnChangeMusicData(MusicData musicData)
        {
            if (musicData == null) { return; }

            GenerateCircleIfNeeded(musicData.SymphonyType);
            currentCircleView?.SetJacket(musicData.MusicSprite);
        }

        private void GenerateCircleIfNeeded(SymphonyType symphonyType)
        {
            if (currentCircleObject != null) { return; }

            GameObject circlePrefab = symphonyTypePresentationDatabase?.GetCirclePrefab(symphonyType);
            if (circlePrefab == null)
            {
                Debug.LogWarning($"[CircleController] Circle prefab is not set: {symphonyType}");
                return;
            }

            Transform parent = circleParent != null ? circleParent : transform;
            currentCircleObject = Instantiate(circlePrefab, parent);
            currentCircleObject.transform.localPosition = Vector3.zero;
            currentCircleObject.transform.localRotation = Quaternion.identity;
            currentCircleObject.transform.localScale = Vector3.one;

            currentCircleView = currentCircleObject.GetComponentInChildren<ResultCircleView>(true);
            if (currentCircleView == null)
            {
                currentCircleView = currentCircleObject.AddComponent<ResultCircleView>();
            }
        }
    }

}
