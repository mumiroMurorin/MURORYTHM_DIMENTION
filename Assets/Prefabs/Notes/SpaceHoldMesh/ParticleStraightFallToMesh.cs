using System.Collections.Generic;
using UnityEngine;

public class ParticleStraightFallToMesh : MonoBehaviour
{
    enum TargetMode
    {
        Vertex,
        Surface
    }

    [Header("References")]
    [SerializeField] ParticleSystem targetParticleSystem;
    [SerializeField] MeshRenderer targetMeshRenderer;

    [Header("Spawn")]
    [SerializeField] TargetMode targetMode = TargetMode.Surface;
    [SerializeField] int surfaceSampleCount = 128;
    [SerializeField] float spawnDistance = 5f;
    [SerializeField] bool placeParticlesOnEnable = true;
    [SerializeField] bool placeOnlyNewParticles = true;
    [SerializeField] bool rebuildTargetsEveryFrame = true;

    [Header("Move")]
    [SerializeField] bool moveOnUpdate = true;
    [SerializeField] float fallSpeed = 10f;
    [SerializeField] float arriveDistance = 0.05f;
    [SerializeField] bool killOnArrive = true;
    [SerializeField] bool stickOnArrive = true;

    ParticleSystem.Particle[] particles;
    readonly List<Vector3> localTargetPoints = new List<Vector3>();
    readonly Dictionary<uint, Vector3> seedToLocalTarget = new Dictionary<uint, Vector3>();
    readonly HashSet<uint> aliveSeeds = new HashSet<uint>();
    readonly List<uint> removeSeedBuffer = new List<uint>();

    MeshFilter targetMeshFilter;
    Mesh cachedMesh;
    int cachedSurfaceSampleCount = -1;

    void Awake()
    {
        if (targetParticleSystem == null)
        {
            targetParticleSystem = GetComponent<ParticleSystem>();
        }

        ResolveTargetMeshFilter();
        RebuildTargetPointsIfNeeded(true);
    }

    void OnEnable()
    {
        if (placeParticlesOnEnable)
        {
            PlaceParticles();
        }
    }

    void LateUpdate()
    {
        if (!moveOnUpdate) { return; }

        MoveParticles();
    }

    public void SetTargetMeshRenderer(MeshRenderer meshRenderer)
    {
        targetMeshRenderer = meshRenderer;
        ResolveTargetMeshFilter();
        RebuildTargetPointsIfNeeded(true);
        seedToLocalTarget.Clear();
    }

    public void SetParticleSystem(ParticleSystem particleSystem)
    {
        targetParticleSystem = particleSystem;
        seedToLocalTarget.Clear();
    }

    public void PlaceParticles()
    {
        if (!CanProcess()) { return; }

        RebuildTargetPointsIfNeeded(rebuildTargetsEveryFrame);
        if (localTargetPoints.Count == 0) { return; }

        int particleCount = GetParticles();
        ParticleSystem.MainModule main = targetParticleSystem.main;
        aliveSeeds.Clear();

        for (int i = 0; i < particleCount; i++)
        {
            uint seed = particles[i].randomSeed;
            aliveSeeds.Add(seed);

            if (placeOnlyNewParticles && seedToLocalTarget.ContainsKey(seed))
            {
                continue;
            }

            Vector3 localTarget = GetTargetLocalPosition(i, seed);
            seedToLocalTarget[seed] = localTarget;

            Vector3 localSpawn = localTarget;
            localSpawn.z += spawnDistance;
            particles[i].position = LocalMeshPositionToParticleSimulationPosition(localSpawn, main);
        }

        RemoveDeadSeeds();
        targetParticleSystem.SetParticles(particles, particleCount);
    }

    public void MoveParticles()
    {
        if (!CanProcess()) { return; }

        RebuildTargetPointsIfNeeded(rebuildTargetsEveryFrame);
        if (localTargetPoints.Count == 0) { return; }

        int particleCount = GetParticles();
        ParticleSystem.MainModule main = targetParticleSystem.main;
        aliveSeeds.Clear();

        for (int i = 0; i < particleCount; i++)
        {
            uint seed = particles[i].randomSeed;
            aliveSeeds.Add(seed);

            if (!seedToLocalTarget.TryGetValue(seed, out Vector3 localTarget))
            {
                localTarget = GetTargetLocalPosition(i, seed);
                seedToLocalTarget[seed] = localTarget;

                Vector3 localSpawn = localTarget;
                localSpawn.z += spawnDistance;
                particles[i].position = LocalMeshPositionToParticleSimulationPosition(localSpawn, main);
            }

            Vector3 localPosition = ParticleSimulationPositionToLocalMeshPosition(particles[i].position, main);
            localPosition.x = localTarget.x;
            localPosition.y = localTarget.y;
            localPosition.z = Mathf.MoveTowards(localPosition.z, localTarget.z, fallSpeed * Time.deltaTime);

            bool arrived = Mathf.Abs(localPosition.z - localTarget.z) <= arriveDistance;
            if (arrived)
            {
                localPosition.z = localTarget.z;

                if (killOnArrive)
                {
                    particles[i].remainingLifetime = 0f;
                }
            }

            if (!stickOnArrive || !arrived)
            {
                particles[i].position = LocalMeshPositionToParticleSimulationPosition(localPosition, main);
            }
        }

        RemoveDeadSeeds();
        targetParticleSystem.SetParticles(particles, particleCount);
    }

