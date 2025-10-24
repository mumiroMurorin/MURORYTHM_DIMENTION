using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreSetterInSelectScene : MonoBehaviour
{
    [Header("éñëOÇ…ê∂ê¨Ç≥ÇÍÇÈÉmÅ[Écêî")]
    [SerializeField] int perfectNum = 1219;
    [SerializeField] int greatNum = 10;
    [SerializeField] int maxCombo = 1234;

    [Inject] IScoreSetter scoreSetter;
    [Inject] IJudgementRecorder judgementRecorder;

    public int AddNoteCount { get { return maxCombo - perfectNum - greatNum; } }

    public void Initialize()
    {
        if (scoreSetter == null) { return; }
        if (judgementRecorder == null) { return; }

        scoreSetter?.ResetScore();
        scoreSetter?.SetScoreCalculater(new ScoreCalculater(maxCombo));

        for (int i = 0; i < greatNum - 1; i++)
        {
            judgementRecorder.AddJudgement(Judgement.Great, false);
        }

        for (int i = 0; i < perfectNum - 1; i++)
        {
            judgementRecorder.AddJudgement(Judgement.Perfect, false);
        }

        if (greatNum > 0) { judgementRecorder.AddJudgement(Judgement.Great, true); }
        if (perfectNum > 0) { judgementRecorder.AddJudgement(Judgement.Perfect, true); }
    }
}
