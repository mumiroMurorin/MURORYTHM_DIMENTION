using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class SpaceInputHandlerForHandTracking : MonoBehaviour, ISpaceInputHandler, ICameraInfoHolder
{
    private const int CAPTURE_INDEX = 0;

    [SerializeField] private Mediapipe.Unity.Tutorial.HandTracking handTracking;
    [SerializeField] private HandRigDriver hand;

    private readonly ReactiveProperty<Vector3> rightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> RightHandPos => rightHandPos;

    private readonly ReactiveProperty<Vector3> leftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => leftHandPos;

    public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => handTracking != null ? handTracking.WebCamInfo : emptyWebCamInfo;
    public IReadOnlyReactiveProperty<int> CameraFps => handTracking != null ? handTracking.CameraFps : emptyCameraFps;

    private readonly ReactiveProperty<WebCamTexture> emptyWebCamInfo = new ReactiveProperty<WebCamTexture>(null);
    private readonly ReactiveProperty<int> emptyCameraFps = new ReactiveProperty<int>(0);

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
        handTracking.Initialize(optionGetter.TrackingSettings);
    }

    [System.Obsolete]
    public void StartTracking()
    {
        handTracking.StartTracking();
    }

    /// <summary>
    /// BodyTrackingの情報を正規化
    /// </summary>
    private void ReadData()
    {
        if (handTracking.LandmarkList == null)
        {
            isTracking = false;
            CanGetRightHand = false;
            CanGetLeftHand = false;
            return;
        }

        CanGetRightHand = handTracking.LandmarkList.Count > 0;
        CanGetLeftHand = handTracking.LandmarkList.Count > 1;

        if (handTracking.LandmarkList.Count > 0)
        {
            rightHandPos.Value = new Vector3(
                handTracking.LandmarkList[0].Landmark[CAPTURE_INDEX].X,
                handTracking.LandmarkList[0].Landmark[CAPTURE_INDEX].Y,
                handTracking.LandmarkList[0].Landmark[CAPTURE_INDEX].Z
            );
        }

        if (handTracking.LandmarkList.Count > 1)
        {
            leftHandPos.Value = new Vector3(
                handTracking.LandmarkList[1].Landmark[CAPTURE_INDEX].X,
                handTracking.LandmarkList[1].Landmark[CAPTURE_INDEX].Y,
                handTracking.LandmarkList[1].Landmark[CAPTURE_INDEX].Z
            );
        }

        isTracking = true;
    }
}
