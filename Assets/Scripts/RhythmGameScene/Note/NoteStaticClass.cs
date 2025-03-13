using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class CalcNoteTransform
    {
        static public float NoteAngle(int[] range)
        {
            return (range[0] + (range.Length - 1) / 2f - 7.5f) * 11.25f;
        }

    }
}