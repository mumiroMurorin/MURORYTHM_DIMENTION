using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class OptionHolder : INoteSpawnDataOptionHolder
    {
        /// <summary>
        /// �m�[�c��1�b�Ԃɓ���(unity�P��)���x
        /// </summary>
        public float NoteSpeed { get; set; } = 100f;

    }

    public interface INoteSpawnDataOptionHolder
    {
        public float NoteSpeed { get; }
    }
}
