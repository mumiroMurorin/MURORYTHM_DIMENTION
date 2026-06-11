using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// Divine hold start note component.
/// </summary>
public class NoteObject_DivineHoldStart : NoteObject<NoteData_DivineHoldStart>
{
    NoteData_DivineHoldStart noteData;

    bool isJudged;

    public override void Initialize(NoteData_DivineHoldStart data)
    {
        noteData = data;
        Bind();
    }

    private void Bind()
    {
        if (noteData == null) { return; }
        if (noteData.OptionGetter.IsAutoMode) { return; }

        foreach (int index in noteData.Range)
        {
            if (noteData.SliderInput == null) { break; }
            if (noteData.Timer == null) { break; }

            noteData.SliderInput?.GetSliderInputReactiveProperty(index)
                .Where(isHoldStart => isHoldStart)
                .Where(_ => !isJudged)
                .Where(_ => noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => !noteData.OptionGetter.IsAutoMode)
                .Subscribe(_ =>
                {
                    NormalJudge();
                    SetDisable();
                })
                .AddTo(this.gameObject);
        }
    }

    private void Update()
    {
        if (noteData.OptionGetter.IsAutoMode && noteData.Timing <= noteData.Timer.Time)
        {
            NormalJudge();
            SetDisable();
            return;
        }

        if (noteData.JudgementWindow.IsPassJudgementRange(noteData.Timer.Time, noteData.Timing))
        {
            NormalJudge();
            SetDisable();
        }
    }

    private void NormalJudge()
    {
        Judgement judgement;
        if (noteData.OptionGetter.IsAutoMode) { judgement = Judgement.Perfect; }
        else { judgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing); }

        var judgementData = new NoteJudgementData(this.noteData, judgement, noteData.Timer.Time - noteData.Timing);
        noteData.JudgementRecorder?.RecordJudgement(judgementData);

        SoundManager.Instance.PlaySE(noteData.NoteType, judgement);
        isJudged = true;
    }

    private void SetDisable()
    {
        this.gameObject.SetActive(false);
    }
}

public class NoteData_DivineHoldStart : NoteData_HoldStart
{
    public override NoteType NoteType => NoteType.DivineHoldStart;
}
