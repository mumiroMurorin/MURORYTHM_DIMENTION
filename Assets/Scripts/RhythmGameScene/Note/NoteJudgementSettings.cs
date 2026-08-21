using UnityEngine;
using NaughtyAttributes;

public abstract class NoteJudgementSettings : ScriptableObject
{
    [SerializeField, Expandable] JudgementWindowObject judgementWindowObject;

    public JudgementWindow CreateJudgementWindowOrDefault(JudgementWindow fallback)
    {
        if (judgementWindowObject == null) { return fallback; }
        if (judgementWindowObject.JudgementWindow == null) { return fallback; }

        return judgementWindowObject.JudgementWindow.Copy();
    }

    public JudgementWindow CreateJudgementWindowIfMissing(JudgementWindow current)
    {
        if (current != null) { return current; }

        return CreateJudgementWindowOrDefault(current);
    }
}
