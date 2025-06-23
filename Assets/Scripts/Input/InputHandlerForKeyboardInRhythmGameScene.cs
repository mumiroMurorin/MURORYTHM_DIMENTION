using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitionerInRhythmGameScene;

public class InputHandlerForKeyboardInRhythmGameScene : InputHandlerForKeyboard
{
    [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;

    protected override void EachUpdate()
    {
        // LeftCtrl + Rでリザルトシーンへ
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R)) { SkipRhythmGame(); }

        // LeftCtrl + Sでセレクトシーンへ
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)) { BackSelectScene(); }

        // LeftCtrl + Zでリトライ
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) { Retry(); }
    }

    /// <summary>
    /// セレクトシーンに戻る
    /// </summary>
    private void BackSelectScene()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.TransitionSelectScene);
    }

    private void SkipRhythmGame()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.TransitionResultScene);
    }

    private void Retry()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.Retry);
    }
}
