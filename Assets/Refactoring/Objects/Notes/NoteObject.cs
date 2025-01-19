using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public abstract class NoteObject<T> : MonoBehaviour where T : INoteData
    {
        /// <summary>
        /// �e�X�̃f�[�^�ŏ�����
        /// </summary>
        /// <param name="noteData"></param>
        public abstract void Initialize(T noteData);
    }

}
