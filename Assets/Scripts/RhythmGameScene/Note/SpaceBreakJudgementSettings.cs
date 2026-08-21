using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Note Judgement Settings/SpaceBreak", fileName = "SpaceBreakJudgementSettings")]
public class SpaceBreakJudgementSettings : NoteJudgementSettings
{
    [SerializeField] float judgementMarginRadius = 0.25f;
    [SerializeField] float judgeMagnitude = 1f;

    public float JudgementMarginRadius => judgementMarginRadius;
    public float JudgeMagnitude => judgeMagnitude;
}
