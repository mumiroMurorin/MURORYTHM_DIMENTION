using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public static class EditModeExtensions
    {
        public static bool IsInEditModeList(this EditMode editMode, EditMode[] targetEditModes)
        {
            foreach (var mode in targetEditModes)
            {
                if (mode == editMode) { return true; }
            }
            return false;
        }
    }
}