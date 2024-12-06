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
        None, // ���肪�Ȃ���
    }

    /// <summary>
    /// ������L�^����
    /// </summary>
    public interface IJudgementRecorder
    {
        void RecordJudgement(Judgement judgement);
    }
}