    bool CanProcess()
    {
        if (targetParticleSystem == null) { return false; }
        if (targetMeshRenderer == null) { return false; }

        ResolveTargetMeshFilter();
        return targetMeshFilter != null;
    }

    int GetParticles()
    {
        int maxParticles = targetParticleSystem.main.maxParticles;
        if (particles == null || particles.Length < maxParticles)
        {
            particles = new ParticleSystem.Particle[maxParticles];
        }

        return targetParticleSystem.GetParticles(particles);
    }

    void ResolveTargetMeshFilter()
    {
        targetMeshFilter = targetMeshRenderer != null
            ? targetMeshRenderer.GetComponent<MeshFilter>()
            : null;
    }

    void RebuildTargetPointsIfNeeded(bool force)
    {
        if (targetMeshFilter == null) { return; }

        Mesh mesh = targetMeshFilter.sharedMesh;
        if (mesh == null) { return; }

        bool shouldRebuild =
            force ||
            cachedMesh != mesh ||
            cachedSurfaceSampleCount != surfaceSampleCount ||
            localTargetPoints.Count == 0;

        if (!shouldRebuild) { return; }

        cachedMesh = mesh;
        cachedSurfaceSampleCount = surfaceSampleCount;
        BuildLocalTargetPoints(mesh);
    }

    void BuildLocalTargetPoints(Mesh mesh)
    {
        localTargetPoints.Clear();

        if (targetMode == TargetMode.Vertex)
        {
            localTargetPoints.AddRange(mesh.vertices);
            return;
        }

        BuildSurfaceTargetPoints(mesh);
    }

    void BuildSurfaceTargetPoints(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        if (vertices == null || vertices.Length == 0) { return; }
        if (triangles == null || triangles.Length < 3)
        {
            localTargetPoints.AddRange(vertices);
            return;
        }

        int triangleCount = triangles.Length / 3;
        int sampleCount = Mathf.Max(1, surfaceSampleCount);

        for (int i = 0; i < sampleCount; i++)
        {
            int triangleIndex = Mathf.FloorToInt((i / (float)sampleCount) * triangleCount) * 3;
            triangleIndex = Mathf.Clamp(triangleIndex, 0, triangles.Length - 3);

            Vector3 a = vertices[triangles[triangleIndex]];
            Vector3 b = vertices[triangles[triangleIndex + 1]];
            Vector3 c = vertices[triangles[triangleIndex + 2]];

            float u = Hash01((uint)(i * 92837111 + 1));
            float v = Hash01((uint)(i * 689287499 + 7));

            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }

            localTargetPoints.Add(a + (b - a) * u + (c - a) * v);
        }
    }

    Vector3 GetTargetLocalPosition(int particleIndex, uint seed)
    {
        int index = Mathf.Abs((int)(seed + (uint)particleIndex)) % localTargetPoints.Count;
        return localTargetPoints[index];
    }

    Vector3 ParticleSimulationPositionToLocalMeshPosition(Vector3 particlePosition, ParticleSystem.MainModule main)
    {
        Vector3 worldPosition = main.simulationSpace switch
        {
            ParticleSystemSimulationSpace.World => particlePosition,
            ParticleSystemSimulationSpace.Custom when main.customSimulationSpace != null => main.customSimulationSpace.TransformPoint(particlePosition),
            _ => targetParticleSystem.transform.TransformPoint(particlePosition)
        };

        return targetMeshRenderer.transform.InverseTransformPoint(worldPosition);
    }

    Vector3 LocalMeshPositionToParticleSimulationPosition(Vector3 localMeshPosition, ParticleSystem.MainModule main)
    {
        Vector3 worldPosition = targetMeshRenderer.transform.TransformPoint(localMeshPosition);

        return main.simulationSpace switch
        {
            ParticleSystemSimulationSpace.World => worldPosition,
            ParticleSystemSimulationSpace.Custom when main.customSimulationSpace != null => main.customSimulationSpace.InverseTransformPoint(worldPosition),
            _ => targetParticleSystem.transform.InverseTransformPoint(worldPosition)
        };
    }

    void RemoveDeadSeeds()
    {
        removeSeedBuffer.Clear();

        foreach (uint seed in seedToLocalTarget.Keys)
        {
            if (!aliveSeeds.Contains(seed))
            {
                removeSeedBuffer.Add(seed);
            }
        }

        for (int i = 0; i < removeSeedBuffer.Count; i++)
        {
            seedToLocalTarget.Remove(removeSeedBuffer[i]);
        }
    }

    static float Hash01(uint x)
    {
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        x *= 0x846ca68b;
        x ^= x >> 16;
        return (x & 0x00ffffff) / 16777215f;
    }
}
