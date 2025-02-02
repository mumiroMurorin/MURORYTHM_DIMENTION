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

        [SerializeField] Vector3 controllerCenter;
        [SerializeField] Vector3 controllerSize;

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

        /// <summary>
        /// -1～1に正規化
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

            // -1～1の範囲に収めて返す
            return new Vector3(
                Mathf.Clamp(normalized.x, -1f, 1f),
                Mathf.Clamp(normalized.y, -1f, 1f),
                Mathf.Clamp(normalized.z, -1f, 1f)
            );
        }

        private void SendData()
        {
            spaceInputSetter?.SetSpaceInput(SpaceTrackingTag.RightHand, Normalize(right_hand_pos), timer.Value.Time);
            spaceInputSetter?.SetSpaceInput(SpaceTrackingTag.LeftHand, Normalize(left_hand_pos), timer.Value.Time);
            spaceInputSetter?.SetCanGetSpaceInput(isTracking);
        }
    }

}