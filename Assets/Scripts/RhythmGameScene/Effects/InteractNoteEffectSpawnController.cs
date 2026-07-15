using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectSpawnController : MonoBehaviour
{
    [SerializeField] List<InteractNoteEffectSpawner> spawners;

    public void SpawnEffect(NoteJudgementData judgementData)
    {
        if (spawners == null) { return; }

        foreach (InteractNoteEffectSpawner spawner in spawners)
        {
            if (spawner == null) { continue; }
            if (!spawner.ConditionChecker(judgementData)) { continue; }

            spawner.Spawn(judgementData);
        }
    }
}