using UnityEditor;
using UnityEngine;

public static class TerrainShapeSculptor
{
    private const string PrefabPath = "Assets/Prefabs/Stages/Creation_HIll/Stage_Creation_HIll.prefab";
    private const string TerrainObjectName = "Terrain_5";

    // Bell-curve spread as a fraction of the terrain's half-extent (0-0.5, smaller = narrower dome).
    private const float Sigma = 0.28f;
    // Fine surface roughness, as a fraction of the peak height, so the dome doesn't look mathematically perfect.
    private const float NoiseAmplitudeFraction = 0.06f;
    private const float NoiseScale = 0.08f;

    [MenuItem("Tools/Terrain Bake/Reshape Terrain_5 As Natural Hill (Prefab)")]
    public static void ReshapeAsNaturalHill()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform terrainTransform = FindDeep(root.transform, TerrainObjectName);
            if (terrainTransform == null)
            {
                Debug.LogError($"[TerrainShapeSculptor] '{TerrainObjectName}' was not found inside {PrefabPath}.");
                return;
            }

            Terrain terrain = terrainTransform.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError($"[TerrainShapeSculptor] '{TerrainObjectName}' has no Terrain component or TerrainData.");
                return;
            }

            ApplyGaussianHill(terrain);
            int snapped = SnapPropsToSurface(terrain, root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TerrainShapeSculptor] Terrain_5 (prefab) reshaped into a natural bell-curve hill. " +
                      $"Re-snapped {snapped} prop(s) to the new surface. " +
                      "Re-run the grass painter to match the grass to the new shape.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>Reshapes the given Terrain's heightmap into a natural bell-curve dome, in place.</summary>
    public static void ApplyGaussianHill(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        int res = data.heightmapResolution;
        float[,] oldHeights = data.GetHeights(0, 0, res, res);

        float peak = 0f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                if (oldHeights[y, x] > peak) peak = oldHeights[y, x];
        if (peak <= 0f) peak = 0.4f; // sane fallback if the source heightmap was flat

        float noiseOffsetX = 1000f;
        float noiseOffsetY = 2000f;

        float[,] newHeights = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            float ny = (float)y / (res - 1) - 0.5f;
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1) - 0.5f;
                float dist2 = nx * nx + ny * ny;
                float gaussian = Mathf.Exp(-dist2 / (2f * Sigma * Sigma));

                float noise = FractalNoise(x * NoiseScale + noiseOffsetX, y * NoiseScale + noiseOffsetY, 3);
                float h = gaussian * peak + noise * peak * NoiseAmplitudeFraction * gaussian;

                newHeights[y, x] = Mathf.Clamp01(h);
            }
        }

        data.SetHeights(0, 0, newHeights);
    }

    /// <summary>Re-snaps every direct child of the Rocks/Bushes/Trees groups under searchRoot onto the terrain surface. Returns how many were moved.</summary>
    public static int SnapPropsToSurface(Terrain terrain, Transform searchRoot)
    {
        int snapped = SnapGroupToSurface(terrain, searchRoot, "Rocks");
        snapped += SnapGroupToSurface(terrain, searchRoot, "Bushes");
        snapped += SnapGroupToSurface(terrain, searchRoot, "Trees");
        return snapped;
    }

    private static float FractalNoise(float x, float y, int octaves)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float totalAmplitude = 0f;
        for (int i = 0; i < octaves; i++)
        {
            value += (Mathf.PerlinNoise(x * frequency, y * frequency) * 2f - 1f) * amplitude;
            totalAmplitude += amplitude;
            amplitude *= 0.5f;
            frequency *= 2.3f;
        }
        return value / totalAmplitude;
    }

    private static int SnapGroupToSurface(Terrain terrain, Transform root, string groupName)
    {
        Transform group = FindDeep(root, groupName);
        if (group == null) return 0;

        int count = 0;
        for (int i = 0; i < group.childCount; i++)
        {
            Transform child = group.GetChild(i);
            Vector3 pos = child.position;
            float surfaceY = terrain.SampleHeight(pos) + terrain.transform.position.y;
            child.position = new Vector3(pos.x, surfaceY, pos.z);
            count++;
        }
        return count;
    }

    public static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
