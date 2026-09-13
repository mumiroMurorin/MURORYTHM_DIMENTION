using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Note Judgement Settings/Catalog", fileName = "NoteJudgementSettingsCatalog")]
public class NoteJudgementSettingsCatalog : ScriptableObject
{
    [Header("Difficulty Profiles")]
    [SerializeField] NoteJudgementSettingsProfile easy = new NoteJudgementSettingsProfile();
    [SerializeField] NoteJudgementSettingsProfile normal = new NoteJudgementSettingsProfile();
    [SerializeField] NoteJudgementSettingsProfile hard = new NoteJudgementSettingsProfile();
    [SerializeField] NoteJudgementSettingsProfile master = new NoteJudgementSettingsProfile();

    public NoteJudgementConfig GetJudgementSettings(NoteType noteType, Difficulty difficulty)
    {
        return GetProfile(difficulty)?.GetJudgementSettings(noteType);
    }

    public T GetJudgementSettings<T>(NoteType noteType, Difficulty difficulty)
        where T : NoteJudgementConfig
    {
        T settings = GetJudgementSettings(noteType, difficulty) as T;
        if (settings == null)
        {
            Debug.LogWarning($"[System] Judgement settings type mismatch: {noteType} / {difficulty} / {typeof(T).Name}", this);
        }

        return settings;
    }

    NoteJudgementSettingsProfile GetProfile(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return easy;
            case Difficulty.Normal:
                return normal;
            case Difficulty.Hard:
                return hard;
            case Difficulty.Master:
                return master;
            default:
                Debug.LogWarning($"[System] Unknown difficulty '{difficulty}'. Normal judgement settings will be used.", this);
                return normal;
        }
    }
}

[Serializable]
public class NoteJudgementSettingsProfile
{
    [Header("Touch")]
    [SerializeField] NoteJudgementConfig touch = new NoteJudgementConfig();
    [SerializeField] NoteJudgementConfig divineTouch = new NoteJudgementConfig();

    [Header("Hold")]
    [SerializeField] NoteJudgementConfig holdStart = new NoteJudgementConfig();
    [SerializeField] NoteJudgementConfig divineHoldStart = new NoteJudgementConfig();
    [SerializeField] NoteJudgementConfig holdRelay = new NoteJudgementConfig();
    [SerializeField] NoteJudgementConfig holdRelayHidden = new NoteJudgementConfig();
    [SerializeField] NoteJudgementConfig holdEnd = new NoteJudgementConfig();

    [Header("Dynamic")]
    [SerializeField] DynamicNoteJudgementConfig dynamicGroundUpward = new DynamicNoteJudgementConfig();
    [SerializeField] DynamicNoteJudgementConfig dynamicGroundDownward = new DynamicNoteJudgementConfig();
    [SerializeField] DynamicNoteJudgementConfig dynamicGroundLeftward = new DynamicNoteJudgementConfig();
    [SerializeField] DynamicNoteJudgementConfig dynamicGroundRightward = new DynamicNoteJudgementConfig();

    [Header("Space")]
    [SerializeField] SpaceBreakJudgementConfig spaceBreak = new SpaceBreakJudgementConfig();
    [SerializeField] SpaceHoldJudgementConfig spaceHoldRelay = new SpaceHoldJudgementConfig();
    [SerializeField] SpaceHoldJudgementConfig spaceHoldRelayHidden = new SpaceHoldJudgementConfig();

    public NoteJudgementConfig GetJudgementSettings(NoteType noteType)
    {
        switch (noteType)
        {
            case NoteType.Touch:
                return touch;
            case NoteType.DivineTouch:
                return divineTouch;
            case NoteType.HoldStart:
                return holdStart;
            case NoteType.DivineHoldStart:
                return divineHoldStart;
            case NoteType.HoldRelay:
                return holdRelay;
            case NoteType.HoldRelayHidden:
                return holdRelayHidden;
            case NoteType.HoldEnd:
                return holdEnd;
            case NoteType.DynamicGroundUpward:
                return dynamicGroundUpward;
            case NoteType.DynamicGroundDownward:
                return dynamicGroundDownward;
            case NoteType.DynamicGroundLeftward:
                return dynamicGroundLeftward;
            case NoteType.DynamicGroundRightward:
                return dynamicGroundRightward;
            case NoteType.SpaceBreak:
                return spaceBreak;
            case NoteType.SpaceHoldRelay:
                return spaceHoldRelay;
            case NoteType.SpaceHoldRelayHidden:
                return spaceHoldRelayHidden;
            default:
                return null;
        }
    }
}

[Serializable]
public class NoteJudgementConfig
{
    [SerializeField] JudgementWindow judgementWindow = new JudgementWindow();

    public JudgementWindow CreateJudgementWindowOrDefault(JudgementWindow fallback)
    {
        return judgementWindow != null ? judgementWindow.Copy() : fallback;
    }

    public JudgementWindow CreateJudgementWindowIfMissing(JudgementWindow current)
    {
        return current ?? CreateJudgementWindowOrDefault(null);
    }
}

[Serializable]
public class DynamicNoteJudgementConfig : NoteJudgementConfig
{
    [SerializeField] float judgeMagnitude = 1f;

    public float JudgeMagnitude => judgeMagnitude;
}

[Serializable]
public class SpaceBreakJudgementConfig : NoteJudgementConfig
{
    [SerializeField] float judgementMarginRadius = 0.25f;
    [SerializeField] float judgeMagnitude = 1f;

    public float JudgementMarginRadius => judgementMarginRadius;
    public float JudgeMagnitude => judgeMagnitude;
}

[Serializable]
public class SpaceHoldJudgementConfig : NoteJudgementConfig
{
    [SerializeField] float judgementMarginRadius = 0.25f;

    public float JudgementMarginRadius => judgementMarginRadius;
}
