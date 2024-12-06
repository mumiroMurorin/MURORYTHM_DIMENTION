using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public enum Judgement
    {
        Perfect,
        Great,
        Good,
        Miss,
        None, // ”»’è‚ª‚È‚¢Žž
    }

    /// <summary>
    /// ”»’è‚ð‹L˜^‚·‚é
    /// </summary>
    public interface IJudgementRecorder
    {
        void RecordJudgement(Judgement judgement);
    }
}