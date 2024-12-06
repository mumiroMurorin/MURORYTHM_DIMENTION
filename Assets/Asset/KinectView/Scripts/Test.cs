using UnityEngine;
using Windows.Kinect;

public class Test : MonoBehaviour
{
    [SerializeField] private BodySourceManager _manager;

    private Body[] bodies;
    private bool isTracking;

    private Vector3 right_hand_pos = Vector3.zero;
    private Vector3 left_hand_pos = Vector3.zero;

    private void Start()
    {

    }

    private void Update()
    {
        Track();
        Debug.Log(IsTracking());
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
        int num = 0;
        foreach (var body in bodies)
        {
            num++;

            if (body == null) { continue; }
            if (!body.IsTracked) { continue; }

            right_hand_pos = body.Joints[JointType.HandRight].ToVector3();
            left_hand_pos = body.Joints[JointType.HandLeft].ToVector3();

            isTracking = true;
            break;

            // ����̕��ʂ̍��W�̎��o����
            //Debug.Log($"Right Hand Position : {body.Joints[JointType.HandRight].ToVector3()}");
        }

        //var body = bodies[0];
        //if (body == null) { return; }

        //Debug.Log(body.IsTracked);

        //right_hand_pos = body.Joints[JointType.HandRight].ToVector3();
        //left_hand_pos = body.Joints[JointType.HandLeft].ToVector3();

        Debug.Log(right_hand_pos + " , " + left_hand_pos);
    }

    //���g���b�L���O����Ă��邩�ǂ�����Ԃ�
    public bool IsTracking()
    {
        return isTracking;
    }

    //���͂���Ă���l��Ԃ�
    public Vector3 ReturnHandPos(bool isRight)
    {
        if (isRight) { return right_hand_pos; }
        return left_hand_pos;
    }
}

public static class JointExtensions
{
    public static Vector3 ToVector3(this Windows.Kinect.Joint joint)
        => new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
}