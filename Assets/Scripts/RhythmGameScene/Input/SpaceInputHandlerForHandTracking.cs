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
            Debug.Log("ÅyMediaPipeÅzÉJÉÅÉâÇ™ê⁄ë±Ç≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ");
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
    /// BodyTrackingÇÃèÓïÒÇê≥ãKâª
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

        //Debug.Log($"ÅyMediaPipeÅzRight: {rightHandPos} left: {leftHandPos}");
    }

    /// <summary>
    /// -1Å`1Ç…ê≥ãKâª
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Vector3 NormalizeUsedVector2(Vector3 pos)
    {
        // êFÅXã·ñ°ÇµÇΩåãâ XYÇÃÇ›ÇégópÇ∑ÇÈÇ±Ç∆Ç…
        Vector3 controllerLeft = optionGetter.TrackingSettings.ControllerLeftEdge.Value;
        controllerLeft = new Vector3(controllerLeft.x, controllerLeft.y);
        Vector3 controllerRight = optionGetter.TrackingSettings.ControllerRightEdge.Value;
        controllerRight = new Vector3(controllerRight.x, controllerRight.y);

        Vector3 center = controllerLeft + (controllerRight - controllerLeft) / 2f;
        Vector3 controllerLowerCenter = optionGetter.TrackingSettings.ControllerLowerCenter.Value;
        controllerLowerCenter = new Vector3(controllerLowerCenter.x, controllerLowerCenter.y);
        Vector3 controllerUpperCenter = controllerLowerCenter + (center - controllerLowerCenter) * 2f;

        Vector2 xy = new Vector2(NormalizeScalar(controllerLeft, controllerRight, pos), NormalizeScalar(controllerLowerCenter, controllerUpperCenter, pos));
        xy = Vector2.ClampMagnitude(xy, 1f);

        return new Vector3(xy.x, xy.y, 0f);
    }

    /// <summary>
    /// -1Å`1Ç…ê≥ãKâª
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Vector3 NormalizeUsedVector3(Vector3 pos)
    {
        Vector3 controllerLeft = optionGetter.TrackingSettings.ControllerLeftEdge.Value;
        Vector3 controllerRight = optionGetter.TrackingSettings.ControllerRightEdge.Value;

        Vector3 center = controllerLeft + (controllerRight - controllerLeft) / 2f;
        Vector3 controllerLowerCenter = optionGetter.TrackingSettings.ControllerLowerCenter.Value;
        Vector3 controllerUpperCenter = controllerLowerCenter + (center - controllerLowerCenter) * 2f;

        Vector2 xy = new Vector2(NormalizeScalar(controllerLeft, controllerRight, pos), NormalizeScalar(controllerLowerCenter, controllerUpperCenter, pos));
        xy = Vector2.ClampMagnitude(xy, 1f);

        return new Vector3(xy.x, xy.y, 0f);
    }

    public static float NormalizeScalar(Vector3 posA, Vector3 posB, Vector3 targetPos)
    {
        if((posA - posB).sqrMagnitude == 0) { return 0; }

        Vector3 baseVec = posB - posA;
        Vector3 toTarget = targetPos - posA;

        float ratio = Vector3.Dot(toTarget, baseVec.normalized) / baseVec.magnitude;

        return Mathf.Lerp(-1, 1, ratio);
    }

    /// <summary>
    /// ÉfÅ[É^Çï€éùÉNÉâÉXÇ…ëóêM
    /// </summary>
    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, NormalizeUsedVector2(rightHandPos.Value), timer.Value != null ? timer.Value.Time : 0);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, NormalizeUsedVector2(leftHandPos.Value), timer.Value != null ? timer.Value.Time : 0);

        spaceInputSetter.SetCanGetSpaceInput(isTracking);
    }
}