using UnityEngine;

public class InteractNoteEffectSpawner_SpaceHoldBullet : InteractNoteEffectSpawner
{
    [SerializeField] float fireDelayInterval = 0.03f;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        if (judgementData == null) { return false; }
        if (judgementData.NoteData is not ISpaceHoldBulletEffectNoteData spaceHoldData) { return false; }

        return spaceHoldData.IsSpaceHoldEnd;
    }

    public override IInteractNoteEffectController Spawn(NoteJudgementData judgementData)
    {
        if (judgementData.NoteData is ISpaceHoldBulletEffectNoteData { IsSpaceHoldEnd: true } spaceHoldData)
        {
            SpaceHoldBulletInteractEffectController.FireBullets(spaceHoldData.HoldNumber, fireDelayInterval);
            return null;
        }

        return base.Spawn(judgementData);
    }

    protected override Vector3 CalcSpawnPos(NoteJudgementData judgementData)
    {
        return Vector3.zero;
    }

    protected override Quaternion CalcSpawnRotate(NoteJudgementData judgementData)
    {
        return Quaternion.identity;
    }
}
