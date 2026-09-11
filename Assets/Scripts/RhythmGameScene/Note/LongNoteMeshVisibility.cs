using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class LongNoteMeshVisibility : MonoBehaviour
{
    sealed class RendererChunk
    {
        public MeshRenderer Renderer;
        public MeshRenderer StyleSource;
        public float MinDistance;
        public float MaxDistance;
    }

    readonly List<RendererChunk> chunks = new();
    readonly List<Mesh> generatedMeshes = new();
    MaterialPropertyBlock propertyBlock;
    ITimeGetter timer;
    INotePositionCalculator positionCalculator;
    float noteSpeed;
    float originDistance;
    float visibleBehindDistance;
    float visibleAheadDistance;
    bool isExternallyManaged;

    static readonly int TrackClipEnabledId = Shader.PropertyToID("_TrackClipEnabled");
    static readonly int TrackVisibleMinId = Shader.PropertyToID("_TrackVisibleMin");
    static readonly int TrackVisibleMaxId = Shader.PropertyToID("_TrackVisibleMax");

    public static void Attach(
        Component note,
        float originDistance,
        float curveRadius,
        ITimeGetter timer,
        INotePositionCalculator positionCalculator,
        float noteSpeed)
    {
        if (note == null) { return; }

        LongNoteMeshVisibility visibility = note.GetComponent<LongNoteMeshVisibility>();
        if (visibility == null)
        {
            visibility = note.gameObject.AddComponent<LongNoteMeshVisibility>();
        }

        visibility.Initialize(originDistance, curveRadius, timer, positionCalculator, noteSpeed);
    }

    public void SetVisibleRange(float minDistance, float maxDistance)
    {
        isExternallyManaged = true;
        ApplyVisibleRange(minDistance, maxDistance);
    }

    void ApplyVisibleRange(float minDistance, float maxDistance)
    {
        foreach (RendererChunk chunk in chunks)
        {
            if (chunk.Renderer == null) { continue; }

            if (chunk.StyleSource != null && chunk.Renderer != chunk.StyleSource &&
                chunk.Renderer.sharedMaterial != chunk.StyleSource.sharedMaterial)
            {
                chunk.Renderer.sharedMaterial = chunk.StyleSource.sharedMaterial;
            }

            chunk.Renderer.enabled =
                chunk.MaxDistance >= minDistance &&
                chunk.MinDistance <= maxDistance;

            chunk.Renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(TrackClipEnabledId, 1f);
            propertyBlock.SetFloat(TrackVisibleMinId, minDistance - originDistance);
            propertyBlock.SetFloat(TrackVisibleMaxId, maxDistance - originDistance);
            chunk.Renderer.SetPropertyBlock(propertyBlock);
        }
    }

    void Initialize(
        float originDistance,
        float curveRadius,
        ITimeGetter timer,
        INotePositionCalculator positionCalculator,
        float noteSpeed)
    {
        chunks.Clear();
        this.timer = timer;
        this.positionCalculator = positionCalculator;
        this.noteSpeed = noteSpeed;
        this.originDistance = originDistance;
        isExternallyManaged = false;
        propertyBlock ??= new MaterialPropertyBlock();

        float circumference = Mathf.Max(2f * Mathf.PI * curveRadius, 0.01f);
        visibleBehindDistance = Mathf.Max(0f, Mathf.Min(5f, circumference - 0.01f));
        visibleAheadDistance = Mathf.Max(
            0f,
            Mathf.Min(100f, circumference - visibleBehindDistance - 0.01f));
        float chunkLength = Mathf.Clamp(circumference * 0.125f, 10f, 100f);
        MeshRenderer[] sourceRenderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer sourceRenderer in sourceRenderers)
        {
            SplitRendererIfNeeded(sourceRenderer, originDistance, chunkLength);
        }

        ApplyAutomaticVisibleRange();
    }

    void LateUpdate()
    {
        if (!isExternallyManaged)
        {
            ApplyAutomaticVisibleRange();
        }
    }

    void ApplyAutomaticVisibleRange()
    {
        if (timer == null || positionCalculator == null) { return; }

        float currentDistance = positionCalculator.GetPosition(timer.Time) * noteSpeed;
        ApplyVisibleRange(
            currentDistance - visibleBehindDistance,
            currentDistance + visibleAheadDistance);
    }

    void SplitRendererIfNeeded(
        MeshRenderer sourceRenderer,
        float originDistance,
        float chunkLength)
    {
        if (sourceRenderer == null || !sourceRenderer.TryGetComponent(out MeshFilter sourceFilter)) { return; }

        Mesh sourceMesh = sourceFilter.sharedMesh;
        if (sourceMesh == null || sourceMesh.vertexCount == 0) { return; }

        List<Vector4> trackCoordinates = new();
        sourceMesh.GetUVs(1, trackCoordinates);
        if (trackCoordinates.Count != sourceMesh.vertexCount) { return; }

        float minTrackDistance = trackCoordinates.Min(x => x.x);
        float maxTrackDistance = trackCoordinates.Max(x => x.x);
        if (maxTrackDistance - minTrackDistance <= chunkLength)
        {
            chunks.Add(new RendererChunk
            {
                Renderer = sourceRenderer,
                StyleSource = sourceRenderer,
                MinDistance = originDistance + minTrackDistance,
                MaxDistance = originDistance + maxTrackDistance
            });
            return;
        }

        int[] sourceTriangles = sourceMesh.triangles;
        Dictionary<int, List<int>> trianglesByChunk = new();
        for (int i = 0; i + 2 < sourceTriangles.Length; i += 3)
        {
            int a = sourceTriangles[i];
            int b = sourceTriangles[i + 1];
            int c = sourceTriangles[i + 2];
            float centerDistance = (trackCoordinates[a].x + trackCoordinates[b].x + trackCoordinates[c].x) / 3f;
            int chunkIndex = Mathf.FloorToInt((centerDistance - minTrackDistance) / chunkLength);

            if (!trianglesByChunk.TryGetValue(chunkIndex, out List<int> triangles))
            {
                triangles = new List<int>();
                trianglesByChunk.Add(chunkIndex, triangles);
            }

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        if (trianglesByChunk.Count == 0) { return; }

        bool isFirstChunk = true;
        foreach (KeyValuePair<int, List<int>> pair in trianglesByChunk.OrderBy(x => x.Key))
        {
            Mesh chunkMesh = CreateChunkMesh(sourceMesh, pair.Value);
            generatedMeshes.Add(chunkMesh);

            MeshRenderer chunkRenderer;
            if (isFirstChunk)
            {
                sourceFilter.sharedMesh = chunkMesh;
                chunkRenderer = sourceRenderer;
                isFirstChunk = false;
            }
            else
            {
                chunkRenderer = CreateChunkRenderer(sourceRenderer, chunkMesh, pair.Key);
            }

            GetDistanceRange(trackCoordinates, pair.Value, originDistance, out float minDistance, out float maxDistance);
            chunks.Add(new RendererChunk
            {
                Renderer = chunkRenderer,
                StyleSource = sourceRenderer,
                MinDistance = minDistance,
                MaxDistance = maxDistance
            });
        }

        Destroy(sourceMesh);
    }

    MeshRenderer CreateChunkRenderer(MeshRenderer source, Mesh mesh, int chunkIndex)
    {
        GameObject chunkObject = new GameObject($"{source.gameObject.name}_Chunk{chunkIndex}");
        chunkObject.layer = source.gameObject.layer;
        chunkObject.transform.SetParent(source.transform, false);

        MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = source.sharedMaterials;
        renderer.shadowCastingMode = source.shadowCastingMode;
        renderer.receiveShadows = source.receiveShadows;
        renderer.lightProbeUsage = source.lightProbeUsage;
        renderer.reflectionProbeUsage = source.reflectionProbeUsage;
        renderer.probeAnchor = source.probeAnchor;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;

        source.GetPropertyBlock(propertyBlock);
        renderer.SetPropertyBlock(propertyBlock);
        return renderer;
    }

    static Mesh CreateChunkMesh(Mesh source, List<int> sourceTriangles)
    {
        Vector3[] sourceVertices = source.vertices;
        Vector3[] sourceNormals = source.normals;
        Vector4[] sourceTangents = source.tangents;
        Color32[] sourceColors = source.colors32;
        List<Vector4>[] sourceUvs = new List<Vector4>[8];
        for (int channel = 0; channel < sourceUvs.Length; channel++)
        {
            sourceUvs[channel] = new List<Vector4>();
            source.GetUVs(channel, sourceUvs[channel]);
        }

        Dictionary<int, int> indexMap = new();
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector4> tangents = new();
        List<Color32> colors = new();
        List<Vector4>[] uvs = Enumerable.Range(0, 8).Select(_ => new List<Vector4>()).ToArray();
        List<int> triangles = new(sourceTriangles.Count);

        foreach (int sourceIndex in sourceTriangles)
        {
            if (!indexMap.TryGetValue(sourceIndex, out int chunkIndex))
            {
                chunkIndex = vertices.Count;
                indexMap.Add(sourceIndex, chunkIndex);
                vertices.Add(sourceVertices[sourceIndex]);

                if (sourceNormals.Length == sourceVertices.Length) { normals.Add(sourceNormals[sourceIndex]); }
                if (sourceTangents.Length == sourceVertices.Length) { tangents.Add(sourceTangents[sourceIndex]); }
                if (sourceColors.Length == sourceVertices.Length) { colors.Add(sourceColors[sourceIndex]); }

                for (int channel = 0; channel < sourceUvs.Length; channel++)
                {
                    if (sourceUvs[channel].Count == sourceVertices.Length)
                    {
                        uvs[channel].Add(sourceUvs[channel][sourceIndex]);
                    }
                }
            }

            triangles.Add(chunkIndex);
        }

        Mesh mesh = new Mesh
        {
            name = $"{source.name}_Chunk",
            indexFormat = vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        if (normals.Count == vertices.Count) { mesh.SetNormals(normals); }
        else { mesh.RecalculateNormals(); }
        if (tangents.Count == vertices.Count) { mesh.SetTangents(tangents); }
        if (colors.Count == vertices.Count) { mesh.SetColors(colors); }

        for (int channel = 0; channel < uvs.Length; channel++)
        {
            if (uvs[channel].Count == vertices.Count)
            {
                mesh.SetUVs(channel, uvs[channel]);
            }
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    static void GetDistanceRange(
        List<Vector4> trackCoordinates,
        List<int> triangles,
        float originDistance,
        out float minDistance,
        out float maxDistance)
    {
        minDistance = float.PositiveInfinity;
        maxDistance = float.NegativeInfinity;

        foreach (int index in triangles)
        {
            float distance = originDistance + trackCoordinates[index].x;
            minDistance = Mathf.Min(minDistance, distance);
            maxDistance = Mathf.Max(maxDistance, distance);
        }
    }

    void OnDestroy()
    {
        foreach (Mesh mesh in generatedMeshes)
        {
            if (mesh != null) { Destroy(mesh); }
        }
    }
}
