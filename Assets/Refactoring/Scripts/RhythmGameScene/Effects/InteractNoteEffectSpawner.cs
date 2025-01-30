using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public abstract class InteractNoteEffectSpawner : MonoBehaviour
    {
        [SerializeField] protected Transform parent;

        /// <summary>
        /// �]���A�j���[�V�����̏o���ɓK���Ă��邩���肷��
        /// </summary>
        /// <returns></returns>
        public abstract bool ConditionChecker(NoteJudgementData judgementData);

        /// <summary>
        /// �C���X�^���X��
        /// </summary>
        /// <returns></returns>
        public abstract GameObject Spawn(NoteJudgementData judgementData);
    }

}
