using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInRootScene
{
    public class Transitioner_Setting : IPhaseTransitionerInRootScene
    {
        readonly PhaseStatusInRootScene status = PhaseStatusInRootScene.SettingOption;

        bool IPhaseTransitionerInRootScene.ConditionChecker(PhaseStatusInRootScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRootScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"OptionSetting\"");
        }
    }

}
