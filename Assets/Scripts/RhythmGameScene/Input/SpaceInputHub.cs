using UnityEngine;
using UniRx;
using VContainer;

public class SpaceInputHub : MonoBehaviour, ISpaceInputHub
{
    [Header("Tracking Targets")]
    [SerializeField] private SerializeInterface<ISpaceInputHandler> bodyTrackingHandler;
    [SerializeField] private SerializeInterface<ISpaceInputHandler> handTrackingHandler;
    [SerializeField] private SerializeInterface<ISpaceInputHandler> graphRunnerTrackingHandler;
    [SerializeField] private SerializeInterface<ISpaceInputHandler> leapMotionHandler;
    [SerializeField] private SerializeInterface<ISpaceInputHandler> kinectHandler;
    [SerializeField] private SerializeInterface<ITimeGetter> timer;

    private readonly ReactiveProperty<WebCamTexture> emptyWebCamInfo = new ReactiveProperty<WebCamTexture>(null);
    private readonly ReactiveProperty<int> emptyCameraFps = new ReactiveProperty<int>(0);

    private ISpaceInputSetter spaceInputSetter;
    private IOptionGetter optionGetter;
    private bool isHandlersInitialized;

    public IReadOnlyReactiveProperty<Vector3> RightHandPos => GetActiveHandler()?.RightHandPos ?? emptyRightHandPos;
    public IReadOnlyReactiveProperty<Vector3> LeftHandPos => GetActiveHandler()?.LeftHandPos ?? emptyLeftHandPos;
    public bool CanGetRightHand => GetActiveHandler()?.CanGetRightHand == true;
    public bool CanGetLeftHand => GetActiveHandler()?.CanGetLeftHand == true;
    public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => GetActiveCameraInfoHolder()?.WebCamInfo ?? emptyWebCamInfo;
    public IReadOnlyReactiveProperty<int> CameraFps => GetActiveCameraInfoHolder()?.CameraFps ?? emptyCameraFps;

    private readonly ReactiveProperty<Vector3> emptyRightHandPos = new ReactiveProperty<Vector3>(Vector3.zero);
    private readonly ReactiveProperty<Vector3> emptyLeftHandPos = new ReactiveProperty<Vector3>(Vector3.zero);

    [Inject]
    public void Construct(ISpaceInputSetter inputSetter, IOptionGetter optionGetter)
    {
        spaceInputSetter = inputSetter;
        this.optionGetter = optionGetter;

        InitializeHandlers();
        BindTrackingMode();
    }

    private void Start()
    {
        
    }

    public void Initialize(IOptionGetter optionGetter)
    {
        // Hub itself does not need a secondary initialization path.
    }

    private void InitializeHandlers()
    {
        if (isHandlersInitialized)
        {
            return;
        }

        bodyTrackingHandler?.Value?.Initialize(optionGetter);
        graphRunnerTrackingHandler?.Value?.Initialize(optionGetter);
        leapMotionHandler?.Value?.Initialize(optionGetter);
        kinectHandler?.Value?.Initialize(optionGetter);

        isHandlersInitialized = true;
    }

    private void BindTrackingMode()
    {
        optionGetter?.CurrentTrackingMode
            .Subscribe(ApplyTrackingMode)
            .AddTo(this.gameObject);
    }

    private void ApplyTrackingMode(TrackingMode trackingMode)
    {
        if (leapMotionHandler?.Value is SpaceInputHandlerForLeapMotion leapMotion)
        {
            leapMotion.SetProviderActive(trackingMode == TrackingMode.LeapMotion);
        }
    }

    private void Update()
    {
        PushTrackingDataToModel();
    }

    public bool IsExistCamera()
    {
        return GetActiveHandler()?.IsExistCamera() == true;
    }

    public void InitializeBodyTracking()
    {
        GetActiveHandler()?.InitializeBodyTracking();
    }

    public void StartTracking()
    {
        GetActiveHandler()?.StartTracking();
    }

    public void SwitchCamera()
    {
        GetActiveHandler()?.SwitchCamera();
    }

    private void PushTrackingDataToModel()
    {
        var handler = GetActiveHandler();
        if (handler == null || spaceInputSetter == null)
        {
            return;
        }

        var currentTime = timer?.Value != null ? timer.Value.Time : 0f;
        var trackingSettings = optionGetter?.TrackingSettings;
        var rightHandPos = SpaceInputNormalizer.NormalizeUsedVector2(handler.RightHandPos.Value, trackingSettings);
        var leftHandPos = SpaceInputNormalizer.NormalizeUsedVector2(handler.LeftHandPos.Value, trackingSettings);

        spaceInputSetter.SetCanGetSpaceInput(SpaceTrackingTag.RightHand, handler.CanGetRightHand);
        spaceInputSetter.SetCanGetSpaceInput(SpaceTrackingTag.LeftHand, handler.CanGetLeftHand);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, rightHandPos, currentTime);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, leftHandPos, currentTime);
    }

    private ISpaceInputHandler GetActiveHandler()
    {
        var trackingMode = optionGetter?.CurrentTrackingMode.Value ?? TrackingMode.BodyTracking;

        return trackingMode switch
        {
            TrackingMode.BodyTracking => bodyTrackingHandler?.Value,
            TrackingMode.HandTracking => graphRunnerTrackingHandler?.Value,
            TrackingMode.GraphRunnerHandTracking => graphRunnerTrackingHandler?.Value,
            TrackingMode.LeapMotion => leapMotionHandler?.Value,
            TrackingMode.Kinect => kinectHandler?.Value,
            _ => bodyTrackingHandler?.Value,
        };
    }

    private ICameraInfoHolder GetActiveCameraInfoHolder()
    {
        return GetActiveHandler() as ICameraInfoHolder;
    }
}
