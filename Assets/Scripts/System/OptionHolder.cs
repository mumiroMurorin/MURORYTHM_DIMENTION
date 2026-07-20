using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class OptionHolder : INoteSpawnDataOptionGetter, INoteSpawnDataOptionSetter, IVolumeGetter, IOptionGetter, IOptionSetter
{
    /// <summary>
    /// オプションに値を加減算
    /// </summary>
    /// <param name="optionType"></param>
    /// <param name="delta"></param>
    public bool SetOption(OptionType optionType, int delta)
    {
        bool isChangable = false;

        switch (optionType)
        {
            case OptionType.NoteSpeed:
                isChangable = AddNoteSpeed(delta);
                break;
            case OptionType.Offset:
                isChangable = AddOffset(delta);
                break;
            case OptionType.DivisionNum:
                isChangable = AddGroundDivisionNum(delta);
                break;
            case OptionType.JudgementSEVolume:
                isChangable = AddJudgementSeVolume(delta);
                break;
            case OptionType.IsEnabledFastLate:
                isChangable = SetIsEnabledFastLate(!IsEnabledFastLate.Value);
                break;
            case OptionType.MainInfo:
                isChangable = ChangeMainInfo();
                break;
            case OptionType.SubInfo:
                isChangable = ChangeSubInfo();
                break;
            default:
                isChangable = false;
                break;
        }

        if (isChangable && delta != 0) { OnChangeOptionValueListnener.OnNext(delta > 0 ? 1 : -1); }

        return isChangable;
    }

    /// <summary>
    /// オプションアセットの値を反映
    /// </summary>
    /// <param name="asset"></param>
    public void SetOption(OptionAsset asset)
    {
        SetNoteSpeed(asset.NoteSpeed);
        SetNoteCurveRadius(asset.NoteCurveRadius);
        SetOffsetMs(asset.Offset);
        SetSEVolume(asset.SeVolume);
        SetBGMVolume(asset.BgmVolume);
        SetDivisionNum(asset.DivisionNum);
        SetIsEnabledFastLate(asset.IsEnabledFastLate);
        SetMainInfo(asset.MainInfo);
        SetSubInfo(asset.SubInfo);
    }

    /// <summary>
    /// オプションに変更があった際発火する
    /// </summary>
    Subject<int> OnChangeOptionValueListnener = new Subject<int>();
    public IObservable<int> OnChangeOptionValue => OnChangeOptionValueListnener;

    #region NoteSpeed

    const int MAX_NOTESPEED = 500;
    const int MIN_NOTESPEED = 20;

    /// <summary>
    /// ノーツが1秒間に動く(unity単位)速度
    /// </summary>
    ReactiveProperty<float> noteSpeed = new ReactiveProperty<float>(170f);
    public IReadOnlyReactiveProperty<float> NoteSpeed => noteSpeed;
    public float NoteSpeedDisplay => noteSpeed.Value / 20f;
    public void SetNoteSpeed(float speed)
    {
        noteSpeed.Value = Mathf.Clamp(speed, MIN_NOTESPEED, MAX_NOTESPEED);
    }

    /// <summary>
    /// += delta * 20f
    /// </summary>
    /// <param name="delta"></param>
    bool AddNoteSpeed(int delta)
    {
        if (noteSpeed.Value >= MAX_NOTESPEED && delta > 0) { return false; }
        if (noteSpeed.Value <= MIN_NOTESPEED && delta < 0) { return false; }

        noteSpeed.Value = Mathf.Clamp(noteSpeed.Value + delta * 10f, MIN_NOTESPEED, MAX_NOTESPEED);

        return true;
    }

    #endregion


    #region NoteCurveRadius

    const float MAX_NOTE_CURVE_RADIUS = 10000f;
    const float MIN_NOTE_CURVE_RADIUS = 10f;

    // 【ノーツ軌道】ノーツが進行する円弧の半径
    ReactiveProperty<float> noteCurveRadius = new ReactiveProperty<float>(2000f);
    public IReadOnlyReactiveProperty<float> NoteCurveRadius => noteCurveRadius;
    public void SetNoteCurveRadius(float radius)
    {
        noteCurveRadius.Value = Mathf.Clamp(radius, MIN_NOTE_CURVE_RADIUS, MAX_NOTE_CURVE_RADIUS);
    }

    #endregion


    #region SEVolume

    // SE関係
    ReactiveProperty<float> seVolume = new ReactiveProperty<float>(0.8f);
    public IReadOnlyReactiveProperty<float> SEVolume => seVolume;
    public int SEVolumeDisplay => (int)(seVolume.Value * 10f);
    void SetSEVolume(float value)
    {
        seVolume.Value = Mathf.Clamp01(value);
    }

    /// <summary>
    /// += delta * 0.1f
    /// </summary>
    /// <param name="delta"></param>
    bool AddSEVolume(int delta)
    {
        if (seVolume.Value <= 0f && delta < 0) { return false; }
        if (seVolume.Value >= 1f && delta > 0) { return false; }

        seVolume.Value = Mathf.Clamp01(seVolume.Value + delta * 0.1f);
        return true;
    }

    // 判定音関係
    ReactiveProperty<float> judgementSeVolume = new ReactiveProperty<float>(0.8f);
    public IReadOnlyReactiveProperty<float> JudgementSEVolume => judgementSeVolume;
    public int JudgementSEVolumeDisplay => Mathf.RoundToInt(judgementSeVolume.Value * 10);
    void SetJudgementSeVolume(float value)
    {
        judgementSeVolume.Value = Mathf.Clamp01(value);
    }

    /// <summary>
    /// += delta * 0.1f
    /// </summary>
    /// <param name="delta"></param>
    bool AddJudgementSeVolume(int delta)
    {
        if (judgementSeVolume.Value <= 0f && delta < 0) { return false; }
        if (judgementSeVolume.Value >= 1f && delta > 0) { return false; }

        judgementSeVolume.Value = Mathf.Clamp01(judgementSeVolume.Value + delta * 0.1f);

        return true;
    }

    #endregion


    #region BGMVolume

    // BGM関係
    ReactiveProperty<float> bgmVolume = new ReactiveProperty<float>(0.8f);
    public IReadOnlyReactiveProperty<float> BGMVolume => bgmVolume;
    public int BGMVolumeDisplay => (int)(bgmVolume.Value * 10f);
    void SetBGMVolume(float value)
    {
        bgmVolume.Value = Mathf.Clamp01(value);
    }

    /// <summary>
    /// += delta * 0.1f
    /// </summary>
    /// <param name="delta"></param>
    void AddBGMVolume(int delta)
    {
        bgmVolume.Value = Mathf.Clamp01(bgmVolume.Value + delta * 0.1f);
    }

    #endregion


    #region Offset

    const float MAX_OFFSET = 1000f;
    const float MIN_OFFSET = -1000f;

    // オフセット関係
    ReactiveProperty<float> offset = new ReactiveProperty<float>(0);
    public IReadOnlyReactiveProperty<float> OffsetMs => offset;
    public int OffsetDisplay => (int)offset.Value;
    public void SetOffsetMs(float value)
    {
        offset.Value = Mathf.Clamp(value, MIN_OFFSET, MAX_OFFSET);
    }

    /// <summary>
    /// += delta * 10f
    /// </summary>
    /// <param name="delta"></param>
    bool AddOffset(int delta)
    {
        if (offset.Value <= MIN_OFFSET && delta < 0) { return false; }
        if (offset.Value >= MAX_OFFSET && delta > 0) { return false; }

        offset.Value = Mathf.Clamp(offset.Value + delta * 10f, MIN_OFFSET, MAX_OFFSET);
        return true;
    }

    #endregion


    #region DivisionNum

    ReactiveProperty<int> groundDivisionNum = new ReactiveProperty<int>(4);
    public IReadOnlyReactiveProperty<int> GroundDivisionNum => groundDivisionNum;
    public int GroundDivisionNumDisplay => groundDivisionNum.Value;
    public bool AddGroundDivisionNum(int delta)
    {
        if(delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                switch (groundDivisionNum.Value)
                {
                    case 1:
                        SetDivisionNum(2);
                        break;
                    case 2:
                        SetDivisionNum(4);
                        break;
                    case 4:
                        SetDivisionNum(8);
                        break;
                    case 8:
                        SetDivisionNum(16);
                        break;
                    case 16:
                        SetDivisionNum(1);
                        break;
                }
            }
        }
        else
        {
            for (int i = 0; i < Mathf.Abs(delta); i++)
            {
                switch (groundDivisionNum.Value)
                {
                    case 1:
                        SetDivisionNum(16);
                        break;
                    case 2:
                        SetDivisionNum(1);
                        break;
                    case 4:
                        SetDivisionNum(2);
                        break;
                    case 8:
                        SetDivisionNum(4);
                        break;
                    case 16:
                        SetDivisionNum(8);
                        break;
                }
            }
        }

        return true;
    }
    void SetDivisionNum(int num)
    {
        if (num > 16) { return; }
        if (num < 1) { return; }
        if (num % 2 != 0) { return; }

        groundDivisionNum.Value = num;
    }

    #endregion


    #region FastLate

    ReactiveProperty<bool> isEnabledFastLate = new ReactiveProperty<bool>(false);
    public IReadOnlyReactiveProperty<bool> IsEnabledFastLate => isEnabledFastLate;
    public bool SetIsEnabledFastLate(bool isEnabled)
    {
        isEnabledFastLate.Value = isEnabled;
        return true;
    }
    public string EnabledFastLateDisplay 
    {
        get { return isEnabledFastLate.Value ? "表示する" : "表示しない"; }
    }

    #endregion


    #region Info

    // メイン情報
    ReactiveProperty<InfoTypeMain> mainInfo = new ReactiveProperty<InfoTypeMain>(InfoTypeMain.ComboAP);
    public IReadOnlyReactiveProperty<InfoTypeMain> MainInfo => mainInfo;
    public bool ChangeMainInfo()
    {
        switch (mainInfo.Value)
        {
            case InfoTypeMain.None:
                mainInfo.Value = InfoTypeMain.Combo;
                break;
            case InfoTypeMain.Combo:
                mainInfo.Value = InfoTypeMain.ComboFC;
                break;
            case InfoTypeMain.ComboFC:
                mainInfo.Value = InfoTypeMain.ComboAP;
                break;
            case InfoTypeMain.ComboAP:
                mainInfo.Value = InfoTypeMain.ScoreRank;
                break;
            case InfoTypeMain.ScoreRank:
                mainInfo.Value = InfoTypeMain.ScoreRankSubtraction;
                break;
            case InfoTypeMain.ScoreRankSubtraction:
                mainInfo.Value = InfoTypeMain.None;
                break;
        }

        return true;
    }
    public void SetMainInfo(InfoTypeMain type)
    {
        mainInfo.Value = type;
    }
    public string MainInfoDisplay { 
        get 
        {
            switch (mainInfo.Value)
            {
                case InfoTypeMain.None:
                    return "表示しない";
                case InfoTypeMain.Combo:
                    return "コンボ";
                case InfoTypeMain.ComboFC:
                    return "コンボ\n<size=50%>(FC表示あり)";
                case InfoTypeMain.ComboAP:
                    return "コンボ\n<size=50%>(AP表示あり)";
                case InfoTypeMain.ScoreRank:
                    return "スコアランク\n<size=50%>(加算方式)";
                case InfoTypeMain.ScoreRankSubtraction:
                    return "スコアランク\n<size=50%>(減算方式)";
            }

            return $"{mainInfo.Value}";
        } 
    }

    // サブ情報
    ReactiveProperty<InfoTypeSub> subInfo = new ReactiveProperty<InfoTypeSub>(InfoTypeSub.Breakdown);
    public IReadOnlyReactiveProperty<InfoTypeSub> SubInfo => subInfo;
    public bool ChangeSubInfo()
    {
        switch (subInfo.Value)
        {
            case InfoTypeSub.None:
                subInfo.Value = InfoTypeSub.ScoreAddition;
                break;
            case InfoTypeSub.ScoreAddition:
                subInfo.Value = InfoTypeSub.ScoreSubtraction;
                break;
            case InfoTypeSub.ScoreSubtraction:
                subInfo.Value = InfoTypeSub.ComboRank;
                break;
            case InfoTypeSub.ComboRank:
                subInfo.Value = InfoTypeSub.Breakdown;
                break;
            case InfoTypeSub.Breakdown:
                subInfo.Value = InfoTypeSub.None;
                break;
        }

        return true;
    }
    public void SetSubInfo(InfoTypeSub type)
    {
        subInfo.Value = type;
    }

    public string SubInfoDisplay
    {
        get
        {
            switch (subInfo.Value)
            {
                case InfoTypeSub.None:
                    return "表示しない";
                case InfoTypeSub.ScoreAddition:
                    return "スコア\n<size=50%>(加算方式)";
                case InfoTypeSub.ScoreSubtraction:
                    return "スコア\n<size=50%>(減算方式)";
                case InfoTypeSub.ComboRank:
                    return "AP/FC";
                case InfoTypeSub.Breakdown:
                    return "判定内訳";
            }

            return $"{subInfo.Value}";
        }
    }


    #endregion


    #region Cheat

    ReactiveProperty<bool> isAutoMode = new ReactiveProperty<bool>();
    public bool IsAutoMode { get { return isAutoMode.Value; } private set { isAutoMode.Value = value; } }
    public IReadOnlyReactiveProperty<bool> IsAutoModeRP => isAutoMode;
    public void SetAutoMode(bool isAutoMode)
    {
        IsAutoMode = isAutoMode;
    }
    #endregion


    #region TrackingMode

    ReactiveProperty<TrackingMode> currentTrackingMode = new ReactiveProperty<TrackingMode>(TrackingMode.BodyTracking);
    public IReadOnlyReactiveProperty<TrackingMode> CurrentTrackingMode => currentTrackingMode;
    public void SetCurrentTrackingMode(TrackingMode trackingMode)
    {
        if (currentTrackingMode.Value == trackingMode) { return; }

        currentTrackingMode.Value = trackingMode;
    }

    #endregion


    #region TutorialGuideCharacter

    ReactiveProperty<TutorialGuideCharacterType> currentTutorialGuideCharacterType = new ReactiveProperty<TutorialGuideCharacterType>(TutorialGuideCharacterType.Destruction);
    public IReadOnlyReactiveProperty<TutorialGuideCharacterType> CurrentTutorialGuideCharacterType => currentTutorialGuideCharacterType;
    public void SetCurrentTutorialGuideCharacterType(TutorialGuideCharacterType tutorialGuideCharacterType)
    {
        if (currentTutorialGuideCharacterType.Value == tutorialGuideCharacterType) { return; }

        currentTutorialGuideCharacterType.Value = tutorialGuideCharacterType;
    }

    public void ResetTutorialGuideCharacterType()
    {
        SetCurrentTutorialGuideCharacterType(TutorialGuideCharacterType.Shikiboo);
    }

    #endregion


    #region 筐体設定

    BodyTrackingSettings trackingSettings = new BodyTrackingSettings();
    public BodyTrackingSettings TrackingSettings => trackingSettings;

    #endregion
}

