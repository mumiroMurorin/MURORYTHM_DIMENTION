using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpaceHoldBulletTrailBuilder : MonoBehaviour
{
    [SerializeField] SpaceHoldBulletInteractEffectController bulletPrefab;
    [SerializeField] Transform bulletParent;
    [SerializeField] Vector3 spawnCenter = Vector3.zero;
    [SerializeField] Vector3 normalizedPositionScale = Vector3.one;
    [SerializeField] Vector3 spawnInterval = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] float minSpawnDistance = 0.01f;
    [SerializeField] float stayThreshold = 0.01f;
    [SerializeField] float minChargeDuration = 0.2f;

    readonly Dictionary<int, List<BulletEntry>> holdNumberToBullets = new Dictionary<int, List<BulletEntry>>();

    public void Build(NoteData_SpaceHoldMesh noteData, Transform fallbackParent)
    {
        if (noteData == null) { return; }
        if (bulletPrefab == null) { return; }
        if (noteData.TimeToVertices == null || noteData.TimeToVertices.Count == 0) { return; }

        SpaceHoldBulletInteractEffectController.ClearWaitingBullets(noteData.HoldNumber);
        ClearBuiltBullets(noteData.HoldNumber);

        Transform parent = bulletParent != null ? bulletParent : transform;
        if (parent == null) { return; }

        List<TimedPathPoint> pathPoints = CreatePathPoints(noteData);
        List<TimedPathPoint> sampledPoints = SampleByDistance(pathPoints);
        List<BulletEntry> bullets = new List<BulletEntry>();
        Vector3? lastSpawnChartPosition = null;
        float fireTiming = pathPoints.Count > 0 ? pathPoints[^1].Timing : noteData.Timing;

        foreach (TimedPathPoint point in sampledPoints)
        {
            if (!point.HasCharge && lastSpawnChartPosition.HasValue && Vector3.Distance(lastSpawnChartPosition.Value, point.ChartPosition) < minSpawnDistance)
            {
                continue;
            }

            SpaceHoldBulletInteractEffectController bullet = Instantiate(bulletPrefab, parent);
            bullet.InitializeWaitingBullet(noteData.HoldNumber, point.LocalPosition);
            bullet.SetWidth(point.Width);
            bullet.SetChargeParticleDuration(fireTiming - point.Timing);
            if (point.HasCharge)
            {
                bullet.SetChargeTiming(point.ChargeStartTiming, point.ChargeEndTiming);
            }
            bullet.gameObject.SetActive(false);
            bullets.Add(new BulletEntry(bullet, point.Timing));
            lastSpawnChartPosition = point.ChartPosition;
        }

        holdNumberToBullets[noteData.HoldNumber] = bullets;
    }

    public void UpdateVisibleBullets(int holdNumber, float currentTime)
    {
        if (!holdNumberToBullets.TryGetValue(holdNumber, out List<BulletEntry> bullets)) { return; }

        foreach (BulletEntry entry in bullets)
        {
            if (entry.IsShown) { continue; }
            if (currentTime < entry.Timing) { continue; }

            entry.Bullet.Play();
            entry.IsShown = true;
            entry.Bullet.UpdateCharge(currentTime);
        }

        foreach (BulletEntry entry in bullets)
        {
            if (!entry.IsShown) { continue; }

            entry.Bullet.UpdateCharge(currentTime);
        }
    }

    public void Clear(int holdNumber)
    {
        SpaceHoldBulletInteractEffectController.ClearWaitingBullets(holdNumber);
        ClearBuiltBullets(holdNumber);
    }

    private List<TimedPathPoint> CreatePathPoints(NoteData_SpaceHoldMesh noteData)
    {
        List<TimeToVertices> sortedTimeToVertices = noteData.TimeToVertices
            .OrderBy(x => x.Timing)
            .ToList();
        List<TimedPathPoint> points = new List<TimedPathPoint>();

        foreach (TimeToVertices timeToVertices in sortedTimeToVertices)
        {
            if (timeToVertices.Vertices == null || timeToVertices.Vertices.Length == 0) { continue; }

            Vector2 center = CalcCenter(timeToVertices.Vertices);
            Vector3 chartPosition = new Vector3(center.x, center.y, 0f);
            float width = CalcWidth(timeToVertices.Vertices);

            // Keep waiting bullets on the source XY plane and offset the whole pattern by spawnCenter.
            Vector3 localPosition = spawnCenter + new Vector3(
                center.x * normalizedPositionScale.x,
                center.y * normalizedPositionScale.y,
                0f);

            points.Add(new TimedPathPoint(chartPosition, localPosition, timeToVertices.Timing, width));
        }

        return points;
    }

    private List<TimedPathPoint> SampleByDistance(List<TimedPathPoint> pathPoints)
    {
        List<TimedPathPoint> sampledPoints = new List<TimedPathPoint>();
        if (pathPoints == null || pathPoints.Count == 0) { return sampledPoints; }
        if (pathPoints.Count == 1 || !IsValidSpawnInterval()) { return sampledPoints; }

        sampledPoints.AddRange(CreateChargePoints(pathPoints));

        if (IsSameStartAndEnd(pathPoints))
        {
            if (sampledPoints.Count <= 0)
            {
                sampledPoints.Add(pathPoints[0]);
            }
            return sampledPoints;
        }

        float totalChartDistance = CalcTotalChartDistance(pathPoints);
        if (totalChartDistance <= minSpawnDistance)
        {
            if (sampledPoints.Count <= 0)
            {
                sampledPoints.Add(pathPoints[0]);
            }
            return sampledPoints;
        }

        float totalIntervalDistance = CalcTotalIntervalDistance(pathPoints);
        if (totalIntervalDistance <= 1f)
        {
            sampledPoints.Add(pathPoints[0]);
            sampledPoints.Add(pathPoints[^1]);
            return sampledPoints;
        }

        float nextSpawnDistance = 1f;
        float accumulatedDistance = 0f;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            TimedPathPoint start = pathPoints[i - 1];
            TimedPathPoint end = pathPoints[i];
            float segmentLength = CalcIntervalDistance(start.ChartPosition, end.ChartPosition);
            if (segmentLength <= Mathf.Epsilon) { continue; }

            while (nextSpawnDistance < totalIntervalDistance && nextSpawnDistance <= accumulatedDistance + segmentLength)
            {
                float t = (nextSpawnDistance - accumulatedDistance) / segmentLength;
                sampledPoints.Add(new TimedPathPoint(
                    Vector3.Lerp(start.ChartPosition, end.ChartPosition, t),
                    Vector3.Lerp(start.LocalPosition, end.LocalPosition, t),
                    Mathf.Lerp(start.Timing, end.Timing, t),
                    Mathf.Lerp(start.Width, end.Width, t)));
                nextSpawnDistance += 1f;
            }

            accumulatedDistance += segmentLength;
        }

        return sampledPoints.OrderBy(x => x.Timing).ToList();
    }

    private List<TimedPathPoint> CreateChargePoints(List<TimedPathPoint> pathPoints)
    {
        List<TimedPathPoint> chargePoints = new List<TimedPathPoint>();
        int index = 0;

        while (index < pathPoints.Count - 1)
        {
            if (!IsStaySegment(pathPoints[index], pathPoints[index + 1]))
            {
                index++;
                continue;
            }

            int startIndex = index;
            while (index < pathPoints.Count - 1 && IsStaySegment(pathPoints[index], pathPoints[index + 1]))
            {
                index++;
            }

            int endIndex = index;
            float chargeDuration = pathPoints[endIndex].Timing - pathPoints[startIndex].Timing;
            if (chargeDuration >= minChargeDuration)
            {
                chargePoints.Add(pathPoints[startIndex].WithCharge(pathPoints[startIndex].Timing, pathPoints[endIndex].Timing));
            }
        }

        return chargePoints;
    }

    private bool IsStaySegment(TimedPathPoint start, TimedPathPoint end)
    {
        return Vector3.Distance(start.ChartPosition, end.ChartPosition) <= stayThreshold;
    }

    private bool IsSameStartAndEnd(List<TimedPathPoint> pathPoints)
    {
        return Vector3.Distance(pathPoints[0].ChartPosition, pathPoints[^1].ChartPosition) <= minSpawnDistance;
    }

    private TimedPathPoint GetPointAtDistance(List<TimedPathPoint> pathPoints, float targetDistance)
    {
        float accumulatedDistance = 0f;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            TimedPathPoint start = pathPoints[i - 1];
            TimedPathPoint end = pathPoints[i];
            float segmentLength = CalcIntervalDistance(start.ChartPosition, end.ChartPosition);
            if (segmentLength <= Mathf.Epsilon) { continue; }

            if (targetDistance <= accumulatedDistance + segmentLength)
            {
                float t = (targetDistance - accumulatedDistance) / segmentLength;
                return new TimedPathPoint(
                    Vector3.Lerp(start.ChartPosition, end.ChartPosition, t),
                    Vector3.Lerp(start.LocalPosition, end.LocalPosition, t),
                    Mathf.Lerp(start.Timing, end.Timing, t),
                    Mathf.Lerp(start.Width, end.Width, t));
            }

            accumulatedDistance += segmentLength;
        }

        return pathPoints[^1];
    }

    private float CalcTotalChartDistance(List<TimedPathPoint> pathPoints)
    {
        float totalDistance = 0f;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            totalDistance += Vector3.Distance(pathPoints[i - 1].ChartPosition, pathPoints[i].ChartPosition);
        }

        return totalDistance;
    }

    private float CalcTotalIntervalDistance(List<TimedPathPoint> pathPoints)
    {
        float totalDistance = 0f;

        for (int i = 1; i < pathPoints.Count; i++)
        {
            totalDistance += CalcIntervalDistance(pathPoints[i - 1].ChartPosition, pathPoints[i].ChartPosition);
        }

        return totalDistance;
    }

    private float CalcIntervalDistance(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        return new Vector3(
            CalcIntervalAxis(delta.x, spawnInterval.x),
            CalcIntervalAxis(delta.y, spawnInterval.y),
            CalcIntervalAxis(delta.z, spawnInterval.z)).magnitude;
    }

    private bool IsValidSpawnInterval()
    {
        return spawnInterval.x > Mathf.Epsilon
            || spawnInterval.y > Mathf.Epsilon
            || spawnInterval.z > Mathf.Epsilon;
    }

    private float CalcIntervalAxis(float delta, float interval)
    {
        if (interval <= Mathf.Epsilon) { return 0f; }

        return Mathf.Abs(delta) / interval;
    }

    private Vector2 CalcCenter(Vector2[] vertices)
    {
        Vector2 center = Vector2.zero;
        for (int i = 0; i < vertices.Length; i++)
        {
            center += vertices[i];
        }

        return center / vertices.Length;
    }

    private float CalcWidth(Vector2[] vertices)
    {
        if (vertices == null || vertices.Length == 0) { return 0f; }

        float minX = vertices[0].x;
        float maxX = vertices[0].x;

        for (int i = 1; i < vertices.Length; i++)
        {
            minX = Mathf.Min(minX, vertices[i].x);
            maxX = Mathf.Max(maxX, vertices[i].x);
        }

        return Mathf.Abs(maxX - minX);
    }

    private void ClearBuiltBullets(int holdNumber)
    {
        if (!holdNumberToBullets.TryGetValue(holdNumber, out List<BulletEntry> bullets)) { return; }

        foreach (BulletEntry entry in bullets)
        {
            SpaceHoldBulletInteractEffectController bullet = entry.Bullet;
            if (bullet == null) { continue; }

            Destroy(bullet.gameObject);
        }

        holdNumberToBullets.Remove(holdNumber);
    }

    class TimedPathPoint
    {
        public TimedPathPoint(Vector3 chartPosition, Vector3 localPosition, float timing, float width)
        {
            ChartPosition = chartPosition;
            LocalPosition = localPosition;
            Timing = timing;
            Width = width;
        }

        public Vector3 ChartPosition { get; }
        public Vector3 LocalPosition { get; }
        public float Timing { get; }
        public float Width { get; }
        public bool HasCharge { get; private set; }
        public float ChargeStartTiming { get; private set; }
        public float ChargeEndTiming { get; private set; }

        public TimedPathPoint WithCharge(float chargeStartTiming, float chargeEndTiming)
        {
            return new TimedPathPoint(ChartPosition, LocalPosition, Timing, Width)
            {
                HasCharge = true,
                ChargeStartTiming = chargeStartTiming,
                ChargeEndTiming = chargeEndTiming
            };
        }
    }

    class BulletEntry
    {
        public BulletEntry(SpaceHoldBulletInteractEffectController bullet, float timing)
        {
            Bullet = bullet;
            Timing = timing;
        }

        public SpaceHoldBulletInteractEffectController Bullet { get; }
        public float Timing { get; }
        public bool IsShown { get; set; }
    }
}
