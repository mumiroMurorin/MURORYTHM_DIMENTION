using UnityEngine;
using NaughtyAttributes;

public abstract class NoteJudgementSettings : ScriptableObject
{
    [SerializeField, Expandable] JudgementWindowObject judgementWindowObject;
    [SerializeField] DifficultyToJudgementWindow[] difficultyJudgementWindows;

    public JudgementWindow CreateJudgementWindowOrDefault(JudgementWindow fallback)
    {
        if (judgementWindowObject == null) { return fallback; }
        if (judgementWindowObject.JudgementWindow == null) { return fallback; }

        return judgementWindowObject.JudgementWindow.Copy();
    }

    public JudgementWindow CreateJudgementWindowOrDefault(Difficulty difficulty, JudgementWindow fallback)
    {
        JudgementWindow difficultyWindow = FindJudgementWindow(difficulty);
        if (difficultyWindow != null) { return difficultyWindow.Copy(); }

        return CreateJudgementWindowOrDefault(fallback);
    }

    public JudgementWindow CreateJudgementWindowIfMissing(JudgementWindow current)
    {
        if (current != null) { return current; }

        return CreateJudgementWindowOrDefault(current);
    }

    public JudgementWindow CreateJudgementWindowIfMissing(JudgementWindow current, Difficulty difficulty)
    {
        if (current != null) { return current; }

        return CreateJudgementWindowOrDefault(difficulty, current);
    }

    private JudgementWindow FindJudgementWindow(Difficulty difficulty)
    {
        if (difficultyJudgementWindows == null) { return null; }

        for (int i = 0; i < difficultyJudgementWindows.Length; i++)
        {
            DifficultyToJudgementWindow data = difficultyJudgementWindows[i];
            if (data == null) { continue; }
            if (!data.CheckCondition(difficulty)) { continue; }

            return data.JudgementWindow;
        }

        return null;
    }
}
