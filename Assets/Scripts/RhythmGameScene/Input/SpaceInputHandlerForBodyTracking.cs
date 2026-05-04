using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

public class SpaceInputHandlerForBodyTracking : MonoBehaviour, ISpaceInputHandler
{
    const int RIGHT_HAND_INDEX = 19;
    const int LEFT_HAND_INDEX = 20;

    [SerializeField] SerializeInterface<ITimeGetter> timer;
    [SerializeField] Mediapipe.Unity.Tutorial.BodyTracking bodyTracking;

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
            Debug.Log("ÅyMediaPipeÅzÉJÉÅÉâÇ™ê⁄ë±Ç≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ");
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
    /// BodyTrackingÇÃèÓïÒÇê≥ãKâª
    /// </summary>
    private void ReadData()
    {
        if (bodyTracking.LandmarkList == null) { isTracking = false; return; }

        // í èÌ
        if(optionGetter == null || !optionGetter.TrackingSettings.IsHandFlipped.Value)
        {
            rightHandPos.Value = new Vector3(
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].X,
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Y,
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Z
                );

            leftHandPos.Value = new Vector3(
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].X,
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Y,
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Z
                );
        }
        // îΩì]
        else
        {
            leftHandPos.Value = new Vector3(
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].X,
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Y,
                bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Z
              );

            rightHandPos.Value = new Vector3(
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].X,
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Y,
                bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Z
                );
        }

        isTracking = true;
        //Debug.Log($"ÅyMediaPipeÅzRight: {rightHandPos} left: {leftHandPos}");
    }


    /// <summary>
    /// ÉfÅ[É^Çï€éùÉNÉâÉXÇ…ëóêM
    /// </summary>
    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, SpaceInputNormalizer.NormalizeUsedVector2(rightHandPos.Value, optionGetter?.TrackingSettings), timer.Value != null ? timer.Value.Time : 0);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, SpaceInputNormalizer.NormalizeUsedVector2(leftHandPos.Value, optionGetter?.TrackingSettings), timer.Value != null ? timer.Value.Time : 0);

        spaceInputSetter.SetCanGetSpaceInput(isTracking);
    }
}