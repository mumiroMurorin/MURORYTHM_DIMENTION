using UnityEngine;

public class InteractNoteEffectSpawner_HoldEndGroundFly : InteractNoteEffectSpawner
{
    [Header("Ground Fly")]
    [SerializeField] Vector3 localSpawnOffset = Vector3.zero;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        if (judgementData == null) { return false; }
        if (judgementData.Judgement == Judgement.Miss) { return false; }
        return judgementData.NoteData is NoteData_HoldEnd;
    }

    public override IInteractNoteEffectController Spawn(NoteJudgementData judgementData)
    {
        IInteractNoteEffectController controller = base.Spawn(judgementData);

        if (controller is InteractNoteEffectController_HoldEndGroundFly groundFlyController &&
            judgementData.NoteData is NoteData_HoldEnd noteData)
        {
            groundFlyController.InitializeRange(noteData.Range);
        }

        return controller;
    }

    protected override Vector3 CalcSpawnPos(NoteJudgementData judgementData)
    {
        Transform spawnParent = parent != null ? parent : transform;
        return spawnParent.TransformPoint(localSpawnOffset);
    }

    protected override Quaternion CalcSpawnRotate(NoteJudgementData judgementData)
    {
        Transform spawnParent = parent != null ? parent : transform;
        return spawnParent.rotation;
    }
}
