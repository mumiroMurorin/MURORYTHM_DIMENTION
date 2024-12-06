using UnityEngine;
using Windows.Kinect;

public class KinectInput : MonoBehaviour
{
    [SerializeField] private BodySourceManager _manager;
    [SerializeField] private GameObject right_obj;
    [SerializeField] private GameObject left_obj;
    [SerializeField] private bool isHand;

    private Body[] bodies;

    private Vector3 right_hand_pos = Vector3.zero;
    private Vector3 left_hand_pos = Vector3.zero;

    private bool isUseKinect;
    private bool isTracking;

    private void Start()
    {
        
    }

    private void Update()
    {
        Track();
        //Debug.Log(IsTracking());
    }

    //RootOption���Z�b�g
    public void SetRootOption(RootOption r)
    {
        isUseKinect = r.isUseKinect;
    }

    //�g���b�L���O
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

            if (isHand)
            {
                right_obj.transform.position = right_hand_pos;
                left_obj.transform.position = left_hand_pos;
            }

            isTracking = true;
            break;

            // ����̕��ʂ̍��W�̎��o����
            //Debug.Log($"Right Hand Position : {body.Joints[JointType.HandRight].ToVector3()}");
        }
    }

    //���g���b�L���O����Ă��邩�ǂ�����Ԃ�
    public bool IsReturnTracking()
    {
        return ((isUseKinect && isTracking) || !isUseKinect);
    }

    //���͂���Ă���l��Ԃ�
    public Vector3 ReturnHandPos(bool isRight)
    {
        if(isRight) { return right_hand_pos; }
        return left_hand_pos;
    }
}

//public static class JointExtensions
//{
//    public static Vector3 ToVector3(this Windows.Kinect.Joint joint)
//       => new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
//}