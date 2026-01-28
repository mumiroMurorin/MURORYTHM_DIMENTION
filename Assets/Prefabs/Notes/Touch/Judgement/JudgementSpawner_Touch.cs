using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgementSpawner_Touch : JudgementEffectSpawner
{
    [Header("îªíËèoåªîºåa")]
    [SerializeField] float radius = 8f;

    public override bool ConditionChecker(NoteJudgementData judgementData)
    {
        return judgementData.NoteData.NoteType == NoteType.Touch;
    }

    protected override Vector3 CalcSpawnPos(NoteJudgementData judgementData)
    {
        if (judgementData.NoteData is not NoteData_Touch noteData) { return Vector3.zero; }

        float centerIndex = noteData.Range[0] + noteData.Range.Length / 2f;
        float radian = (11.25f * centerIndex - 180f) * Mathf.Deg2Rad;
        Vector3 pos = new Vector3(radius * Mathf.Cos(radian), radius * Mathf.Sin(radian), 0);

        return pos;
    }

    protected override Quaternion CalcSpawnRotate(NoteJudgementData judgementData)
    {
        Vector3 pos = CalcSpawnPos(judgementData);
        Vector3 rot = MeshGenerate.MeshGenerator.GetEularAngle(pos, Vector3.zero) - new Vector3(0, 0, 90f);

        return Quaternion.Euler(rot);
    }
}
