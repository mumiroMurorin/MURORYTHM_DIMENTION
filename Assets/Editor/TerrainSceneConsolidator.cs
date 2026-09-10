using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the case where "Terrain_5" was added directly into an open scene (e.g. Shoma_Lab.unity)
/// as an instance override on top of the Stage_Creation_HIll prefab, instead of living inside the
/// prefab asset itself. This lets you sculpt/grass that scene-local object, then promote it back into
/// the prefab so every other instance picks it up too.
/// </summary>
public static class TerrainSceneConsolidator
{
    private const string PrefabPath = "Assets/Prefabs/Stages/Creation_HIll/Stage_Creation_HIll.prefab";
    private const string TerrainObjectName = "Terrain_5";
    private const string OutputDir = "Assets/Prefabs/Stages/Creation_HIll/BakedTerrain";
    private const string SceneTerrainDataPath = OutputDir + "/Terrain_5_Scene_TerrainData.asset";

    [MenuItem("Tools/Terrain Bake/Scene - Reshape+Grass Terrain_5 In Open Scene")]
    public static void ReshapeAndGrassInOpenScene()
    {
        GameObject terrainGO = FindInActiveScene(TerrainObjectName);
        if (terrainGO == null)
        {
            Debug.LogError($"[TerrainSceneConsolidator] '{TerrainObjectName}' was not found as a root-level object in the active scene.");
            return;
        }

        Terrain terrain = terrainGO.GetComponent<Terrain>();
        TerrainCollider terrainCollider = terrainGO.GetComponent<TerrainCollider>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError($"[TerrainSceneConsolidator] '{TerrainObjectName}' has no Terrain component or TerrainData.");
            return;
        }

        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        TerrainData data = TerrainGrassPainter.EnsureDedicatedTerrainData(
            terrain, terrainCollider, SceneTerrainDataPath, "Terrain_5_Scene_TerrainData");

        TerrainShapeSculptor.ApplyGaussianHill(terrain);

        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(terrainGO);
        Transform searchRoot = instanceRoot != null ? instanceRoot.transform : terrainGO.transform.root;

        int snapped = TerrainShapeSculptor.SnapPropsToSurface(terrain, searchRoot);
        TerrainGrassPainter.PaintGrassDetails(data, terrain, terrainGO.transform, searchRoot);

        EditorUtility.SetDirty(terrainGO);
        Scene scene = terrainGO.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TerrainSceneConsolidator] Reshaped and grass-painted the scene's Terrain_5 in '{scene.name}'. " +
                  $"Re-snapped {snapped} prop(s). Scene saved. " +
                  "Run 'Scene - Apply Terrain_5 To Prefab' next to push this into the prefab asset.");
    }

    [MenuItem("Tools/Terrain Bake/Scene - Apply Terrain_5 To Prefab")]
    public static void ApplySceneTerrainToPrefab()
    {
        GameObject terrainGO = FindInActiveScene(TerrainObjectName);
        if (terrainGO == null)
        {
            Debug.LogError($"[TerrainSceneConsolidator] '{TerrainObjectName}' was not found as a root-level object in the active scene.");
            return;
        }

        PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(terrainGO);
        if (status != PrefabInstanceStatus.Connected)
        {
            Debug.LogError("[TerrainSceneConsolidator] This Terrain_5 is not part of a connected prefab instance, so it can't be applied to a prefab.");
            return;
        }

        // Remove the prefab asset's own (now-superseded) internal Terrain_5 first, so we don't end up
        // with two overlapping Terrain_5 objects inside the prefab.
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform existing = TerrainShapeSculptor.FindDeep(prefabRoot.transform, TerrainObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[TerrainSceneConsolidator] Removed the prefab's own superseded internal Terrain_5.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        PrefabUtility.ApplyAddedGameObject(terrainGO, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(terrainGO.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[TerrainSceneConsolidator] Applied the scene's Terrain_5 into the prefab asset. " +
                   "Every instance of Stage_Creation_HIll now has this hill.");
    }

    [MenuItem("Tools/Terrain Bake/Scene - Apply ALL Stage Changes To Prefab")]
    public static void ApplyEntireStageToPrefab()
    {
        GameObject terrainGO = FindInActiveScene(TerrainObjectName);
        if (terrainGO == null)
        {
            Debug.LogError($"[TerrainSceneConsolidator] '{TerrainObjectName}' was not found as a root-level object in the active scene.");
            return;
        }

        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(terrainGO);
        if (instanceRoot == null)
        {
            Debug.LogError("[TerrainSceneConsolidator] Could not find the Stage_Creation_HIll prefab instance root in the scene.");
            return;
        }

        // Remove the prefab asset's own (now-superseded) internal Terrain_5 first, so ApplyPrefabInstance
        // doesn't end up leaving two overlapping Terrain_5 objects inside the prefab.
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform existing = TerrainShapeSculptor.FindDeep(prefabRoot.transform, TerrainObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[TerrainSceneConsolidator] Removed the prefab's own superseded internal Terrain_5.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        PrefabUtility.ApplyPrefabInstance(instanceRoot, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(terrainGO.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[TerrainSceneConsolidator] Applied every override on the Stage_Creation_HIll instance " +
                  "(Terrain_5, tree/grass changes, everything) into the prefab asset.");
    }

    private static GameObject FindInActiveScene(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = TerrainShapeSculptor.FindDeep(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }
}
