using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public abstract class InteractNoteEffectSpawner : MonoBehaviour
    {
        [SerializeField] protected Transform parent;

        /// <summary>
        /// 評価アニメーションの出現に適しているか判定する
        /// </summary>
        /// <returns></returns>
        public abstract bool ConditionChecker(NoteJudgementData judgementData);

        /// <summary>
        /// インスタンス化
        /// </summary>
        /// <returns></returns>
        public abstract GameObject Spawn(NoteJudgementData judgementData);
    }

}
