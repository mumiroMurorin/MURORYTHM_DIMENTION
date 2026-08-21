using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Note Judgement Settings/SpaceHold", fileName = "SpaceHoldJudgementSettings")]
public class SpaceHoldJudgementSettings : NoteJudgementSettings
{
    [SerializeField] float judgementMarginRadius = 0.25f;

    public float JudgementMarginRadius => judgementMarginRadius;
}
