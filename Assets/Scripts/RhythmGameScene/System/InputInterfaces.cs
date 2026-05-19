using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// スライダーからの入力を受け取る
/// </summary>
public interface ISliderInputGetter
{
    IReadOnlyReactiveProperty<bool> GetSliderInputReactiveProperty(int index);
}


/// <summary>
/// スペースの入力を受け取る
/// </summary>
public interface ISpaceInputGetter
{
    IReadOnlyReactiveCollection<TimeToPos> GetSpaceInput(SpaceTrackingTag spaceTrackingTag);

    IReadOnlyReactiveProperty<Vector3> GetSpaceInputVelocity(SpaceTrackingTag spaceTrackingTag);

    IReadOnlyReactiveProperty<bool> GetCanGetSpaceInputReactiveProperty(SpaceTrackingTag spaceTrackingTag);

    bool IsInSpaceRange(Vector2[] vertices, float radius = 0);

    bool IsInSpaceRange(Vector2[] vertices, SpaceTrackingTag spaceTrackingTag, float radius = 0);
}

/// <summary>
/// スペース入力の情報を保持する
/// </summary>
public interface ISpaceInputHandler
{
    void Initialize(IOptionGetter optionGetter);

    IReadOnlyReactiveProperty<Vector3> RightHandPos { get; }
    IReadOnlyReactiveProperty<Vector3> LeftHandPos { get; }
    bool CanGetRightHand { get; }
    bool CanGetLeftHand { get; }

    bool IsExistCamera();

    void InitializeBodyTracking();

    void StartTracking();

    void SwitchCamera();
}

public interface ISpaceInputHub : ISpaceInputHandler, ICameraInfoHolder { }

public interface ICameraInfoHolder
{
    IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo { get; }

    IReadOnlyReactiveProperty<int> CameraFps { get; }
}


public interface ISliderInputSetter
{
    public void Initialize();

    public void SetSliderInput(int index, bool isEnable);
}

public interface ISpaceInputSetter
{
    public void Initialize();

    public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos, float time);

    public void SetCanGetSpaceInput(SpaceTrackingTag tag, bool isGet);
}

