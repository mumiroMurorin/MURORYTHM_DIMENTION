using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitionerInRhythmGameScene;
using VContainer;

public class InputHandlerForKeyboardInRhythmGameScene : InputHandlerForKeyboard
{
    [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;

    IOptionSetter optionSetter;
    INoteSpawnDataOptionHolder spawnDataGetter;

    [Inject]
    public void Constructor(IOptionSetter optionSetter, INoteSpawnDataOptionHolder spawnDataGetter)
    {
        this.optionSetter = optionSetter;
        this.spawnDataGetter = spawnDataGetter;
    }

    protected override void EachUpdate()
    {
        // LeftCtrl + Aでオートモード
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A)) { SwitchAutoMode(); }

        // LeftCtrl + Rでリザルトシーンへ
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R)) { SkipRhythmGame(); }

        // LeftCtrl + Sでセレクトシーンへ
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)) { BackSelectScene(); }

        // LeftCtrl + Zでリトライ
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) { Retry(); }
    }

    /// <summary>
    /// オートモード切替
    /// </summary>
    private void SwitchAutoMode()
    {
        optionSetter.SetAutoMode(!spawnDataGetter.IsAutoMode);
      
        if (spawnDataGetter.IsAutoMode) { Debug.Log("【System】オートモードに切り替え"); }
        else { Debug.Log("【System】オートモード終了"); }
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