public interface INoteSpawnDataOptionGetter
{
    IReadOnlyReactiveProperty<float> NoteSpeed { get; }

    IReadOnlyReactiveProperty<float> NoteCurveRadius { get; }

    IReadOnlyReactiveProperty<float> OffsetMs { get; }

    IReadOnlyReactiveProperty<bool> IsAutoModeRP { get; }

    bool IsAutoMode { get; }
}

public interface INoteSpawnDataOptionSetter
{
    void SetNoteSpeed(float speed);

    void SetNoteCurveRadius(float radius);

    void SetOffsetMs(float offset);

    void SetAutoMode(bool isAutoMode);
}

public interface IVolumeGetter
{
    IReadOnlyReactiveProperty<float> SEVolume { get; }

    IReadOnlyReactiveProperty<float> JudgementSEVolume { get; }

    IReadOnlyReactiveProperty<float> BGMVolume { get; }
}

public interface IOptionGetter
{
    IObservable<int> OnChangeOptionValue { get; }

    IReadOnlyReactiveProperty<float> NoteSpeed { get; }

    IReadOnlyReactiveProperty<float> NoteCurveRadius { get; }

    float NoteSpeedDisplay { get; }

    IReadOnlyReactiveProperty<float> OffsetMs { get; }

    int OffsetDisplay { get; }

