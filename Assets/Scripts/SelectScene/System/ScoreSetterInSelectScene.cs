using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreSetterInSelectScene : MonoBehaviour
{
    [SerializeField] int combo = 1234;
    [SerializeField] int addNote = 1230;

    [Inject] IScoreSetter scoreSetter;
    [Inject] IJudgementRecorder judgementRecorder;

    public void Initialize()
    {
        if (scoreSetter == null) { return; }
        if (judgementRecorder == null) { return; }

        scoreSetter?.ResetScore();
        scoreSetter?.SetScoreCalculater(new ScoreCalculater(combo));

        for (int i = 0;i < addNote; i++)
        {
            var noteData = new NoteData_Touch();
            judgementRecorder.RecordJudgement(new NoteJudgementData(noteData, Judgement.Perfect, 0f));
        }
    }
}
