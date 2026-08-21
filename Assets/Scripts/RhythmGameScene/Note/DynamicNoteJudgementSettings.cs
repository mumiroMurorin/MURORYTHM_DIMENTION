using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Note Judgement Settings/Dynamic", fileName = "DynamicNoteJudgementSettings")]
public class DynamicNoteJudgementSettings : NoteJudgementSettings
{
    [SerializeField] float judgeMagnitude = 1f;

    public float JudgeMagnitude => judgeMagnitude;
}
