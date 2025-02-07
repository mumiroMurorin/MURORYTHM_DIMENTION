using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class InteractNoteEffectSpawner_HoldRelay : InteractNoteEffectSpawner
    {
        [SerializeField] GameObject perfectEffect;
        [SerializeField] GameObject greatEffect;
        [SerializeField] GameObject goodEffect;

        public override bool ConditionChecker(NoteJudgementData judgementData)
        {
            return judgementData.NoteData.NoteType == NoteType.HoldRelay;
        }

        public override GameObject Spawn(NoteJudgementData judgementData)
        {
            Vector3 pos = Vector3.zero;
            GameObject obj;

            switch (judgementData.Judgement)
            {
                case Judgement.Perfect:
                    obj = Instantiate(perfectEffect, pos, Quaternion.identity, parent);
                    break;
                case Judgement.Great:
                    obj = Instantiate(greatEffect, pos, Quaternion.identity, parent);
                    break;
                case Judgement.Good:
                    obj = Instantiate(goodEffect, pos, Quaternion.identity, parent);
                    break;
                default:
                    return null;
            }

            SetDataForEffect(obj, judgementData.NoteData as NoteData_HoldRelay);
            return obj;
        }

        /// <summary>
        /// エフェクトを初期化する
        /// </summary>
        /// <param name="effectObject"></param>
        /// <param name="noteData"></param>
        private void SetDataForEffect(GameObject effectObject, NoteData_HoldRelay noteData)
        {
            if (!effectObject.TryGetComponent(out IInteractNoteEffectController<NoteData_HoldRelay> effect)) { return; }
            effect.SetEffect(noteData);
        }
    }

}
