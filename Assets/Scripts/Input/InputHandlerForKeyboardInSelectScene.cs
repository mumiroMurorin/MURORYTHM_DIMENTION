using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitionerInSelectScene;

public class InputHandlerForKeyboardInSelectScene : InputHandlerForKeyboard
{
    [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitioner;

    protected override void EachUpdate()
    {
        // LeftCtrl + Rでルートシーンへ
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R)) { TransitionRootScene(); }
    }

    private void TransitionRootScene()
    {
        phaseTransitioner.Value.TransitionPhase(PhaseStatusInSelectScene.TransitionRootScene);
    }
}
