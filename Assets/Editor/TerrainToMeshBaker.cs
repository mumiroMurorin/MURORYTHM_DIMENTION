using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TerrainToMeshBaker
{
    private const string PrefabPath = "Assets/Prefabs/Stages/Creation_HIll/Stage_Creation_HIll.prefab";
    private const string TargetName = "Terrain_6";
    private const string OutputDir = "Assets/Prefabs/Stages/Creation_HIll/BakedTerrain";
    private const int MaxMeshResolution = 256; // vertices per side, downsampled from the heightmap
    private const int BakedTextureSize = 2048;

    [MenuItem("Tools/Terrain Bake/Bake Terrain_6 To Mesh")]
    public static void BakeTerrain6()
    {
        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform targetTransform = FindDeep(root.transform, TargetName);
            if (targetTransform == null)
            {
                Debug.LogError($"[TerrainToMeshBaker] '{TargetName}' was not found inside {PrefabPath}.");
                return;
            }

            Terrain terrain = targetTransform.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError($"[TerrainToMeshBaker] '{TargetName}' has no Terrain component or TerrainData.");
                return;
            }

            TerrainData data = terrain.terrainData;

            Mesh mesh = GenerateMeshFromHeightmap(data);
            mesh.name = "Terrain_6_Mesh";
            AssetDatabase.CreateAsset(mesh, $"{OutputDir}/Terrain_6_Mesh.asset");

            Texture2D bakedTexture = BakeAlbedoTexture(terrain, BakedTextureSize);
            string texturePath = $"{OutputDir}/Terrain_6_BakedAlbedo.png";
            File.WriteAllBytes(texturePath, bakedTexture.EncodeToPNG());
            Object.DestroyImmediate(bakedTexture);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            Material material = new Material(Shader.Find("Standard"));
            material.mainTexture = importedTexture;
            material.name = "Terrain_6_BakedMat";
            AssetDatabase.CreateAsset(material, $"{OutputDir}/Terrain_6_BakedMat.mat");

            List<GameObject> spawnedTrees = SpawnTreeInstances(terrain, targetTransform);

            GameObject meshGO = new GameObject("Terrain_6");
            meshGO.transform.SetParent(targetTransform.parent, false);
            meshGO.transform.localPosition = targetTransform.localPosition;
            meshGO.transform.localRotation = targetTransform.localRotation;
            meshGO.transform.localScale = targetTransform.localScale;
            meshGO.transform.SetSiblingIndex(targetTransform.GetSiblingIndex());
            meshGO.layer = targetTransform.gameObject.layer;
            meshGO.tag = targetTransform.gameObject.tag;
            meshGO.isStatic = targetTransform.gameObject.isStatic;

            MeshFilter mf = meshGO.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = meshGO.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            MeshCollider mc = meshGO.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            foreach (GameObject tree in spawnedTrees)
                tree.transform.SetParent(meshGO.transform, true);

            Object.DestroyImmediate(targetTransform.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[TerrainToMeshBaker] Terrain_6 baked to a rotatable Mesh + MeshCollider. " +
                      "You can now freely rotate it like any regular GameObject.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static Mesh GenerateMeshFromHeightmap(TerrainData data)
    {
        int hmRes = data.heightmapResolution;
        int step = Mathf.Max(1, (hmRes - 1) / (MaxMeshResolution - 1));
        Vector3 size = data.size;
        float[,] heights = data.GetHeights(0, 0, hmRes, hmRes);

        int vw = (hmRes - 1) / step + 1;
        int vh = (hmRes - 1) / step + 1;

        Vector3[] vertices = new Vector3[vw * vh];
        Vector2[] uvs = new Vector2[vw * vh];

        for (int z = 0; z < vh; z++)
        {
            int hz = Mathf.Min(z * step, hmRes - 1);
            for (int x = 0; x < vw; x++)
            {
                int hx = Mathf.Min(x * step, hmRes - 1);
                float height = heights[hz, hx] * size.y;
                float px = (float)hx / (hmRes - 1) * size.x;
                float pz = (float)hz / (hmRes - 1) * size.z;
                vertices[z * vw + x] = new Vector3(px, height, pz);
                uvs[z * vw + x] = new Vector2((float)hx / (hmRes - 1), (float)hz / (hmRes - 1));
            }
        }

        int quadCount = (vw - 1) * (vh - 1);
        int[] triangles = new int[quadCount * 6];
        int ti = 0;
        for (int z = 0; z < vh - 1; z++)
        {
            for (int x = 0; x < vw - 1; x++)
            {
                int i0 = z * vw + x;
                int i1 = z * vw + x + 1;
                int i2 = (z + 1) * vw + x;
                int i3 = (z + 1) * vw + x + 1;
                triangles[ti++] = i0; triangles[ti++] = i2; triangles[ti++] = i1;
                triangles[ti++] = i1; triangles[ti++] = i2; triangles[ti++] = i3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Texture2D BakeAlbedoTexture(Terrain terrain, int resolution)
    {
        TerrainData data = terrain.terrainData;
        Vector3 size = data.size;

        GameObject camGO = new GameObject("TerrainBakeCam");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(size.x, size.z) * 0.5f;
        cam.aspect = 1f;
        cam.transform.position = terrain.transform.position + new Vector3(size.x * 0.5f, size.y + 500f, size.z * 0.5f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = size.y + 1000f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.gray;
        cam.renderingPath = RenderingPath.Forward;

        AmbientMode prevAmbientMode = RenderSettings.ambientMode;
        Color prevAmbientLight = RenderSettings.ambientLight;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;

        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        cam.targetTexture = rt;
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        cam.Render();

        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();

        RenderTexture.active = prevActive;
        cam.targetTexture = null;
        RenderSettings.ambientMode = prevAmbientMode;
        RenderSettings.ambientLight = prevAmbientLight;

        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(rt);

        return tex;
    }

    private static List<GameObject> SpawnTreeInstances(Terrain terrain, Transform originalTerrainTransform)
    {
        List<GameObject> spawned = new List<GameObject>();
        TerrainData data = terrain.terrainData;
        TreePrototype[] prototypes = data.treePrototypes;
        TreeInstance[] instances = data.treeInstances;
        if (prototypes == null || prototypes.Length == 0 || instances == null || instances.Length == 0)
            return spawned;

        Vector3 size = data.size;
        Vector3 terrainWorldPos = originalTerrainTransform.position;

        foreach (TreeInstance instance in instances)
        {
            GameObject prefab = prototypes[instance.prototypeIndex].prefab;
            if (prefab == null) continue;

            Vector3 worldPos = terrainWorldPos + Vector3.Scale(instance.position, size);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, instance.rotation * Mathf.Rad2Deg, 0f);
            go.transform.localScale = new Vector3(instance.widthScale, instance.heightScale, instance.widthScale);
            spawned.Add(go);
        }
        return spawned;
    }
}
