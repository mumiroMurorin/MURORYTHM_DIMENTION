using UnityEngine;

public class InteractNoteEffectSpawner_SpaceHoldEndRingFly : InteractNoteEffectSpawner
{
    [Header("Spawn")]
    [SerializeField] Vector3 localSpawnOffset = Vector3.zero;

    [Header("Visual")]
    [SerializeField] Color ringColor = Color.white;
    [SerializeField] Gradient trailColorGradient = new Gradient();

    protected override IInteractNoteEffectController Instantiate()
    {
        IInteractNoteEffectController controller = base.Instantiate();
        ApplyVisualSettings(controller);
        return controller;
    }

    public override IInteractNoteEffectController Spawn(NoteJudgementData judgementData)
    {
        IInteractNoteEffectController controller = base.Spawn(judgementData);
        ApplyVisualSettings(controller);
        return controller;
    }

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        if (judgementData == null) { return false; }
        if (judgementData.Judgement == Judgement.Miss) { return false; }

        return judgementData.NoteData is ISpaceHoldBulletEffectNoteData { IsSpaceHoldEnd: true };
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

    private void ApplyVisualSettings(IInteractNoteEffectController controller)
    {
        if (controller is not InteractNoteEffect_SpaceHoldEndRingFly ringController) { return; }

        ringController.SetVisualSettings(ringColor, trailColorGradient);
    }
}
