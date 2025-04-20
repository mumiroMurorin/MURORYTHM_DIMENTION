using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Perfect`Good‚Ü‚Å‚Ì”»’è‹–—e”ÍˆÍ‚ğ‚Ü‚Æ‚ß‚½ƒNƒ‰ƒX
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObject/JudgementWindow", fileName = "JudgementWindow")]
public class JudgementWindow : ScriptableObject
{
    [Header("‚»‚ê‚¼‚ê‚Ì”»’è(}n•b)")]
    [SerializeField] float perfectWindow;
    [SerializeField] float greatWindow;
    [SerializeField] float goodWindow;

    public float PerfectWindow { get { return perfectWindow; } }
    public float GreatWindow { get { return greatWindow; } }
    public float GoodWindow { get { return goodWindow; } }

    public Judgement GetJudgement(float currentTime, float judgeTime)
    {
        return GetJudgementAndError(currentTime, judgeTime).Judgement;
    }

    public JudgementAndErrorTime GetJudgementAndError(float currentTime, float judgeTime)
    {
        float error = judgeTime - currentTime;

        // Good”»’è‘O
        if (judgeTime - goodWindow > currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.None, Error = error }; }
        // Good”»’èŒã
        if (judgeTime + goodWindow < currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.Miss, Error = error }; }

        float timingDiff = Mathf.Abs(judgeTime - currentTime);

        if (timingDiff <= perfectWindow) { return new JudgementAndErrorTime { Judgement = Judgement.Perfect, Error = error }; }
        else if (timingDiff <= greatWindow) { return new JudgementAndErrorTime { Judgement = Judgement.Great, Error = error }; }
        else if (timingDiff <= goodWindow) { return new JudgementAndErrorTime { Judgement = Judgement.Good, Error = error }; }

        return new JudgementAndErrorTime { Judgement = Judgement.None };
    }
}
