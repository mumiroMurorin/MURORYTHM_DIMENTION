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
    public void SetOption(OptionType optionType, int delta)
    {
        switch (optionType)
        {
            case OptionType.NoteSpeed:
                AddNoteSpeed(delta);
                break;
            case OptionType.Offset:
                AddOffset(delta);
                break;
            case OptionType.DivisionNum:
                AddGroundDivisionNum(delta);
                break;
                // 仮
            case OptionType.MusicVolume:
                AddBGMVolume(delta);
                break;
            case OptionType.JudgementSEVolume:
                AddJudgementSeVolume(delta);
                break;
            case OptionType.IsEnabledFastLate:
                SetIsEnabledFastLate(!IsEnabledFastLate.Value);
                break;

        }
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
    void AddNoteSpeed(int delta)
    {
        noteSpeed.Value = Mathf.Clamp(noteSpeed.Value + delta * 10f, MIN_NOTESPEED, MAX_NOTESPEED);
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
    void AddSEVolume(int delta)
    {
        seVolume.Value = Mathf.Clamp01(seVolume.Value + delta * 0.1f);
    }

    // 判定音関係
    ReactiveProperty<float> judgementSeVolume = new ReactiveProperty<float>(0.8f);
    public IReadOnlyReactiveProperty<float> JudgementSEVolume => judgementSeVolume;
    public int JudgementSEVolumeDisplay => (int)(judgementSeVolume.Value * 10f);
    void SetJudgementSeVolume(float value)
    {
        judgementSeVolume.Value = Mathf.Clamp01(value);
    }

    /// <summary>
    /// += delta * 0.1f
    /// </summary>
    /// <param name="delta"></param>
    void AddJudgementSeVolume(int delta)
    {
        judgementSeVolume.Value = Mathf.Clamp01(judgementSeVolume.Value + delta * 0.1f);
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
    void AddOffset(int delta)
    {
        offset.Value = Mathf.Clamp(offset.Value + delta * 10f, MIN_OFFSET, MAX_OFFSET);
    }

    #endregion


    #region DivisionNum

    ReactiveProperty<int> groundDivisionNum = new ReactiveProperty<int>(4);
    public IReadOnlyReactiveProperty<int> GroundDivisionNum => groundDivisionNum;
    public int GroundDivisionNumDisplay => groundDivisionNum.Value;
    public void AddGroundDivisionNum(int delta)
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
    }

    #endregion


    #region FastLate

    ReactiveProperty<bool> isEnabledFastLate = new ReactiveProperty<bool>(false);
    public IReadOnlyReactiveProperty<bool> IsEnabledFastLate => isEnabledFastLate;
    public void SetIsEnabledFastLate(bool isEnabled)
    {
        isEnabledFastLate.Value = isEnabled;
    }
    public string EnabledFastLateDisplay 
    {
        get { return isEnabledFastLate.Value ? "表示する" : "表示しない"; }
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

    BodyTrackingSettings TrackingSettings { get; }
}

public interface IOptionSetter
{
    void SetOption(OptionType optionType, int delta);

    void SetNoteSpeed(float speed);

    void SetIsEnabledFastLate(bool isEnabled);

    void SetAutoMode(bool isAutoMode);

    BodyTrackingSettings TrackingSettings { get; }
}
