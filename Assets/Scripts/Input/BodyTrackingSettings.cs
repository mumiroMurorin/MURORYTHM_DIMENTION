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

        SetIsHorizontallyFlipped(dto.isHorizontallyFlipped);
        SetIsVerticallyFlipped(dto.isVerticallyFlipped);
        SetIsHandFlipped(dto.isHandFlipped);
        SetControllerLowerCenter(dto.controllerLowerCenter.ToVector3());
        SetControllerLeftEdge(dto.controllerLeftEdge.ToVector3());
        SetControllerRightEdge(dto.controllerRightEdge.ToVector3());
    }
}

/// <summary>
/// トラッキングに関する設定項目まとめクラス
/// </summary>
[System.Serializable]
public class BodyTrackingSettingsDTO
{
    public BodyTrackingSettingsDTO() { }

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
    public bool isHorizontallyFlipped;

    // 手の左右識別反転
    public bool isHandFlipped;

    // トラッキングの左右識別反転
    public bool isVerticallyFlipped;

    // コントローラ真ん中(7番と8番の間)
    public SimpleVector3 controllerLowerCenter;

    // コントローラ左端(0番)
    public SimpleVector3 controllerLeftEdge;

    // コントローラ右端(15番)
    public SimpleVector3 controllerRightEdge;
}