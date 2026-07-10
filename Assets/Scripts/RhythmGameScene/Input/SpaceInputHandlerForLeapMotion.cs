using Leap;
using UniRx;
using UnityEngine;

public class SpaceInputHandlerForLeapMotion : MonoBehaviour, ISpaceInputHandler, ICameraInfoHolder
{
    [SerializeField] private LeapProvider leapProvider;
    [SerializeField] private bool autoFindProvider = true;
    [SerializeField] private bool enableProviderOnAwake;
    [SerializeField] private bool useStabilizedPalmPosition = true;

    private readonly ReactiveProperty<Vector3> rightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> RightHandPos => rightHandPos;

    private readonly ReactiveProperty<Vector3> leftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => leftHandPos;

    private readonly ReactiveProperty<WebCamTexture> emptyWebCamInfo = new ReactiveProperty<WebCamTexture>(null);
    private readonly ReactiveProperty<int> emptyCameraFps = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => emptyWebCamInfo;
    public IReadOnlyReactiveProperty<int> CameraFps => emptyCameraFps;

    private bool isTracking;
    private bool hasRightHand;
    private bool hasLeftHand;
    public bool CanGetRightHand => hasRightHand;
    public bool CanGetLeftHand => hasLeftHand;
    private IOptionGetter optionGetter;

    public void Initialize(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    private void Awake()
    {
        TryFindLeapProvider();
        SetProviderActive(enableProviderOnAwake);
    }

    private void Update()
    {
        ReadData();
    }

    public bool IsExistCamera()
    {
        return leapProvider != null;
    }

    public void InitializeBodyTracking()
    {
        TryFindLeapProvider();
        isTracking = false;
        hasRightHand = false;
        hasLeftHand = false;
    }

    [System.Obsolete]
    public void StartTracking()
    {
        SetProviderActive(true);
    }

    public void SwitchCamera()
    {
        Debug.Log("[LeapMotion] SwitchCamera is not supported by Leap Motion input.");
    }

    private void TryFindLeapProvider()
    {
        if (leapProvider != null || !autoFindProvider) { return; }

        leapProvider = FindAnyObjectByType<LeapProvider>();
    }

    public void SetProviderActive(bool isActive)
    {
        TryFindLeapProvider();

        if (leapProvider == null) { return; }

        leapProvider.enabled = isActive;
        if (!isActive)
        {
            isTracking = false;
            hasRightHand = false;
            hasLeftHand = false;
        }
    }

    private void ReadData()
    {
        TryFindLeapProvider();

        if (leapProvider == null || !leapProvider.enabled || leapProvider.CurrentFrame == null)
        {
            isTracking = false;
            hasRightHand = false;
            hasLeftHand = false;
            return;
        }

        Hand rightHand = leapProvider.CurrentFrame.GetHand(Chirality.Right);
        Hand leftHand = leapProvider.CurrentFrame.GetHand(Chirality.Left);

        bool detectedRightHand = rightHand != null;
        bool detectedLeftHand = leftHand != null;

        if (!detectedRightHand && !detectedLeftHand)
        {
            isTracking = false;
            hasRightHand = false;
            hasLeftHand = false;
            return;
        }

        Vector3 currentRightHandPos = detectedRightHand ? ConvertHandPosition(rightHand) : rightHandPos.Value;
        Vector3 currentLeftHandPos = detectedLeftHand ? ConvertHandPosition(leftHand) : leftHandPos.Value;
        bool isHandFlipped = optionGetter?.TrackingSettings.IsHandFlipped.Value == true;

        if (!isHandFlipped)
        {
            if (detectedRightHand) { rightHandPos.Value = currentRightHandPos; }
            if (detectedLeftHand) { leftHandPos.Value = currentLeftHandPos; }

            hasRightHand = detectedRightHand;
            hasLeftHand = detectedLeftHand;
        }
        else
        {
            if (detectedRightHand) { leftHandPos.Value = currentRightHandPos; }
            if (detectedLeftHand) { rightHandPos.Value = currentLeftHandPos; }

            hasRightHand = detectedLeftHand;
            hasLeftHand = detectedRightHand;
        }

        isTracking = hasRightHand || hasLeftHand;
    }

    private Vector3 ConvertHandPosition(Hand hand)
    {
        Vector3 pos = useStabilizedPalmPosition ? hand.StabilizedPalmPosition : hand.PalmPosition;
        return pos;
    }
}
