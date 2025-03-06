using System.Collections.Generic;
using UnityEngine;

namespace Mediapipe.Unity.Tutorial
{
    public class MultiLandmarkListAnnotationController : MonoBehaviour
    {
        [SerializeField] GameObject _landmarkPrefab;
        [SerializeField] Transform _landmarkParent;
        [SerializeField] float _landMarkDepth = 0.5f;

        List<GameObject> _landmarkList;

        public void DrawLandMark(List<NormalizedLandmarkList> multiLandmarks)
        {
            if (multiLandmarks == null || multiLandmarks.Count == 0) { return; }

            if (_landmarkList == null)
            {
                _landmarkList = new List<GameObject>();
            }

            int landmarkCount = 0;
            for (int i = 0; i < multiLandmarks.Count; i++)
            {
                var landmarks = multiLandmarks[i];

                for (int j = 0; j < landmarks.Landmark.Count; j++) 
                {
                    var landmark = landmarks.Landmark[j];
                    Vector3 position = Camera.main.ViewportToWorldPoint(new Vector3(landmark.X, 1 - landmark.Y, _landMarkDepth));

                    if (_landmarkList.Count <= landmarkCount)
                    {
                        _landmarkList.Add(Instantiate(_landmarkPrefab, _landmarkParent));
                    }

                    _landmarkList[landmarkCount].transform.position = position;
                    landmarkCount++;
                }
            }

            for (int i = landmarkCount; i < _landmarkList.Count; i++) 
            {
                Destroy(_landmarkList[landmarkCount]);
                _landmarkList.RemoveAt(landmarkCount);
            }
        }

        public void SetActiveLandMarks(bool isActive)
        {
            if(_landmarkList == null) { return; }

            foreach (GameObject obj in _landmarkList)
            {
                obj.SetActive(isActive);
            }

        }
    }
}
