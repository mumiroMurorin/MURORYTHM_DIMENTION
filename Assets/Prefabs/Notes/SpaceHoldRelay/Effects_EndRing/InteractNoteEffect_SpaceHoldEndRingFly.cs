using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MeshGenerate;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractNoteEffect_SpaceHoldEndRingFly : MonoBehaviour, IInteractNoteEffectController
{
    [Header("Mesh")]
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Material ringMaterial;
    [SerializeField] float vertexScale = 10f;
    [SerializeField] float lineWidth = 0.1f;
    [SerializeField] Vector3 meshCenter = Vector3.zero;
    [SerializeField] Vector3 meshLocalOffset = Vector3.zero;
    [SerializeField] bool clearMeshOnReturn = false;

    [Header("Trail")]
    [SerializeField] bool enableTrail = true;
    [SerializeField] Material trailMaterial;
    [SerializeField] Gradient trailColorGradient = new Gradient();
    [SerializeField] float trailTime = 0.4f;
    [SerializeField] float trailWidthMultiplier = 1f;
    [SerializeField] float trailEndWidthMultiplier = 1f;
    [SerializeField] int maxTrailSegments = 48;
    [SerializeField] float trailSegmentRotationOffsetX = 0f;
    [SerializeField] Transform trailEmitterParent;

    [Header("Move")]
    [SerializeField] float flySpeed = 80f;
    [SerializeField] float flyDuration = 1f;
    [SerializeField] float defaultCurveRadius = 2000f;
    [SerializeField] bool useNoteCurveRadius = true;
    [SerializeField] bool rotateAlongGround = true;

    [Header("Fade")]
    [SerializeField] bool enableFadeOut = true;
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] string lineColorPropertyName = "_Color";

    [Header("Life Cycle")]
    [SerializeField] bool disableOnStart = true;

    Action<IInteractNoteEffectController> returnToPool;
    CancellationTokenSource moveCancellation;
    INoteData noteData;
    Judgement judgement;
    Vector3 startLocalPosition;
    Quaternion startLocalRotation;
    float curveRadius;
    bool isPlaying;
    readonly List<TrailRenderer> trailRenderers = new List<TrailRenderer>();
    readonly List<TrailSegment> activeTrailSegments = new List<TrailSegment>();
    readonly Dictionary<TrailRenderer, Gradient> fadedTrailGradientCache = new Dictionary<TrailRenderer, Gradient>();
    MaterialPropertyBlock linePropertyBlock;
    Color baseLineColor = Color.white;
    bool hasVisualOverride;
    const float TRAIL_SEGMENT_FIXED_Y = 90f;

    public void SetVisualSettings(Color ringColor, Gradient trailGradient)
    {
        hasVisualOverride = true;
        baseLineColor = ringColor;

        if (trailGradient != null)
        {
            trailColorGradient = trailGradient;
            fadedTrailGradientCache.Clear();
        }

        ApplyFade(1f);

        for (int i = 0; i < trailRenderers.Count; i++)
        {
            if (trailRenderers[i] == null) { continue; }
            trailRenderers[i].colorGradient = trailColorGradient;
        }
    }

    void Start()
    {
        InitializeTrailEmitterPool();
        gameObject.SetActive(!disableOnStart);
        if (meshRenderer) { meshRenderer.material = ringMaterial; }
        CaptureBaseLineColor();
        ApplyFade(1f);
    }

    void OnDisable()
    {
        CancelMove();
    }

    public void SetTransform(Vector3 pos, Quaternion rotation)
    {
        transform.position = pos;
        transform.rotation = rotation;
    }

    public void SetEffect(INoteData noteData, Judgement judgement, Action<IInteractNoteEffectController> returnToPool)
    {
        this.returnToPool = returnToPool;
        this.noteData = noteData;
        this.judgement = judgement;

        if (judgement == Judgement.Miss)
        {
            ReturnToPool();
            return;
        }

        if (noteData is not ISpaceHoldBulletEffectNoteData { IsSpaceHoldEnd: true } spaceHoldEndData)
        {
            ReturnToPool();
            return;
        }

        List<Vector3> ringPoints = BuildRingMesh(spaceHoldEndData.Vertices);
        BuildTrailEmitters(ringPoints);
        curveRadius = GetCurveRadius(noteData);
    }

    public void Play()
    {
        if (noteData == null || judgement == Judgement.Miss)
        {
            ReturnToPool();
            return;
        }

        gameObject.SetActive(true);
        ClearTrails();
        ApplyFade(1f);

        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        CancelMove();
        moveCancellation = new CancellationTokenSource();
        FlyAsync(moveCancellation.Token).Forget();
    }

    async UniTaskVoid FlyAsync(CancellationToken token)
    {
        isPlaying = true;
        float elapsed = 0f;

        try
        {
            while (elapsed < flyDuration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float distance = flySpeed * elapsed;
                transform.localPosition = startLocalPosition + NoteTrackCurve.GetPosition(distance, curveRadius);

                if (rotateAlongGround)
                {
                    transform.localRotation = startLocalRotation * NoteTrackCurve.GetRotation(distance, curveRadius);
                }

                float normalizedTime = flyDuration > 0f ? Mathf.Clamp01(elapsed / flyDuration) : 1f;
                float alpha = enableFadeOut ? Mathf.Clamp01(fadeCurve.Evaluate(normalizedTime)) : 1f;
                ApplyFade(alpha);
                ApplyTrailSegmentTransforms();

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            isPlaying = false;
        }

        ReturnToPool();
    }

    List<Vector3> BuildRingMesh(Vector2[] vertices)
    {
        if (meshFilter == null) { return new List<Vector3>(); }

        if (vertices == null || vertices.Length < 2)
        {
            meshFilter.mesh = null;
            return new List<Vector3>();
        }

        List<Vector3> points = new List<Vector3>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 point = MeshGenerator.Normalize(vertices[i], meshCenter, vertexScale) + meshLocalOffset;
            points.Add(point);
        }

        meshFilter.mesh = MeshGenerator.GenerateLineMesh(points, lineWidth, true);
        CaptureBaseLineColor();
        return points;
    }

    void BuildTrailEmitters(List<Vector3> ringPoints)
    {
        if (!enableTrail)
        {
            SetTrailEmitterCount(0);
            return;
        }
        if (maxTrailSegments <= 0)
        {
            SetTrailEmitterCount(0);
            return;
        }

        List<TrailSegment> segments = BuildTrailSegments(ringPoints);
        activeTrailSegments.Clear();
        activeTrailSegments.AddRange(segments);
        SetTrailEmitterCount(segments.Count);

        ApplyTrailSegmentTransforms();

        for (int i = 0; i < segments.Count; i++)
        {
            TrailRenderer trail = trailRenderers[i];
            TrailSegment segment = segments[i];
            ApplyTrailSettings(trail, segment.Length);
            trail.Clear();
        }
    }

    void ApplyTrailSegmentTransforms()
    {
        int count = Mathf.Min(trailRenderers.Count, activeTrailSegments.Count);

        for (int i = 0; i < count; i++)
        {
            TrailRenderer trail = trailRenderers[i];
            if (trail == null) { continue; }

            TrailSegment segment = activeTrailSegments[i];
            trail.transform.localPosition = segment.Center;
            trail.transform.localEulerAngles = new Vector3(
                segment.AngleX + trailSegmentRotationOffsetX,
                TRAIL_SEGMENT_FIXED_Y,
                0f);
        }
    }

    List<TrailSegment> BuildTrailSegments(List<Vector3> ringPoints)
    {
        List<TrailSegment> result = new List<TrailSegment>();
        if (ringPoints == null || ringPoints.Count == 0) { return result; }
        if (ringPoints.Count < 2) { return result; }
        if (maxTrailSegments <= 0) { return result; }

        for (int i = 0; i < ringPoints.Count; i++)
        {
            Vector3 from = ringPoints[i];
            Vector3 to = ringPoints[(i + 1) % ringPoints.Count];
            Vector3 direction = to - from;
            float length = direction.magnitude;

            if (length <= Mathf.Epsilon)
            {
                continue;
            }

            result.Add(new TrailSegment(
                (from + to) * 0.5f,
                CalculateTrailSegmentAngleX(direction),
                length));

            if (result.Count >= maxTrailSegments)
            {
                return result;
            }
        }

        return result;
    }

    float CalculateTrailSegmentAngleX(Vector3 direction)
    {
        float angleOnXY = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return 90f - angleOnXY;
    }

    void SetTrailEmitterCount(int count)
    {
        if (enableTrail)
        {
            InitializeTrailEmitterPool();
        }

        for (int i = 0; i < trailRenderers.Count; i++)
        {
            if (trailRenderers[i] == null) { continue; }
            trailRenderers[i].gameObject.SetActive(i < count);
        }
    }

    void InitializeTrailEmitterPool()
    {
        int targetCount = Mathf.Max(0, maxTrailSegments);
        if (trailRenderers.Count >= targetCount) { return; }

        Transform emitterParent = GetTrailEmitterParent();

        while (trailRenderers.Count < targetCount)
        {
            GameObject emitter = new GameObject($"TrailEmitter_{trailRenderers.Count:00}");
            emitter.transform.SetParent(emitterParent, false);
            TrailRenderer trail = emitter.AddComponent<TrailRenderer>();
            ApplyTrailSettings(trail, 0f);
            trail.gameObject.SetActive(false);
            trailRenderers.Add(trail);
        }
    }

    Transform GetTrailEmitterParent()
    {
        if (trailEmitterParent != null) { return trailEmitterParent; }
        return transform;
    }

    void ApplyTrailSettings(TrailRenderer trail, float segmentLength)
    {
        if (trail == null) { return; }

        trail.time = Mathf.Max(0.01f, trailTime);
        trail.startWidth = Mathf.Max(0f, segmentLength * trailWidthMultiplier);
        trail.endWidth = Mathf.Max(0f, segmentLength * trailEndWidthMultiplier);
        trail.colorGradient = trailColorGradient;
        trail.alignment = LineAlignment.TransformZ;
        trail.emitting = true;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;

        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
    }

    void ApplyFade(float alpha)
    {
        ApplyLineFade(alpha);
        ApplyTrailFade(alpha);
    }

    void ApplyLineFade(float alpha)
    {
        if (meshRenderer == null) { return; }
        if (linePropertyBlock == null)
        {
            linePropertyBlock = new MaterialPropertyBlock();
        }

        meshRenderer.GetPropertyBlock(linePropertyBlock);
        Color color = baseLineColor;
        color.a *= alpha;
        linePropertyBlock.SetColor(lineColorPropertyName, color);
        meshRenderer.SetPropertyBlock(linePropertyBlock);
    }

    void ApplyTrailFade(float alpha)
    {
        for (int i = 0; i < trailRenderers.Count; i++)
        {
            TrailRenderer trail = trailRenderers[i];
            if (trail == null) { continue; }

            trail.colorGradient = GetFadedTrailGradient(trail, alpha);
        }
    }

    Gradient GetFadedTrailGradient(TrailRenderer trail, float alpha)
    {
        if (!fadedTrailGradientCache.TryGetValue(trail, out Gradient gradient) || gradient == null)
        {
            gradient = new Gradient();
            fadedTrailGradientCache[trail] = gradient;
        }

        GradientColorKey[] colorKeys = trailColorGradient.colorKeys;
        GradientAlphaKey[] sourceAlphaKeys = trailColorGradient.alphaKeys;
        if (colorKeys == null || colorKeys.Length == 0)
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
        }
        if (sourceAlphaKeys == null || sourceAlphaKeys.Length == 0)
        {
            sourceAlphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
        }

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[sourceAlphaKeys.Length];

        for (int i = 0; i < sourceAlphaKeys.Length; i++)
        {
            alphaKeys[i] = new GradientAlphaKey(sourceAlphaKeys[i].alpha * alpha, sourceAlphaKeys[i].time);
        }

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    void CaptureBaseLineColor()
    {
        if (hasVisualOverride) { return; }
        if (meshRenderer == null) { return; }

        Material material = ringMaterial != null ? ringMaterial : meshRenderer.sharedMaterial;
        if (material == null) { return; }
        if (!material.HasProperty(lineColorPropertyName)) { return; }

        baseLineColor = material.GetColor(lineColorPropertyName);
    }

    readonly struct TrailSegment
    {
        public TrailSegment(Vector3 center, float angleX, float length)
        {
            Center = center;
            AngleX = angleX;
            Length = length;
        }

        public Vector3 Center { get; }
        public float AngleX { get; }
        public float Length { get; }
    }

    void ClearTrails()
    {
        for (int i = 0; i < trailRenderers.Count; i++)
        {
            trailRenderers[i]?.Clear();
        }
    }

    float GetCurveRadius(INoteData noteData)
    {
        if (!useNoteCurveRadius) { return defaultCurveRadius; }

        return noteData switch
        {
            NoteData_SpaceHoldRelay relay when relay.OptionGetter?.NoteCurveRadius != null => relay.OptionGetter.NoteCurveRadius.Value,
            NoteData_SpaceHoldRelayHidden hidden when hidden.OptionGetter?.NoteCurveRadius != null => hidden.OptionGetter.NoteCurveRadius.Value,
            _ => defaultCurveRadius
        };
    }

    void ReturnToPool()
    {
        CancelMove();

        if (clearMeshOnReturn && meshFilter != null)
        {
            meshFilter.mesh = null;
        }

        ClearTrails();
        ApplyFade(1f);
        gameObject.SetActive(false);
        returnToPool?.Invoke(this);
    }

    void CancelMove()
    {
        if (moveCancellation == null) { return; }

        if (!moveCancellation.IsCancellationRequested && isPlaying)
        {
            moveCancellation.Cancel();
        }

        moveCancellation.Dispose();
        moveCancellation = null;
    }
}
