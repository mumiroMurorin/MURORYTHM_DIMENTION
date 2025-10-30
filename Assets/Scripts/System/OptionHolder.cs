using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class OptionHolder : INoteSpawnDataOptionHolder, IVolumeGetter, IOptionGetter, IOptionSetter
{
    /// <summary>
    /// オプションに値を加減算
    /// </summary>
    /// <param name="optionType"></param>
    /// <param name="delta"></param>
    public bool SetOption(OptionType optionType, int delta)
    {
        switch (optionType)
        {
            case OptionType.NoteSpeed:
                return AddNoteSpeed(delta);
            case OptionType.Offset:
                return AddOffset(delta);
            case OptionType.DivisionNum:
                return AddGroundDivisionNum(delta);
            case OptionType.JudgementSEVolume:
                return AddJudgementSeVolume(delta);
            case OptionType.IsEnabledFastLate:
                return SetIsEnabledFastLate(!IsEnabledFastLate.Value);
            case OptionType.MainInfo:
                return ChangeMainInfo();
            case OptionType.SubInfo:
                return ChangeSubInfo();
        }

        return false;
    }


    #region NoteSpeed

    const int MAX_NOTESPEED = 500;
    const int MIN_NOTESPEED = 20;

    /// <summary>
    /// ノーツが1秒間に動く(unity単位)速度
    /// </summary>
    ReactiveProperty<float> noteSpeed = new ReactiveProperty<float>(100f);
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
    void SetOffset(float value)
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
                        groundDivisionNum.Value = 2;
                        break;
                    case 2:
                        groundDivisionNum.Value = 4;
                        break;
                    case 4:
                        groundDivisionNum.Value = 8;
                        break;
                    case 8:
                        groundDivisionNum.Value = 16;
                        break;
                    case 16:
                        groundDivisionNum.Value = 1;
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
                        groundDivisionNum.Value = 16;
                        break;
                    case 2:
                        groundDivisionNum.Value = 1;
                        break;
                    case 4:
                        groundDivisionNum.Value = 2;
                        break;
                    case 8:
                        groundDivisionNum.Value = 4;
                        break;
                    case 16:
                        groundDivisionNum.Value = 8;
                        break;
                }
            }
        }

        return true;
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
    ReactiveProperty<InfoTypeMain> mainInfo = new ReactiveProperty<InfoTypeMain>(InfoTypeMain.ScoreRankSubtraction);
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
    ReactiveProperty<InfoTypeSub> subInfo = new ReactiveProperty<InfoTypeSub>(InfoTypeSub.ScoreAddition);
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


    #region 筐体設定

    BodyTrackingSettings trackingSettings = new BodyTrackingSettings();
    public BodyTrackingSettings TrackingSettings => trackingSettings;

    #endregion
}

public interface INoteSpawnDataOptionHolder
{
    IReadOnlyReactiveProperty<float> NoteSpeed { get; }

    IReadOnlyReactiveProperty<float> OffsetMs { get; }

    IReadOnlyReactiveProperty<bool> IsAutoModeRP { get; }

    bool IsAutoMode { get; }
}

public interface IVolumeGetter
{
    IReadOnlyReactiveProperty<float> SEVolume { get; }

    IReadOnlyReactiveProperty<float> JudgementSEVolume { get; }

    IReadOnlyReactiveProperty<float> BGMVolume { get; }
}

public interface IOptionGetter
{
    IReadOnlyReactiveProperty<float> NoteSpeed { get; }

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

    void SetNoteSpeed(float speed);

    bool SetIsEnabledFastLate(bool isEnabled);

    void SetAutoMode(bool isAutoMode);

    BodyTrackingSettings TrackingSettings { get; }
}
