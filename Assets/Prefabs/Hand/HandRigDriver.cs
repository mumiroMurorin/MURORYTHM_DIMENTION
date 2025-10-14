using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FingerBoneMap
{
    public Transform bone;         // Unity側のボーン
    public int mediapipeIndexA;    // MediaPipe Joint ID (起点)
    public int mediapipeIndexB;    // MediaPipe Joint ID (向かう先)
}

public class HandRigDriver : MonoBehaviour
{
    [SerializeField] FingerBoneMap[] boneMaps;

    // MediaPipeから渡されるワールド座標（例：21関節）
    public Mediapipe.NormalizedLandmarkList MediapipeWorldPoints { private get; set; }

    void Update()
    {
        if(MediapipeWorldPoints == null) { return; }

        for (int i = 0; i < boneMaps.Length; i++)
        {
            var map = boneMaps[i];

            if (MediapipeWorldPoints.Landmark.Count <= map.mediapipeIndexA) { return; }
            if (MediapipeWorldPoints.Landmark.Count <= map.mediapipeIndexB) { return; }

            Vector3 start = new Vector3(
                MediapipeWorldPoints.Landmark[map.mediapipeIndexA].X, 
                MediapipeWorldPoints.Landmark[map.mediapipeIndexA].Y, 
                MediapipeWorldPoints.Landmark[map.mediapipeIndexA].Z
                );
            Vector3 end = new Vector3(
                MediapipeWorldPoints.Landmark[map.mediapipeIndexB].X,
                MediapipeWorldPoints.Landmark[map.mediapipeIndexB].Y,
                MediapipeWorldPoints.Landmark[map.mediapipeIndexB].Z
                );

            Vector3 dir = (end - start).normalized;
            if (dir != Vector3.zero)
            {
                map.bone.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
