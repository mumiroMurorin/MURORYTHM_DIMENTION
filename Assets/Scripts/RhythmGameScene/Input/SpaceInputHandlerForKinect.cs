using UnityEngine;
using Windows.Kinect;
using UniRx;

public class SpaceInputHandlerForKinect : MonoBehaviour, ISpaceInputHandler, ICameraInfoHolder
{
    [SerializeField] private BodySourceManager _manager;

    [SerializeField] private Vector3 controllerCenter;
    [SerializeField] private Vector3 controllerSize;

    private Body[] bodies;

    private readonly ReactiveProperty<Vector3> rightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> RightHandPos => rightHandPos;

    private readonly ReactiveProperty<Vector3> leftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => leftHandPos;

    private readonly ReactiveProperty<WebCamTexture> emptyWebCamInfo = new ReactiveProperty<WebCamTexture>(null);
    private readonly ReactiveProperty<int> emptyCameraFps = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => emptyWebCamInfo;
    public IReadOnlyReactiveProperty<int> CameraFps => emptyCameraFps;

    private bool isTracking;
    public bool CanGetRightHand => isTracking;
    public bool CanGetLeftHand => isTracking;

    public void Initialize(IOptionGetter optionGetter)
    {
    }

    private void Update()
    {
        Track();
    }

    public bool IsExistCamera()
    {
        return _manager != null;
    }

    public void InitializeBodyTracking()
    {
        isTracking = false;
    }

    public void StartTracking()
    {
        isTracking = false;
    }

    public void SwitchCamera()
    {
        Debug.Log("[Kinect] SwitchCamera is not supported.");
    }

    /// <summary>
    /// Acquire tracking data from Kinect.
    /// </summary>
    private void Track()
    {
        isTracking = false;
        if (_manager == null) return;

        bodies = _manager.GetData();

        if (bodies == null) return;

        foreach (var body in bodies)
        {
            if (body == null) { continue; }
            if (!body.IsTracked) { continue; }

            rightHandPos.Value = body.Joints[JointType.HandRight].ToVector3();
            leftHandPos.Value = body.Joints[JointType.HandLeft].ToVector3();

            isTracking = true;
            break;
        }
    }
}
