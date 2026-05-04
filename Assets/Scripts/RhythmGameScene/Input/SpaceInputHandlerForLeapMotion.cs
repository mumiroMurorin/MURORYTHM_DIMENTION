using Leap;
using UniRx;
using UnityEngine;
using VContainer;

public class SpaceInputHandlerForLeapMotion : MonoBehaviour, ISpaceInputHandler
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;
    [SerializeField] LeapProvider leapProvider;
    [SerializeField] bool autoFindProvider = true;
    [SerializeField] bool useStabilizedPalmPosition = true;
    //[SerializeField] Vector3 inputScale = Vector3.one;
    //[SerializeField] Vector3 inputOffset = Vector3.zero;

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

    void Awake()
    {
        TryFindLeapProvider();
    }

    void Update()
    {
        ReadData();
        SendData();
    }

    public bool IsExistCamera()
    {
        return leapProvider != null;
    }

    public void InitializeBodyTracking()
    {
        TryFindLeapProvider();
        isTracking = false;
    }

    [System.Obsolete]
    public void StartTracking()
    {
        TryFindLeapProvider();

        if (leapProvider != null)
        {
            leapProvider.enabled = true;
        }
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

    private void ReadData()
    {
        TryFindLeapProvider();

        if (leapProvider == null || leapProvider.CurrentFrame == null)
        {
            isTracking = false;
            return;
        }

        Hand rightHand = leapProvider.CurrentFrame.GetHand(Chirality.Right);
        Hand leftHand = leapProvider.CurrentFrame.GetHand(Chirality.Left);

        bool hasRightHand = rightHand != null;
        bool hasLeftHand = leftHand != null;

        if (!hasRightHand && !hasLeftHand)
        {
            isTracking = false;
            return;
        }

        Vector3 currentRightHandPos = hasRightHand ? ConvertHandPosition(rightHand) : rightHandPos.Value;
        Vector3 currentLeftHandPos = hasLeftHand ? ConvertHandPosition(leftHand) : leftHandPos.Value;

        if (optionGetter == null || !optionGetter.TrackingSettings.IsHandFlipped.Value)
        {
            if (hasRightHand) { rightHandPos.Value = currentRightHandPos; }
            if (hasLeftHand) { leftHandPos.Value = currentLeftHandPos; }
        }
        else
        {
            if (hasRightHand) { leftHandPos.Value = currentRightHandPos; }
            if (hasLeftHand) { rightHandPos.Value = currentLeftHandPos; }
        }

        isTracking = true;
    }

    private Vector3 ConvertHandPosition(Hand hand)
    {
        Vector3 pos = useStabilizedPalmPosition ? hand.StabilizedPalmPosition : hand.PalmPosition;

        return pos;
    }

    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        float time = timer.Value != null ? timer.Value.Time : 0;
        BodyTrackingSettings settings = optionGetter?.TrackingSettings;

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, SpaceInputNormalizer.NormalizeUsedVector2(rightHandPos.Value, settings), time);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, SpaceInputNormalizer.NormalizeUsedVector2(leftHandPos.Value, settings), time);

        spaceInputSetter.SetCanGetSpaceInput(isTracking);
    }
}
