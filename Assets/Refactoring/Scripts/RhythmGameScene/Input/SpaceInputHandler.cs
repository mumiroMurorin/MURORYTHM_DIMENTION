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
            // �g���b�L���O����
            Track();

            // �f�[�^��n��
            SendData();
        }

        /// <summary>
        /// KINECT����g���b�L���O���𒸂�
        /// </summary>
        private void Track()
        {
            isTracking = false;
            // ���������Q�Ƃ����Ă��Ȃ��Ƃ��̓_��
            if (_manager == null) return;

            // �����Ől�̐g�̏��̔z��(�܂�͕����l�̐g�̍��W)���󂯎��
            bodies = _manager.GetData();

            if (bodies == null) return;

            // �����l�����l��l�̐g�̏������o��
            foreach (var body in bodies)
            {
                if (body == null) { continue; }
                if (!body.IsTracked) { continue; }

                right_hand_pos = body.Joints[JointType.HandRight].ToVector3();
                left_hand_pos = body.Joints[JointType.HandLeft].ToVector3();

                isTracking = true;
                break;

                // ����̕��ʂ̍��W�̎��o����
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