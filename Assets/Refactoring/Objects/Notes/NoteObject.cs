using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public abstract class NoteObject<T> : MonoBehaviour where T : INoteData
    {
        /// <summary>
        /// 各々のデータで初期化
        /// </summary>
        /// <param name="noteData"></param>
        public abstract void Initialize(T noteData);
    }

}
