using UniRx;
using UnityEngine;
using VContainer;
using Mediapipe;

public class SpaceInputHandlerForGraphRunnerHandTracking : MonoBehaviour, ISpaceInputHandler
{
    private enum TrackingPoint
    {
        Wrist,
        PalmCenter,
        MiddleMcp,
    }

    [SerializeField] private SerializeInterface<ITimeGetter> timer;
    [SerializeField] private Mediapipe.Unity.Tutorial.HandTrackingWithGraphRunner handTracking;
    [SerializeField] private TrackingPoint trackingPoint = TrackingPoint.PalmCenter;

    [Header("Handedness")]
    [SerializeField] private bool useMediaPipeHandedness = true;
    [SerializeField] private bool invertMediaPipeHandedness;
    [SerializeField, Range(0f, 1f)] private float handednessScoreThreshold = 0.5f;

    [Header("Smoothing")]
    [SerializeField] private bool smoothInput = true;
    [SerializeField, Min(0.01f)] private float smoothingSharpness = 24f;
    [SerializeField, Min(0f)] private float trackingLostTimeout = 0.2f;

    private readonly ReactiveProperty<Vector3> rightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> RightHandPos => rightHandPos;

    private readonly ReactiveProperty<Vector3> leftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => leftHandPos;

    private bool isTracking;
    private ISpaceInputSetter spaceInputSetter;
    private IOptionGetter optionGetter;
    private int lastResultVersion = -1;

    private Vector3 rightHandTarget;
    private Vector3 leftHandTarget;
    private bool hasRightHandTarget;
    private bool hasLeftHandTarget;
    private bool isRightHandInitialized;
    private bool isLeftHandInitialized;
    private float lastRightHandSeenTime = float.NegativeInfinity;
    private float lastLeftHandSeenTime = float.NegativeInfinity;

    [Inject]
    public void Construct(ISpaceInputSetter inputSetter, IOptionGetter optionGetter)
    {
        spaceInputSetter = inputSetter;
        this.optionGetter = optionGetter;
    }

    private void Update()
    {
        ReadData();
        UpdatePositions();
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
            Debug.Log("[MediaPipe] Camera is not connected.");
            return;
        }

