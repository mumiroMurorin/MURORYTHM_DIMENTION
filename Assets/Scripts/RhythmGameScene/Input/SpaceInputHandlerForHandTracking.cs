using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using System.Linq;

public class SpaceInputHandlerForHandTracking : MonoBehaviour, ISpaceInputHandler
{
    const int CAPTURE_INDEX = 0;

    [SerializeField] SerializeInterface<ITimeGetter> timer;
    [SerializeField] Mediapipe.Unity.Tutorial.HandTracking handTracking;
    [SerializeField] HandRigDriver hand;


    ReactiveProperty<Vector3> rightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> RightHandPos => rightHandPos;

    ReactiveProperty<Vector3> leftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => leftHandPos;

    bool isTracking;

    ISpaceInputSetter spaceInputSetter;
    ISpaceInputGetter spaceInputGetter;
    IOptionGetter optionGetter;

    [Inject]
    public void Construct(ISpaceInputSetter inputSetter, ISpaceInputGetter inputGetter, IOptionGetter optionGetter)
    {
        spaceInputSetter = inputSetter;
        spaceInputGetter = inputGetter;
        this.optionGetter = optionGetter;
    }

    void Update()
    {
        ReadData();
        SendData();
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
            Debug.Log("yMediaPipezƒJƒƒ‰‚ªÚ‘±‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
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
    /// BodyTracking‚Ìî•ñ‚ğ³‹K‰»
    /// </summary>
    private void ReadData()
    {
        if (handTracking.LandmarkList == null) { isTracking = false; return; }

        if (handTracking.LandmarkList.Count > 0)
        {
            rightHandPos.Value = new Vector3(
                handTracking.LandmarkList[0].Landmark[CAPTURE_INDEX].X,
                handTracking.LandmarkList[0].Landmark[CAPTURE_INDEX].Y,
                handTracking.LandmarkList[0].Landmark[CAPTURE_INDEX].Z
                );
        }
        
        if(handTracking.LandmarkList.Count > 1)
        {
            leftHandPos.Value = new Vector3(
                handTracking.LandmarkList[1].Landmark[CAPTURE_INDEX].X,
                handTracking.LandmarkList[1].Landmark[CAPTURE_INDEX].Y,
                handTracking.LandmarkList[1].Landmark[CAPTURE_INDEX].Z
                );
        }

        isTracking = true;

        // hand.MediapipeWorldPoints = handTracking.LandmarkList[0];

        //Debug.Log($"yMediaPipezRight: {rightHandPos} left: {leftHandPos}");
    }


    /// <summary>
    /// ƒf[ƒ^‚ğ•ÛƒNƒ‰ƒX‚É‘—M
    /// </summary>
    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, SpaceInputNormalizer.NormalizeUsedVector2(rightHandPos.Value, optionGetter?.TrackingSettings), timer.Value != null ? timer.Value.Time : 0);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, SpaceInputNormalizer.NormalizeUsedVector2(leftHandPos.Value, optionGetter?.TrackingSettings), timer.Value != null ? timer.Value.Time : 0);

        spaceInputSetter.SetCanGetSpaceInput(isTracking);
    }
}