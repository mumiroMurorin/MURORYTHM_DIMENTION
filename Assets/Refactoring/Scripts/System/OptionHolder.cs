using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class OptionHolder : INoteSpawnDataOptionHolder
    {
        public float NoteSpeed { get; set; }

    }

    public interface INoteSpawnDataOptionHolder
    {
        public float NoteSpeed { get; }
    }
}
