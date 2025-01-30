using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using VContainer;

namespace Refactoring
{
    public class SpaceInputHandler : MonoBehaviour
    {
        [SerializeField] BodySourceManager _manager;
        [SerializeField] SerializeInterface<ITimeGetter> timer;

        Body[] bodies;

        Vector3 right_hand_pos = Vector3.zero;
        Vector3 left_hand_pos = Vector3.zero;

        bool isTracking;

        ISpaceInputSetter spaceInputSetter;

        [Inject]
        public void Inject(ISpaceInputSetter inputSetter)
        {
            spaceInputSetter = inputSetter;
        }

        void Update()
        {
            // トラッキングして
            Track();

            // データを渡す
            SendData();
        }

        /// <summary>
        /// KINECTからトラッキング情報を頂く
        /// </summary>
        private void Track()
        {
            isTracking = false;
            // そもそも参照が取れていないときはダメ
            if (_manager == null) return;

            // ここで人の身体情報の配列(つまりは複数人の身体座標)を受け取る
            bodies = _manager.GetData();

            if (bodies == null) return;

            // 複数人から一人一人の身体情報を取り出す
            foreach (var body in bodies)
            {
                if (body == null) { continue; }
                if (!body.IsTracked) { continue; }

                right_hand_pos = body.Joints[JointType.HandRight].ToVector3();
                left_hand_pos = body.Joints[JointType.HandLeft].ToVector3();

                isTracking = true;
                break;

                // 特定の部位の座標の取り出し方
                //Debug.Log($"Right Hand Position : {body.Joints[JointType.HandRight].ToVector3()}");
            }
        }

        private void SendData()
        {
            spaceInputSetter?.SetSpaceInput(SpaceTrackingTag.RightHand, right_hand_pos, timer.Value.Time);
            spaceInputSetter?.SetSpaceInput(SpaceTrackingTag.LeftHand, left_hand_pos, timer.Value.Time);
            spaceInputSetter?.SetCanGetSpaceInput(isTracking);
        }
    }

}