using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// ”»’è‚ð‹L˜^‚·‚é
    /// </summary>
    public interface IJudgementRecorder
    {
        void RecordJudgement(Judgement judgement);
    }

    public interface ITimeGetter
    {
        float Time { get; }
    }
}