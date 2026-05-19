using UnityEngine;
using UniRx;

public class SpaceInputHandlerForBodyTracking : MonoBehaviour, ISpaceInputHandler, ICameraInfoHolder
{
    private const int RIGHT_HAND_INDEX = 19;
    private const int LEFT_HAND_INDEX = 20;

    [SerializeField] private Mediapipe.Unity.Tutorial.BodyTracking bodyTracking;

    private readonly ReactiveProperty<Vector3> rightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> RightHandPos => rightHandPos;

    private readonly ReactiveProperty<Vector3> leftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => leftHandPos;

    private readonly ReactiveProperty<WebCamTexture> emptyWebCamInfo = new ReactiveProperty<WebCamTexture>(null);
    private readonly ReactiveProperty<int> emptyCameraFps = new ReactiveProperty<int>(0);

    public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => bodyTracking != null ? bodyTracking.WebCamInfo : emptyWebCamInfo;
    public IReadOnlyReactiveProperty<int> CameraFps => bodyTracking != null ? bodyTracking.CameraFps : emptyCameraFps;

    public bool CanGetRightHand { get; private set; }
    public bool CanGetLeftHand { get; private set; }

    private bool isTracking;
    private IOptionGetter optionGetter;

    public void Initialize(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    private void Update()
    {
        ReadData();
    }

    public bool IsExistCamera()
    {
        return WebCamTexture.devices.Length > 0;
    }

    public void SwitchCamera()
    {
        var devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("【MediaPipe】カメラが接続されていません");
            return;
        }

        optionGetter.TrackingSettings.CameraIndex = (optionGetter.TrackingSettings.CameraIndex + 1) % devices.Length;
    }

    public void InitializeBodyTracking()
    {
        bodyTracking.Initialize(optionGetter.TrackingSettings);
    }

    [System.Obsolete]
    public void StartTracking()
    {
        bodyTracking.StartTracking();
    }

    /// <summary>
    /// BodyTrackingの情報を正規化
    /// </summary>
    private void ReadData()
    {
        var landmarkList = bodyTracking.LandmarkList;
        if (landmarkList == null || landmarkList.Landmark == null || landmarkList.Landmark.Count <= LEFT_HAND_INDEX)
        {
            isTracking = false;
            CanGetRightHand = false;
            CanGetLeftHand = false;
            return;
        }

        CanGetRightHand = true;
        CanGetLeftHand = true;

        if (optionGetter == null || !optionGetter.TrackingSettings.IsHandFlipped.Value)
        {
            rightHandPos.Value = new Vector3(
                landmarkList.Landmark[RIGHT_HAND_INDEX].X,
                landmarkList.Landmark[RIGHT_HAND_INDEX].Y,
                landmarkList.Landmark[RIGHT_HAND_INDEX].Z
            );

            leftHandPos.Value = new Vector3(
                landmarkList.Landmark[LEFT_HAND_INDEX].X,
                landmarkList.Landmark[LEFT_HAND_INDEX].Y,
                landmarkList.Landmark[LEFT_HAND_INDEX].Z
            );
        }
        else
        {
            leftHandPos.Value = new Vector3(
                landmarkList.Landmark[RIGHT_HAND_INDEX].X,
                landmarkList.Landmark[RIGHT_HAND_INDEX].Y,
                landmarkList.Landmark[RIGHT_HAND_INDEX].Z
            );

            rightHandPos.Value = new Vector3(
                landmarkList.Landmark[LEFT_HAND_INDEX].X,
                landmarkList.Landmark[LEFT_HAND_INDEX].Y,
                landmarkList.Landmark[LEFT_HAND_INDEX].Z
            );
        }

        isTracking = true;
    }
}
