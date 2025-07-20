using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// トラッキングに関する設定項目まとめクラス
/// </summary>
[System.Serializable]
public class BodyTrackingSettings
{
    // トラッキングの左右反転
    ReactiveProperty<bool> isHorizontallyFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsHorizontallyFlipped => isHorizontallyFlipped;
    public void SetIsHorizontallyFlipped(bool isFlipped)
    {
        isHorizontallyFlipped.Value = isFlipped;
    }

    // 手の左右識別反転
    ReactiveProperty<bool> isHandFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsHandFlipped => isHandFlipped;
    public void SetIsHandFlipped(bool isFlipped)
    {
        isHandFlipped.Value = isFlipped;
    }

    // トラッキングの上下反転
    ReactiveProperty<bool> isVerticallyFlipped = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> IsVerticallyFlipped => isVerticallyFlipped;
    public void SetIsVerticallyFlipped(bool isFlipped)
    {
        isVerticallyFlipped.Value = isFlipped;
    }

    // 筐体真ん中(7番と8番の間)
    ReactiveProperty<Vector3> controllerLowerCenter = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerLowerCenter => controllerLowerCenter;
    public void SetControllerLowerCenter(Vector3 pos)
    {
        controllerLowerCenter.Value = pos;
    }

    // 筐体左端(0番)
    ReactiveProperty<Vector3> controllerLeftEdge = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerLeftEdge => controllerLeftEdge;
    public void SetControllerLeftEdge(Vector3 pos)
    {
        controllerLeftEdge.Value = pos;
    }

    // 筐体右端(15番)
    ReactiveProperty<Vector3> controllerRightEdge = new ReactiveProperty<Vector3>(Vector3.zero);
    public IReadOnlyReactiveProperty<Vector3> ControllerRightEdge => controllerRightEdge;
    public void SetControllerRightEdge(Vector3 pos)
    {
        controllerRightEdge.Value = pos;
    }

    /// <summary>
    /// 保存用クラスから読み込み
    /// </summary>
    /// <param name="dto"></param>
    public void SetFromDTO(BodyTrackingSettingsDTO dto)
    {
        if(dto == null) { return; }

        SetIsHorizontallyFlipped(dto.IsHorizontallyFlipped);
        SetIsVerticallyFlipped(dto.IsVerticallyFlipped);
        SetIsHandFlipped(dto.IsHandFlipped);
        SetControllerLowerCenter(dto.ControllerLowerCenter);
        SetControllerLeftEdge(dto.ControllerLeftEdge);
        SetControllerRightEdge(dto.ControllerRightEdge);
    }
}

/// <summary>
/// トラッキングに関する設定項目まとめクラス
/// </summary>
[System.Serializable]
public class BodyTrackingSettingsDTO
{
    public BodyTrackingSettingsDTO(BodyTrackingSettings settings)
    {
        this.isHorizontallyFlipped = settings.IsHorizontallyFlipped.Value;
        this.isHandFlipped = settings.IsHandFlipped.Value;
        this.isVerticallyFlipped = settings.IsVerticallyFlipped.Value;
        this.controllerLowerCenter = new SimpleVector3(settings.ControllerLowerCenter.Value);
        this.controllerLeftEdge = new SimpleVector3(settings.ControllerLeftEdge.Value);
        this.controllerRightEdge = new SimpleVector3(settings.ControllerRightEdge.Value);
    }

    // トラッキングの左右反転
    [SerializeField] bool isHorizontallyFlipped;
    public bool IsHorizontallyFlipped { get { return isHorizontallyFlipped; } }

    // 手の左右識別反転
    [SerializeField] bool isHandFlipped;
    public bool IsHandFlipped { get { return isHandFlipped; } }

    // トラッキングの左右識別反転
    [SerializeField] bool isVerticallyFlipped;
    public bool IsVerticallyFlipped { get { return isVerticallyFlipped; } }

    // コントローラ真ん中(7番と8番の間)
    [SerializeField] SimpleVector3 controllerLowerCenter;
    public Vector3 ControllerLowerCenter { get { return controllerLowerCenter.ToVector3(); } }

    // コントローラ左端(0番)
    [SerializeField] SimpleVector3 controllerLeftEdge;
    public Vector3 ControllerLeftEdge { get { return controllerLeftEdge.ToVector3(); } }

    // コントローラ右端(15番)
    [SerializeField] SimpleVector3 controllerRightEdge;
    public Vector3 ControllerRightEdge { get { return controllerRightEdge.ToVector3(); } }
}