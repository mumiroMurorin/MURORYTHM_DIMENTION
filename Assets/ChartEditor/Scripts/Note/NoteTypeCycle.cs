using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public static class NoteTypeCycle
    {
        public static DeploymentNoteType NextNoteType(DeploymentNoteType type)
        {
            switch (type)
            {
                case DeploymentNoteType.Touch:
                    return DeploymentNoteType.DivineTouch;
                case DeploymentNoteType.DivineTouch:
                    return DeploymentNoteType.Touch;

                case DeploymentNoteType.HoldStart:
                    return DeploymentNoteType.DivineHoldStart;
                case DeploymentNoteType.DivineHoldStart:
                    return DeploymentNoteType.HoldStart;

                case DeploymentNoteType.DynamicGroundUpward:
                    return DeploymentNoteType.DynamicGroundDownward;
                case DeploymentNoteType.DynamicGroundDownward:
                    return DeploymentNoteType.DynamicGroundUpward;

                case DeploymentNoteType.DynamicGroundLeftward:
                    return DeploymentNoteType.DynamicGroundRightward;
                case DeploymentNoteType.DynamicGroundRightward:
                    return DeploymentNoteType.DynamicGroundLeftward;

                case DeploymentNoteType.HoldEnd:
                    return DeploymentNoteType.HoldEndUnjudge;
                case DeploymentNoteType.HoldEndUnjudge:
                    return DeploymentNoteType.HoldEnd;

                case DeploymentNoteType.HoldRelay:
                    return DeploymentNoteType.HoldMeshRelay;
                case DeploymentNoteType.HoldMeshRelay:
                    return DeploymentNoteType.HoldRelay;

                case DeploymentNoteType.SpaceHoldRelay:
                    return DeploymentNoteType.SpaceHoldMeshRelay;
                case DeploymentNoteType.SpaceHoldMeshRelay:
                    return DeploymentNoteType.SpaceHoldRelay;

            }

            return type;
        }
    }
}
