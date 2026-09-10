using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitionerInResultScene;

public class InputHandlerForKeyboardInResultScene : InputHandlerForKeyboard
{
    [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
    [SerializeField] ScoreRecorder scoreRecorder;
    [SerializeField] KeyCode deleteCurrentScoreKey = KeyCode.Delete;
    [SerializeField] bool requireLeftControlForDeleteCurrentScore = true;

    protected override void EachUpdate()
    {
        // Ctrl + Z‚ÅƒŠƒgƒ‰ƒC
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) { Retry(); }

        if (IsDeleteCurrentScoreShortcutDown()) { DeleteCurrentScore(); }
    }

    private void Retry()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.Retry);
    }

    private bool IsDeleteCurrentScoreShortcutDown()
    {
        if (requireLeftControlForDeleteCurrentScore && !Input.GetKey(KeyCode.LeftControl)) { return false; }

        return Input.GetKeyDown(deleteCurrentScoreKey);
    }

    private void DeleteCurrentScore()
    {
        if (scoreRecorder == null)
        {
            scoreRecorder = FindObjectOfType<ScoreRecorder>();
        }

        scoreRecorder?.DeleteCurrentPlayRecord();
    }
}
