using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Blends the shared border between two adjacent Terrain tiles in the currently open scene so there is
/// no visible seam/cliff where they meet - both sides are eased toward a shared height profile along
/// the border and fade back to their own original shape a short distance away from it.
/// </summary>
public static class TerrainEdgeBlender
{
    private const string TerrainAName = "Terrain_5";
    private const string TerrainBName = "Terrain_(-200.00, -24.60, -251.00)";
    private const string OutputDir = "Assets/Prefabs/Stages/Creation_HIll/BakedTerrain";
    private const string SceneTerrainADataPath = OutputDir + "/Terrain_5_Scene_TerrainData.asset";
    private const string SceneTerrainBDataPath = OutputDir + "/Terrain_Neighbor_TerrainData.asset";

    private const float BlendDistance = 40f; // world units, how far into each terrain the seam fix reaches
    private const int BorderSampleCount = 257;

    [MenuItem("Tools/Terrain Bake/Scene - Blend Terrain_5 With Neighbor")]
    public static void BlendNeighbors()
    {
        GameObject aGO = FindInActiveScene(TerrainAName);
        GameObject bGO = FindInActiveScene(TerrainBName);
        if (aGO == null || bGO == null)
        {
            Debug.LogError($"[TerrainEdgeBlender] Could not find both terrains in the active scene. " +
                            $"A='{TerrainAName}' found={aGO != null}, B='{TerrainBName}' found={bGO != null}.");
            return;
        }

        Terrain terrainA = aGO.GetComponent<Terrain>();
        Terrain terrainB = bGO.GetComponent<Terrain>();
        TerrainCollider colA = aGO.GetComponent<TerrainCollider>();
        TerrainCollider colB = bGO.GetComponent<TerrainCollider>();
        if (terrainA == null || terrainB == null)
        {
            Debug.LogError("[TerrainEdgeBlender] One of the objects has no Terrain component.");
            return;
        }

        TerrainData dataA = TerrainGrassPainter.EnsureDedicatedTerrainData(terrainA, colA, SceneTerrainADataPath, "Terrain_5_Scene_TerrainData");
        TerrainData dataB = TerrainGrassPainter.EnsureDedicatedTerrainData(terrainB, colB, SceneTerrainBDataPath, "Terrain_Neighbor_TerrainData");

        Vector3 posA = terrainA.transform.position;
        Vector3 posB = terrainB.transform.position;
        Vector3 sizeA = dataA.size;
        Vector3 sizeB = dataB.size;

        float gapZ_AminBmax = Mathf.Abs(posA.z - (posB.z + sizeB.z));
        float gapZ_BminAmax = Mathf.Abs(posB.z - (posA.z + sizeA.z));
        float gapX_AminBmax = Mathf.Abs(posA.x - (posB.x + sizeB.x));
        float gapX_BminAmax = Mathf.Abs(posB.x - (posA.x + sizeA.x));

        float minGap = Mathf.Min(Mathf.Min(gapZ_AminBmax, gapZ_BminAmax), Mathf.Min(gapX_AminBmax, gapX_BminAmax));
        if (minGap > 5f)
            Debug.LogWarning($"[TerrainEdgeBlender] The two terrains don't appear to touch exactly (closest gap ~{minGap:F1}m). Blending across the nearest edge anyway.");

        bool axisIsZ = minGap == gapZ_AminBmax || minGap == gapZ_BminAmax;
        int axis = axisIsZ ? 2 : 0;
        int perpAxis = axisIsZ ? 0 : 2;

        bool aIsLowSide = axisIsZ ? (gapZ_AminBmax <= gapZ_BminAmax) : (gapX_AminBmax <= gapX_BminAmax);
        float borderWorld = aIsLowSide ? posA[axis] : posA[axis] + sizeA[axis];

        float perpMin = Mathf.Max(posA[perpAxis], posB[perpAxis]);
        float perpMax = Mathf.Min(posA[perpAxis] + sizeA[perpAxis], posB[perpAxis] + sizeB[perpAxis]);
        if (perpMax <= perpMin)
        {
            Debug.LogError("[TerrainEdgeBlender] The terrains don't overlap along the perpendicular axis - can't find a shared border.");
            return;
        }

        // Sample the average of both terrains' current edge heights along the border.
        float[] targetHeights = new float[BorderSampleCount];
        for (int i = 0; i < BorderSampleCount; i++)
        {
            float t = (float)i / (BorderSampleCount - 1);
            float perpWorld = Mathf.Lerp(perpMin, perpMax, t);

            float hA = SampleWorldHeight(dataA, posA, sizeA, axis, perpAxis, borderWorld, perpWorld);
            float hB = SampleWorldHeight(dataB, posB, sizeB, axis, perpAxis, borderWorld, perpWorld);
            targetHeights[i] = (hA + hB) * 0.5f;
        }

        BlendTerrainToBorder(dataA, posA, sizeA, axis, perpAxis, borderWorld, aIsLowSide, perpMin, perpMax, targetHeights);
        BlendTerrainToBorder(dataB, posB, sizeB, axis, perpAxis, borderWorld, !aIsLowSide, perpMin, perpMax, targetHeights);

        EditorUtility.SetDirty(aGO);
        EditorUtility.SetDirty(bGO);
        Scene scene = aGO.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TerrainEdgeBlender] Blended '{TerrainAName}' and '{TerrainBName}' along their shared " +
                  $"{(axisIsZ ? "Z" : "X")}-axis edge (blend distance {BlendDistance}m each side). Scene saved.");
    }

    private static float SampleWorldHeight(TerrainData data, Vector3 pos, Vector3 size, int axis, int perpAxis, float axisWorld, float perpWorld)
    {
        float normAxis = Mathf.Clamp01((axisWorld - pos[axis]) / size[axis]);
        float normPerp = Mathf.Clamp01((perpWorld - pos[perpAxis]) / size[perpAxis]);
        // TerrainData.GetInterpolatedHeight takes (x, z) normalized coordinates in that order.
        float nx = axis == 0 ? normAxis : normPerp;
        float nz = axis == 2 ? normAxis : normPerp;
        return data.GetInterpolatedHeight(nx, nz);
    }

    private static void BlendTerrainToBorder(TerrainData data, Vector3 pos, Vector3 size, int axis, int perpAxis,
        float borderWorld, bool isLowSide, float perpMin, float perpMax, float[] targetHeights)
    {
        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);

        for (int y = 0; y < res; y++)
        {
            float nz = (float)y / (res - 1);
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);

                float normAxis = axis == 0 ? nx : nz;
                float normPerp = axis == 0 ? nz : nx;

                float axisWorld = pos[axis] + normAxis * size[axis];
                float perpWorld = pos[perpAxis] + normPerp * size[perpAxis];

                float distFromBorder = isLowSide ? (axisWorld - borderWorld) : (borderWorld - axisWorld);
                if (distFromBorder < 0f || distFromBorder > BlendDistance)
                    continue; // outside the terrain on the wrong side, or too far from the seam to matter

                if (perpWorld < perpMin || perpWorld > perpMax)
                    continue; // outside the overlapping range with the neighbor

                float t = Mathf.SmoothStep(0f, 1f, distFromBorder / BlendDistance);

                float perpT = Mathf.InverseLerp(perpMin, perpMax, perpWorld);
                float targetWorldHeight = SampleArray(targetHeights, perpT);

                float originalWorldHeight = heights[y, x] * size.y;
                float blendedWorldHeight = Mathf.Lerp(targetWorldHeight, originalWorldHeight, t);

                heights[y, x] = Mathf.Clamp01(blendedWorldHeight / size.y);
            }
        }

        data.SetHeights(0, 0, heights);
    }

    private static float SampleArray(float[] arr, float t)
    {
        t = Mathf.Clamp01(t);
        float f = t * (arr.Length - 1);
        int i0 = Mathf.FloorToInt(f);
        int i1 = Mathf.Min(i0 + 1, arr.Length - 1);
        float frac = f - i0;
        return Mathf.Lerp(arr[i0], arr[i1], frac);
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