    IReadOnlyReactiveProperty<float> SEVolume { get; }

    int SEVolumeDisplay { get; }

    IReadOnlyReactiveProperty<float> BGMVolume { get; }

    int BGMVolumeDisplay { get; }

    IReadOnlyReactiveProperty<float> JudgementSEVolume { get; }

    int JudgementSEVolumeDisplay { get; }

    IReadOnlyReactiveProperty<bool> IsEnabledFastLate { get; }
    string EnabledFastLateDisplay { get; }

    IReadOnlyReactiveProperty<int> GroundDivisionNum { get; }
    int GroundDivisionNumDisplay { get; }

    IReadOnlyReactiveProperty<InfoTypeMain> MainInfo { get; }
    string MainInfoDisplay { get; }

    IReadOnlyReactiveProperty<InfoTypeSub> SubInfo { get; }
    string SubInfoDisplay { get; }

    IReadOnlyReactiveProperty<TrackingMode> CurrentTrackingMode { get; }

    IReadOnlyReactiveProperty<TutorialGuideCharacterType> CurrentTutorialGuideCharacterType { get; }

    BodyTrackingSettings TrackingSettings { get; }
}

public interface IOptionSetter
{
    /// <summary>
    /// オプションの値を次の値に
    /// </summary>
    /// <param name="optionType"></param>
    /// <param name="delta"></param>
    /// <returns>値の変更に成功？</returns>
    bool SetOption(OptionType optionType, int delta);

    void SetOption(OptionAsset asset);

    bool SetIsEnabledFastLate(bool isEnabled);

    void SetCurrentTrackingMode(TrackingMode trackingMode);

    void SetCurrentTutorialGuideCharacterType(TutorialGuideCharacterType tutorialGuideCharacterType);

    void ResetTutorialGuideCharacterType();

    BodyTrackingSettings TrackingSettings { get; }
}


