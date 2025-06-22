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
    [SerializeField] Vector3 controllerCenter;
    [SerializeField] Vector3 controllerSize;

    [Space(30)]
    [SerializeField] TMPro.TextMeshProUGUI velocityTextRight;
    [SerializeField] TMPro.TextMeshProUGUI velocityTextLeft;

    Vector3 right_hand_pos = Vector3.zero;
    Vector3 left_hand_pos = Vector3.zero;

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

    private void Start()
    {
        Bind();
    }

    void Update()
    {
        ReadData();
        SendData();
    }

    private void Bind()
    {
        spaceInputGetter?.GetSpaceInputVelocity(SpaceTrackingTag.RightHand)
            .Subscribe(value =>
            {
                if(velocityTextRight == null) { return; }
                velocityTextRight.text = value.y.ToString("F2");
            })
            .AddTo(this.gameObject);

        spaceInputGetter?.GetSpaceInputVelocity(SpaceTrackingTag.LeftHand)
            .Subscribe(value =>
            {
                if (velocityTextLeft == null) { return; }
                velocityTextLeft.text = value.y.ToString("F2");
            })
            .AddTo(this.gameObject);
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

        right_hand_pos = new Vector3(
            bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].X,
            bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Y,
            bodyTracking.LandmarkList.Landmark[RIGHT_HAND_INDEX].Z
            );

        left_hand_pos = new Vector3(
            bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].X,
            bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Y,
            bodyTracking.LandmarkList.Landmark[LEFT_HAND_INDEX].Z
            );

        isTracking = true;
        // Debug.Log($"【MediaPipe】Right: {right_hand_pos} left: {left_hand_pos}");
    }

    /// <summary>
    /// -1～1に正規化
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Vector3 Normalize(Vector3 pos)
    {
        Vector3 normalized = new Vector3(
            (pos.x - controllerCenter.x) / (controllerSize.x / 2f),
            (pos.y - controllerCenter.y) / (controllerSize.y / 2f),
            (pos.z - controllerCenter.z) / (controllerSize.z / 2f)
        );

        // -1～1の範囲に収めて返す
        Vector2 xy = new Vector2(normalized.x, normalized.y);
        xy = Vector2.ClampMagnitude(xy, 1f);

        return new Vector3(
            xy.x,
            xy.y,
            Mathf.Clamp(normalized.z, -1f, 1f)
        );
    }

    /// <summary>
    /// データを保持クラスに送信
    /// </summary>
    private void SendData()
    {
        if (spaceInputSetter == null) { return; }

        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.RightHand, Normalize(right_hand_pos), timer.Value.Time);
        spaceInputSetter.SetSpaceInput(SpaceTrackingTag.LeftHand, Normalize(left_hand_pos), timer.Value.Time);
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
public class BodyTrackingSettings
{
    // トラッキングの左右反転
    ReactiveProperty<bool> isHorizontallyFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsHorizontallyFlipped => isHorizontallyFlipped;
    public void SetIsHorizontallyFlipped(bool isFlipped)
    {
        isHorizontallyFlipped.Value = isFlipped;
    }

    // カメラ解像度(横)
    ReactiveProperty<int> cameraWidth = new ReactiveProperty<int>();
    public IReadOnlyReactiveProperty<int> CameraWidth => cameraWidth;
    public void SetCameraWidth(int width)
    {
        cameraWidth.Value = width;
    }

    // カメラ解像度(縦)
    ReactiveProperty<int> cameraHeight = new ReactiveProperty<int>();
    public IReadOnlyReactiveProperty<int> CameraHeight => cameraHeight;
    public void SetCameraHeight(int height)
    {
        cameraHeight.Value = height;
    }

    // 筐体真ん中(7番と8番の間)
    ReactiveProperty<Vector3> controllerCenter = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerCenter => controllerCenter;
    public void SetControllerCenter(Vector3 pos)
    {
        controllerCenter.Value = pos;
    }

    // 筐体サイズ(直径)
    ReactiveProperty<Vector3> controllerSize = new ReactiveProperty<Vector3>(Vector3.one);
    public IReadOnlyReactiveProperty<Vector3> ControllerSize => controllerSize;
    public void SetControllerSize(Vector3 size)
    {
        controllerSize.Value = size;
    }
}