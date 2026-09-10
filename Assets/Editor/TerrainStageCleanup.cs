using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Clears all painted grass/flower detail layers and every scattered tree instance from the hill in
/// the open scene, then plants a single large tree at the hill's peak.
/// </summary>
public static class TerrainStageCleanup
{
    private const string TerrainAName = "Terrain_5";
    private const string TerrainBName = "Terrain_(-200.00, -24.60, -251.00)";
    private const string BigTreePrefabPath = "Assets/Imports/Stylized Nature Environment/Prefabs/tree_a.prefab";
    // World-tree feel: noticeably wider trunk/canopy (X/Z) relative to height (Y), and larger overall.
    private static readonly Vector3 BigTreeScale = new Vector3(4.5f, 4f, 4.5f);

    [MenuItem("Tools/Terrain Bake/Scene - Clear Grass+Trees, Plant One Big Tree")]
    public static void ClearAndPlantBigTree()
    {
        Scene scene = SceneManager.GetActiveScene();

        int clearedDetailTerrains = ClearGrassDetails(scene, TerrainAName);
        clearedDetailTerrains += ClearGrassDetails(scene, TerrainBName);

        int removedTrees = RemoveAllTrees(scene);

        GameObject planted = PlantBigTreeAtHillPeak(scene, TerrainAName);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (planted != null)
            Debug.Log($"[TerrainStageCleanup] Cleared grass on {clearedDetailTerrains} terrain(s), removed {removedTrees} tree instance(s), " +
                      $"and planted 1 big tree ('{planted.name}') at the hill peak. Scene saved.");
        else
            Debug.LogWarning($"[TerrainStageCleanup] Cleared grass on {clearedDetailTerrains} terrain(s) and removed {removedTrees} tree instance(s), " +
                              "but failed to plant the big tree - see previous error.");
    }

    private static int ClearGrassDetails(Scene scene, string terrainName)
    {
        GameObject go = FindInScene(scene, terrainName);
        if (go == null) return 0;
        Terrain terrain = go.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null) return 0;

        TerrainData data = terrain.terrainData;
        if (data.detailPrototypes != null && data.detailPrototypes.Length > 0)
            data.detailPrototypes = new DetailPrototype[0];

        return 1;
    }

    private static int RemoveAllTrees(Scene scene)
    {
        List<GameObject> toRemove = new List<GameObject>();

        // Anything that is (or was instantiated from) one of the Stylized Nature Environment tree_* prefabs.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLowerInvariant().StartsWith("tree_") || t.name.ToLowerInvariant().StartsWith("tree "))
                    toRemove.Add(t.gameObject);
            }
        }

        // Also empty out a "Trees" grouping object if one exists, in case its children weren't caught above.
        GameObject treesGroup = FindInScene(scene, "Trees");
        if (treesGroup != null)
        {
            for (int i = treesGroup.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = treesGroup.transform.GetChild(i).gameObject;
                if (!toRemove.Contains(child))
                    toRemove.Add(child);
            }
        }

        foreach (GameObject go in toRemove)
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }
        return toRemove.Count;
    }

    private static GameObject PlantBigTreeAtHillPeak(Scene scene, string terrainName)
    {
        GameObject terrainGO = FindInScene(scene, terrainName);
        if (terrainGO == null)
        {
            Debug.LogError($"[TerrainStageCleanup] '{terrainName}' not found in the active scene.");
            return null;
        }
        Terrain terrain = terrainGO.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError($"[TerrainStageCleanup] '{terrainName}' has no Terrain component or TerrainData.");
            return null;
        }

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BigTreePrefabPath);
        if (treePrefab == null)
        {
            Debug.LogError($"[TerrainStageCleanup] Could not load tree prefab at {BigTreePrefabPath}.");
            return null;
        }

        Vector3 size = terrain.terrainData.size;
        Vector3 center = terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        float surfaceY = terrain.SampleHeight(center) + terrain.transform.position.y;

        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, scene);
        tree.name = "BigTree";
        tree.transform.position = new Vector3(center.x, surfaceY, center.z);
        tree.transform.localScale = BigTreeScale;

        GameObject treesGroup = FindInScene(scene, "Trees");
        if (treesGroup != null)
        {
            tree.transform.SetParent(treesGroup.transform, true);
            treesGroup.SetActive(true);
        }

        return tree;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = TerrainShapeSculptor.FindDeep(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }
}
