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

    /// <summary>
    /// 修飾キーの判定 (左右どちらのキーでも反応する)
    /// </summary>
    public static class ModifierKey
    {
        public static bool Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        public static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }
}