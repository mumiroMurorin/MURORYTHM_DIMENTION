using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StageTypeToPrefabDatabase", menuName = "ScriptableObject/StageTypeToPrefabDatabase")]
public class StageTypeToPrefabDatabase : ScriptableObject
{
    [SerializeField] StageTypeToPrefab[] stageTypeToPrefabs;

    public GameObject GetPrefab(StageType stageType)
    {
        if (stageTypeToPrefabs == null) { return null; }

        foreach (StageTypeToPrefab pair in stageTypeToPrefabs)
        {
            GameObject prefab = pair.CheckAndGetPrefab(stageType);
            if (prefab != null) { return prefab; }
        }

        return null;
    }
}

[Serializable]
public class StageTypeToPrefab
{
    [SerializeField] StageType stageType;
    [SerializeField] GameObject prefab;

    public GameObject CheckAndGetPrefab(StageType type)
    {
        if (stageType != type) { return null; }
        return prefab;
    }
}