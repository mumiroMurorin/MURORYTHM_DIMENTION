using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class OptionHolder : INoteSpawnDataOptionHolder
    {
        /// <summary>
        /// ノーツが1秒間に動く(unity単位)速度
        /// </summary>
        public float NoteSpeed { get; set; } = 100f;

    }

    public interface INoteSpawnDataOptionHolder
    {
        public float NoteSpeed { get; }
    }
}
