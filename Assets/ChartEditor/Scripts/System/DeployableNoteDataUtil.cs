using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ChartEditor
{
    public static class DeployableNoteDataUtil
    {
        public static IEnumerable<IDeployableNoteData> OrderedByAddress(this IEnumerable<IDeployableNoteData> source)
        {
            return source
                .OrderBy(v => v.Address.BarIndex)
                .ThenBy(v => v.Address.SubDivisionIndex)
                .ThenBy(v => v.Address.Range[0]);
        }

        public static DeploymentNoteType ToDeploymentNoteType(this NoteType noteType)
        {
            switch (noteType)
            {
                case NoteType.Touch:
                    return DeploymentNoteType.Touch;
                case NoteType.DivineTouch:
                    return DeploymentNoteType.DivineTouch;
                case NoteType.HoldStart:
                    return DeploymentNoteType.HoldStart;
                case NoteType.HoldRelay:
                    return DeploymentNoteType.HoldRelay;
                case NoteType.HoldRelayHidden:
                    return DeploymentNoteType.HoldMeshRelay;
                case NoteType.HoldEnd:
                    return DeploymentNoteType.HoldEnd;
                case NoteType.HoldEndUnjudge:
                    return DeploymentNoteType.HoldEndUnjudge;
                case NoteType.SpaceHoldRelay:
                    return DeploymentNoteType.SpaceHoldRelay;
                case NoteType.SpaceHoldRelayHidden:
                    return DeploymentNoteType.SpaceHoldMeshRelay;
                case NoteType.SpaceBreak:
                    return DeploymentNoteType.SpaceBreak;
                case NoteType.DynamicGroundUpward:
                    return DeploymentNoteType.DynamicGroundUpward;
                case NoteType.DynamicGroundDownward:
                    return DeploymentNoteType.DynamicGroundDownward;
                case NoteType.DynamicGroundRightward:
                    return DeploymentNoteType.DynamicGroundRightward;
                case NoteType.DynamicGroundLeftward:
                    return DeploymentNoteType.DynamicGroundLeftward;
                default:
                    throw new ArgumentOutOfRangeException(nameof(noteType), noteType, $"No DeploymentNoteType mapping for {noteType}");
            }
        }

        public static NoteType ToNoteType(this DeploymentNoteType noteType)
        {
            switch (noteType)
            {
                case DeploymentNoteType.Touch:
                    return NoteType.Touch;
                case DeploymentNoteType.DivineTouch:
                    return NoteType.DivineTouch;
                case DeploymentNoteType.HoldStart:
                    return NoteType.HoldStart;
                case DeploymentNoteType.HoldRelay:
                    return NoteType.HoldRelay;
                case DeploymentNoteType.HoldMeshRelay:
                    return NoteType.HoldRelayHidden;
                case DeploymentNoteType.HoldEnd:
                    return NoteType.HoldEnd;
                case DeploymentNoteType.HoldEndUnjudge:
                    return NoteType.HoldEndUnjudge;
                case DeploymentNoteType.SpaceHoldStart:
                case DeploymentNoteType.SpaceHoldRelay:
                    return NoteType.SpaceHoldRelay;
                case DeploymentNoteType.SpaceHoldMeshRelay:
                    return NoteType.SpaceHoldRelayHidden;
                case DeploymentNoteType.SpaceHoldEnd:
                    return NoteType.SpaceHoldRelay;
                case DeploymentNoteType.SpaceBreak:
                    return NoteType.SpaceBreak;
                case DeploymentNoteType.DynamicGroundUpward:
                    return NoteType.DynamicGroundUpward;
                case DeploymentNoteType.DynamicGroundDownward:
                    return NoteType.DynamicGroundDownward;
                case DeploymentNoteType.DynamicGroundRightward:
                    return NoteType.DynamicGroundRightward;
                case DeploymentNoteType.DynamicGroundLeftward:
                    return NoteType.DynamicGroundLeftward;
                default:
                    throw new ArgumentOutOfRangeException(nameof(noteType), noteType, $"No NoteType mapping for {noteType}");
            }
        }
    }

}