        optionGetter.TrackingSettings.CameraIndex = (optionGetter.TrackingSettings.CameraIndex + 1) % devices.Length;
    }

    public void InitializeBodyTracking()
    {
        handTracking.Initialize(optionGetter.TrackingSettings);
    }

    public void StartTracking()
    {
        handTracking.StartTracking();
    }

    private void ReadData()
    {
        if (handTracking.ResultVersion == lastResultVersion)
        {
            UpdateTrackingState();
            return;
        }

        lastResultVersion = handTracking.ResultVersion;

        var landmarks = handTracking.LandmarkList;
        if (landmarks == null || landmarks.Count == 0)
        {
            hasRightHandTarget = false;
            hasLeftHandTarget = false;
            UpdateTrackingState();
            return;
        }

        var handednessList = handTracking.HandednessList;
        var updatedRightHand = false;
        var updatedLeftHand = false;

        for (var i = 0; i < landmarks.Count; i++)
        {
            var handLandmarks = landmarks[i];
            if (handLandmarks == null) { continue; }

            var target = ToTrackingPosition(handLandmarks);
            var handTag = ResolveHandTag(i, handednessList);

            if (optionGetter?.TrackingSettings.IsHandFlipped.Value == true)
            {
                handTag = SwapHandTag(handTag);
            }

            if (handTag == SpaceTrackingTag.RightHand)
            {
                rightHandTarget = target;
                hasRightHandTarget = true;
                updatedRightHand = true;
                lastRightHandSeenTime = Time.realtimeSinceStartup;
            }
            else
            {
                leftHandTarget = target;
                hasLeftHandTarget = true;
                updatedLeftHand = true;
                lastLeftHandSeenTime = Time.realtimeSinceStartup;
            }
        }

        if (!updatedRightHand && IsTrackingLost(lastRightHandSeenTime))
        {
            hasRightHandTarget = false;
        }

        if (!updatedLeftHand && IsTrackingLost(lastLeftHandSeenTime))
        {
            hasLeftHandTarget = false;
        }

        UpdateTrackingState();
    }

    private SpaceTrackingTag ResolveHandTag(int index, System.Collections.Generic.IReadOnlyList<ClassificationList> handednessList)
    {
        if (TryResolveByHandedness(index, handednessList, out var handTag))
        {
            return invertMediaPipeHandedness ? SwapHandTag(handTag) : handTag;
        }

        // Fallback keeps the previous implementation's behavior.
        return index == 0 ? SpaceTrackingTag.RightHand : SpaceTrackingTag.LeftHand;
    }

    private bool TryResolveByHandedness(int index, System.Collections.Generic.IReadOnlyList<ClassificationList> handednessList, out SpaceTrackingTag handTag)
    {
        handTag = SpaceTrackingTag.RightHand;

        if (!useMediaPipeHandedness || handednessList == null || index >= handednessList.Count) { return false; }

        var classifications = handednessList[index]?.Classification;
        if (classifications == null || classifications.Count == 0) { return false; }

        var classification = classifications[0];
        if (classification.Score < handednessScoreThreshold) { return false; }

        if (System.String.Equals(classification.Label, "Right", System.StringComparison.OrdinalIgnoreCase))
        {
            handTag = SpaceTrackingTag.RightHand;
            return true;
        }

        if (System.String.Equals(classification.Label, "Left", System.StringComparison.OrdinalIgnoreCase))
        {
            handTag = SpaceTrackingTag.LeftHand;
            return true;
        }

        return false;
    }

    private static SpaceTrackingTag SwapHandTag(SpaceTrackingTag handTag)
    {
        return handTag == SpaceTrackingTag.RightHand ? SpaceTrackingTag.LeftHand : SpaceTrackingTag.RightHand;
    }

    private Vector3 ToTrackingPosition(NormalizedLandmarkList landmarks)
    {
        return trackingPoint switch
        {
            TrackingPoint.PalmCenter => AverageLandmarks(landmarks, 0, 5, 9, 13, 17),
            TrackingPoint.MiddleMcp => ToVector3(landmarks.Landmark[9]),
            _ => ToVector3(landmarks.Landmark[0]),
        };
    }

    private static Vector3 AverageLandmarks(NormalizedLandmarkList landmarks, params int[] indices)
    {
        var sum = Vector3.zero;
        var count = 0;

        foreach (var index in indices)
        {
            if (index < 0 || index >= landmarks.Landmark.Count) { continue; }

            sum += ToVector3(landmarks.Landmark[index]);
            count++;
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    private static Vector3 ToVector3(NormalizedLandmark landmark)
    {
        return new Vector3(landmark.X, landmark.Y, landmark.Z);
    }

    private void UpdatePositions()
    {
        UpdatePosition(rightHandPos, rightHandTarget, hasRightHandTarget, ref isRightHandInitialized);
        UpdatePosition(leftHandPos, leftHandTarget, hasLeftHandTarget, ref isLeftHandInitialized);
    }

    private void UpdatePosition(ReactiveProperty<Vector3> position, Vector3 target, bool hasTarget, ref bool isInitialized)
    {
        if (!hasTarget) { return; }

        if (!smoothInput || !isInitialized)
        {
            position.Value = target;
            isInitialized = true;
            return;
        }

        var lerpRate = 1f - Mathf.Exp(-smoothingSharpness * Time.deltaTime);
        position.Value = Vector3.Lerp(position.Value, target, lerpRate);
    }

    private bool IsTrackingLost(float lastSeenTime)
    {
        return Time.realtimeSinceStartup - lastSeenTime > trackingLostTimeout;
    }

    private void UpdateTrackingState()
    {
        isTracking = hasRightHandTarget || hasLeftHandTarget;
    }

    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        var currentTime = timer.Value != null ? timer.Value.Time : 0;
        var settings = optionGetter?.TrackingSettings;

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, SpaceInputNormalizer.NormalizeUsedVector2(rightHandPos.Value, settings), currentTime);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, SpaceInputNormalizer.NormalizeUsedVector2(leftHandPos.Value, settings), currentTime);
        spaceInputSetter.SetCanGetSpaceInput(isTracking);
    }
}
