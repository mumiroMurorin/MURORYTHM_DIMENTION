using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TerrainGrassPainter
{
    private const string PrefabPath = "Assets/Prefabs/Stages/Creation_HIll/Stage_Creation_HIll.prefab";
    private const string TerrainObjectName = "Terrain_5";
    private const string OutputDir = "Assets/Prefabs/Stages/Creation_HIll/BakedTerrain";
    private const string NewTerrainDataPath = OutputDir + "/Terrain_5_TerrainData.asset";

    private const int DetailResolution = 512;
    private const int DetailResolutionPerPatch = 16;
    private const int BaseDensity = 7;        // out of ~16, kept moderate for performance
    private const float MaxSlopeDegrees = 35f; // no grass past this steepness
    private const float RockExclusionRadius = 3.5f;
    private const float BushExclusionRadius = 2.0f;
    private const float TreeExclusionRadius = 1.5f;

    [MenuItem("Tools/Terrain Bake/Paint Grass On Terrain_5 (Prefab)")]
    public static void PaintGrass()
    {
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform terrainTransform = TerrainShapeSculptor.FindDeep(root.transform, TerrainObjectName);
            if (terrainTransform == null)
            {
                Debug.LogError($"[TerrainGrassPainter] '{TerrainObjectName}' was not found inside {PrefabPath}.");
                return;
            }

            Terrain terrain = terrainTransform.GetComponent<Terrain>();
            TerrainCollider terrainCollider = terrainTransform.GetComponent<TerrainCollider>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError($"[TerrainGrassPainter] '{TerrainObjectName}' has no Terrain component or TerrainData.");
                return;
            }

            TerrainData data = EnsureDedicatedTerrainData(terrain, terrainCollider, NewTerrainDataPath, "Terrain_5_TerrainData");
            PaintGrassDetails(data, terrain, terrainTransform, root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[TerrainGrassPainter] Grass detail layers painted on Terrain_5 (prefab) with wind sway enabled.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// Makes sure the given Terrain uses its own TerrainData asset (duplicating the current one if it's
    /// still pointing at a shared asset) so detail/height edits never leak into other scenes/prefabs.
    /// </summary>
    public static TerrainData EnsureDedicatedTerrainData(Terrain terrain, TerrainCollider terrainCollider, string targetPath, string dataName)
    {
        TerrainData data = terrain.terrainData;
        bool alreadyDedicated = data != null && AssetDatabase.GetAssetPath(data) == targetPath;
        if (alreadyDedicated) return data;

        TerrainData duplicated = Object.Instantiate(data);
        duplicated.name = dataName;
        string dir = Path.GetDirectoryName(targetPath).Replace('\\', '/');
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
        AssetDatabase.CreateAsset(duplicated, assetPath);

        terrain.terrainData = duplicated;
        if (terrainCollider != null)
            terrainCollider.terrainData = duplicated;

        return duplicated;
    }

    /// <summary>Paints wind-enabled grass detail layers onto the given (already-dedicated) TerrainData.</summary>
    public static void PaintGrassDetails(TerrainData data, Terrain terrain, Transform terrainTransform, Transform searchRoot)
    {
        Texture2D grass01 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imports/Stylized Nature Environment/Nature/grass01.PSD");
        Texture2D grass02 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imports/Stylized Nature Environment/Nature/grass02.PSD");
        Texture2D grass03 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imports/Stylized Nature Environment/Nature/grass03.PSD");

        List<Texture2D> grassTextures = new List<Texture2D>();
        foreach (Texture2D t in new[] { grass01, grass02, grass03 })
            if (t != null) grassTextures.Add(t);

        if (grassTextures.Count == 0)
        {
            Debug.LogError("[TerrainGrassPainter] Could not load grass01/02/03.PSD from the Stylized Nature Environment pack.");
            return;
        }

        DetailPrototype[] prototypes = new DetailPrototype[grassTextures.Count];
        for (int i = 0; i < grassTextures.Count; i++)
        {
            prototypes[i] = new DetailPrototype
            {
                usePrototypeMesh = false,
                prototypeTexture = grassTextures[i],
                renderMode = DetailRenderMode.Grass,
                useInstancing = true,
                minWidth = 0.8f,
                maxWidth = 1.4f,
                minHeight = 0.6f,
                maxHeight = 1.1f,
                noiseSeed = 12345 + i,
                noiseSpread = 0.6f,
                healthyColor = new Color(0.55f, 0.75f, 0.35f),
                dryColor = new Color(0.75f, 0.72f, 0.35f),
                density = 1f
            };
        }
        data.detailPrototypes = prototypes;

        // Native, GPU-cheap wind sway for all Grass-mode detail instances.
        data.wavingGrassSpeed = 0.4f;
        data.wavingGrassAmount = 0.3f;
        data.wavingGrassStrength = 0.4f;
        data.wavingGrassTint = new Color(0.85f, 0.9f, 0.8f);

        terrain.detailObjectDistance = 500f;
        terrain.detailObjectDensity = 1f;

        data.SetDetailResolution(DetailResolution, DetailResolutionPerPatch);

        List<Vector3> rockPoints = CollectWorldPositions(searchRoot, "Rocks");
        List<Vector3> bushPoints = CollectWorldPositions(searchRoot, "Bushes");
        List<Vector3> treePoints = CollectWorldPositions(searchRoot, "Trees");

        PaintDetailLayers(data, terrainTransform, prototypes.Length, rockPoints, bushPoints, treePoints);
    }

    private static List<Vector3> CollectWorldPositions(Transform root, string groupName)
    {
        List<Vector3> points = new List<Vector3>();
        Transform group = TerrainShapeSculptor.FindDeep(root, groupName);
        if (group == null) return points;

        foreach (Transform t in group.GetComponentsInChildren<Transform>())
        {
            if (t == group) continue;
            points.Add(t.position);
        }
        return points;
    }

    private static void PaintDetailLayers(TerrainData data, Transform terrainTransform, int layerCount,
        List<Vector3> rockPoints, List<Vector3> bushPoints, List<Vector3> treePoints)
    {
        int w = data.detailWidth;
        int h = data.detailHeight;
        Vector3 size = data.size;
        Vector3 terrainOrigin = terrainTransform.position;

        int[][,] layers = new int[layerCount][,];
        for (int l = 0; l < layerCount; l++)
            layers[l] = new int[h, w];

        System.Random rng = new System.Random(2024);

        for (int y = 0; y < h; y++)
        {
            float normZ = (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float normX = (float)x / (w - 1);

                float steepness = data.GetSteepness(normX, normZ);
                if (steepness > MaxSlopeDegrees)
                    continue;

                Vector3 worldPos = terrainOrigin + new Vector3(normX * size.x, 0f, normZ * size.z);

                if (IsNear(worldPos, rockPoints, RockExclusionRadius)) continue;
                if (IsNear(worldPos, bushPoints, BushExclusionRadius)) continue;
                if (IsNear(worldPos, treePoints, TreeExclusionRadius)) continue;

                // Fade density out gradually as slope increases, and add light random variation.
                float slopeFactor = 1f - Mathf.Clamp01(steepness / MaxSlopeDegrees);
                int density = Mathf.RoundToInt(BaseDensity * slopeFactor * (0.7f + 0.6f * (float)rng.NextDouble()));
                if (density <= 0) continue;

                int layer = rng.Next(layerCount);
                layers[layer][y, x] = density;
            }
        }

        for (int l = 0; l < layerCount; l++)
            data.SetDetailLayer(0, 0, l, layers[l]);
    }

    private static bool IsNear(Vector3 point, List<Vector3> others, float radius)
    {
        float sqrRadius = radius * radius;
        foreach (Vector3 o in others)
        {
            Vector3 d = point - o;
            d.y = 0f;
            if (d.sqrMagnitude <= sqrRadius) return true;
        }
        return false;
    }
}
