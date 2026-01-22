using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInResultScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] OperationDictionary operationDictionary;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.LoadData;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("yTransitionzTransition to \"LoadData\"");

            SoundManager.Instance.PlayBGM(BGM_Type.Result);
            RegisterOperation();

            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.FadeIn);

        }

        private void RegisterOperation()
        {
            operationDictionary.RegisterOperation(OperationTag.Result_ResultConfirm, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.FadeOut); });
        }
    }

}
