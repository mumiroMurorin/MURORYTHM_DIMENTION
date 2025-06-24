using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

public class SpaceInputHandlerForMediaPipe : MonoBehaviour, ISpaceInputHandler
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
        if (bodyTracking.LandmarkList == null) { isTracking = false; return; }

        // 通常
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
        // 反転
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
        //Debug.Log($"【MediaPipe】Right: {rightHandPos} left: {leftHandPos}");
    }

    /// <summary>
    /// -1～1に正規化
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Vector3 Normalize(Vector3 pos)
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
    /// データを保持クラスに送信
    /// </summary>
    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, Normalize(rightHandPos.Value), timer.Value != null ? timer.Value.Time : 0);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, Normalize(leftHandPos.Value), timer.Value != null ? timer.Value.Time : 0);
        spaceInputSetter.SetCanGetSpaceInput(isTracking);
    }
}

public interface ISpaceInputHandler
{
    void InitializeBodyTracking();

    void StartTracking();
}

/// <summary>
/// トラッキングに関する設定項目まとめクラス
/// </summary>
[System.Serializable]
public class BodyTrackingSettings
{
    [Header("トラッキングの左右反転")]
    [SerializeField] ReactiveProperty<bool> isHorizontallyFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsHorizontallyFlipped => isHorizontallyFlipped;
    public void SetIsHorizontallyFlipped(bool isFlipped)
    {
        isHorizontallyFlipped.Value = isFlipped;
    }

    [Header("手の左右識別反転")]
    [SerializeField] ReactiveProperty<bool> isHandFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsHandFlipped => isHandFlipped;
    public void SetIsHandFlipped(bool isFlipped)
    {
        isHandFlipped.Value = isFlipped;
    }

    [Header("トラッキングの上下反転")]
    [SerializeField] ReactiveProperty<bool> isVerticallyFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsVerticallyFlipped => isVerticallyFlipped;
    public void SetIsVerticallyFlipped(bool isFlipped)
    {
        isVerticallyFlipped.Value = isFlipped;
    }

    [Header("筐体真ん中(7番と8番の間)")]
    [SerializeField] ReactiveProperty<Vector3> controllerLowerCenter = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerLowerCenter => controllerLowerCenter;
    public void SetControllerLowerCenter(Vector3 pos)
    {
        controllerLowerCenter.Value = pos;
    }

    [Header("筐体左端(0番)")]
    [SerializeField] ReactiveProperty<Vector3> controllerLeftEdge = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerLeftEdge => controllerLeftEdge;
    public void SetControllerLeftEdge(Vector3 pos)
    {
        controllerLeftEdge.Value = pos;
    }

    [Header("筐体右端(15番)")]
    [SerializeField] ReactiveProperty<Vector3> controllerRightEdge = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerRightEdge => controllerRightEdge;
    public void SetControllerRightEdge(Vector3 pos)
    {
        controllerRightEdge.Value = pos;
    }

    /// <summary>
    /// 引数のインスタンスをこのインスタンスにディープコピー
    /// </summary>
    /// <param name="origin"></param>
    public void CopyOption(BodyTrackingSettings origin)
    {
        SetIsHorizontallyFlipped(origin.IsHorizontallyFlipped.Value);
        SetIsVerticallyFlipped(origin.IsVerticallyFlipped.Value);
        SetIsHandFlipped(origin.isHandFlipped.Value);
        SetControllerLeftEdge(origin.controllerLeftEdge.Value);
        SetControllerRightEdge(origin.controllerRightEdge.Value);
        SetControllerLowerCenter(origin.controllerLowerCenter.Value);
    }
}