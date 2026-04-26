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
                case DeploymentNoteType.DynamicGroundUpward:
                    return DeploymentNoteType.DynamicGroundDownward;
                case DeploymentNoteType.DynamicGroundDownward:
                    return DeploymentNoteType.DynamicGroundUpward;
                case DeploymentNoteType.DynamicGroundLeftward:
                    return DeploymentNoteType.DynamicGroundRightward;
                case DeploymentNoteType.DynamicGroundRightward:
                    return DeploymentNoteType.DynamicGroundLeftward;
                case DeploymentNoteType.SpaceHold:
                    return DeploymentNoteType.SpaceHoldHidden;
                case DeploymentNoteType.SpaceHoldHidden:
                    return DeploymentNoteType.SpaceHold;
            }

            return type;
        }

        public static DeploymentNoteType NextHoldNoteType(DeploymentNoteType type, bool isTailNote)
        {
            if (isTailNote)
            {
                switch (type)
                {
                    case DeploymentNoteType.Hold:
                        return DeploymentNoteType.HoldEndUnjudge;
                    case DeploymentNoteType.HoldEndUnjudge:
                        return DeploymentNoteType.Hold;
                }
            }
            else
            {
                switch (type)
                {
                    case DeploymentNoteType.Hold:
                        return DeploymentNoteType.HoldHidden;
                    case DeploymentNoteType.HoldHidden:
                        return DeploymentNoteType.Hold;
                }
            }

            return type;
        }
    }
}
