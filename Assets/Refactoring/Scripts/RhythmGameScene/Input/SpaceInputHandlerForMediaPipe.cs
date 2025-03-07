using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using VContainer;

namespace Refactoring
{
    public class SpaceInputHandlerForMediaPipe : MonoBehaviour
    {
        const int RIGHT_HAND_INDEX = 19;
        const int LEFT_HAND_INDEX = 20;

        [SerializeField] SerializeInterface<ITimeGetter> timer;

        [SerializeField] Mediapipe.Unity.Tutorial.BodyTracking bodyTracking;
        [SerializeField] Vector3 controllerCenter;
        [SerializeField] Vector3 controllerSize;

        Vector3 right_hand_pos = Vector3.zero;
        Vector3 left_hand_pos = Vector3.zero;

        bool isTracking;

        ISpaceInputSetter spaceInputSetter;

        [Inject]
        public void Construct(ISpaceInputSetter inputSetter)
        {
            spaceInputSetter = inputSetter;
        }

        void Update()
        {
            SetData();
            SendData();
        }

        private void SetData()
        {
            if(bodyTracking.LandmarkList == null) { isTracking = false; return; }

            right_hand_pos = new Vector3(
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].X, 
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Y, 
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Z
                );

            left_hand_pos = new Vector3(
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].X, 
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Y, 
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Z
                );

            isTracking = true;
            // Debug.Log($"ÅyMediaPipeÅzRight: {right_hand_pos} left: {left_hand_pos}");
        }

        /// <summary>
        /// -1Å`1Ç…ê≥ãKâª
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Vector3 Normalize(Vector3 pos)
        {
            Vector3 normalized = new Vector3(
                (pos.x - controllerCenter.x) / (controllerSize.x / 2f),
                (pos.y - controllerCenter.y) / (controllerSize.y / 2f),
                (pos.z - controllerCenter.z) / (controllerSize.z / 2f)
            );

            // -1Å`1ÇÃîÕàÕÇ…é˚ÇﬂÇƒï‘Ç∑
            return new Vector3(
                Mathf.Clamp(normalized.x, -1f, 1f),
                Mathf.Clamp(normalized.y, -1f, 1f),
                Mathf.Clamp(normalized.z, -1f, 1f)
            );
        }

        private void SendData()
        {
            if (spaceInputSetter == null) { return; }

            spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, Normalize(right_hand_pos), timer.Value.Time);
            spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, Normalize(left_hand_pos), timer.Value.Time);
            spaceInputSetter.SetCanGetSpaceInput(isTracking);
        }
    }

}