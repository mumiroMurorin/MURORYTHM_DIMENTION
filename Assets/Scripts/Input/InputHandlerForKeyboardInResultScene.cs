using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitionerInResultScene;

public class InputHandlerForKeyboardInResultScene : InputHandlerForKeyboard
{
    [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;

    protected override void EachUpdate()
    {
        // Ctrl + Z‚ÅƒŠƒgƒ‰ƒC
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) { Retry(); }
    }

    private void Retry()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.Retry);
    }
}
