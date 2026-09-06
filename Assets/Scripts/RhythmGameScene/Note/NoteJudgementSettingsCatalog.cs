using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Note Judgement Settings/Catalog", fileName = "NoteJudgementSettingsCatalog")]
public class NoteJudgementSettingsCatalog : ScriptableObject
{
    [Header("Touch")]
    [SerializeField, Expandable] TouchJudgementSettings touch;
    [SerializeField, Expandable] DefaultNoteJudgementSettings divineTouch;

    [Header("Hold")]
    [SerializeField, Expandable] DefaultNoteJudgementSettings holdStart;
    [SerializeField, Expandable] DefaultNoteJudgementSettings divineHoldStart;
    [SerializeField, Expandable] DefaultNoteJudgementSettings holdRelay;
    [SerializeField, Expandable] DefaultNoteJudgementSettings holdRelayHidden;
    [SerializeField, Expandable] DefaultNoteJudgementSettings holdEnd;

    [Header("Dynamic")]
    [SerializeField, Expandable] DynamicNoteJudgementSettings dynamicGroundUpward;
    [SerializeField, Expandable] DynamicNoteJudgementSettings dynamicGroundDownward;
    [SerializeField, Expandable] DynamicNoteJudgementSettings dynamicGroundLeftward;
    [SerializeField, Expandable] DynamicNoteJudgementSettings dynamicGroundRightward;

    [Header("Space")]
    [SerializeField, Expandable] SpaceBreakJudgementSettings spaceBreak;
    [SerializeField, Expandable] SpaceHoldJudgementSettings spaceHoldRelay;
    [SerializeField, Expandable] SpaceHoldJudgementSettings spaceHoldRelayHidden;

    public TouchJudgementSettings Touch => touch;
    public DefaultNoteJudgementSettings DivineTouch => divineTouch;
    public DefaultNoteJudgementSettings HoldStart => holdStart;
    public DefaultNoteJudgementSettings DivineHoldStart => divineHoldStart;
    public DefaultNoteJudgementSettings HoldRelay => holdRelay;
    public DefaultNoteJudgementSettings HoldRelayHidden => holdRelayHidden;
    public DefaultNoteJudgementSettings HoldEnd => holdEnd;
    public DynamicNoteJudgementSettings DynamicGroundUpward => dynamicGroundUpward;
    public DynamicNoteJudgementSettings DynamicGroundDownward => dynamicGroundDownward;
    public DynamicNoteJudgementSettings DynamicGroundLeftward => dynamicGroundLeftward;
    public DynamicNoteJudgementSettings DynamicGroundRightward => dynamicGroundRightward;
    public SpaceBreakJudgementSettings SpaceBreak => spaceBreak;
    public SpaceHoldJudgementSettings SpaceHoldRelay => spaceHoldRelay;
    public SpaceHoldJudgementSettings SpaceHoldRelayHidden => spaceHoldRelayHidden;

    public NoteJudgementSettings GetJudgementSettings(NoteType noteType)
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
