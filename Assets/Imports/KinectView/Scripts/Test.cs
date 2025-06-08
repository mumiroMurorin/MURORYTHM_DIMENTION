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

    //トラッキング
    private void Track()
    {
        isTracking = false;
        // そもそも参照が取れていないときはダメ
        if (_manager == null) return;

        // ここで人の身体情報の配列(つまりは複数人の身体座標)を受け取る
        bodies = _manager.GetData();

        if (bodies == null) return;

        // 複数人から一人一人の身体情報を取り出す
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

            // 特定の部位の座標の取り出し方
            //Debug.Log($"Right Hand Position : {body.Joints[JointType.HandRight].ToVector3()}");
        }

        //var body = bodies[0];
        //if (body == null) { return; }

        //Debug.Log(body.IsTracked);

        //right_hand_pos = body.Joints[JointType.HandRight].ToVector3();
        //left_hand_pos = body.Joints[JointType.HandLeft].ToVector3();

        Debug.Log(right_hand_pos + " , " + left_hand_pos);
    }

    //今トラッキングされているかどうかを返す
    public bool IsTracking()
    {
        return isTracking;
    }

    //入力されている値を返す
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