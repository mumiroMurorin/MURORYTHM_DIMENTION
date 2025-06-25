using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Lab : MonoBehaviour
{
    [SerializeField] List<Vector3> frontList;
    [SerializeField] List<Vector3> backList;
    [SerializeField] float depth = 5f;
    [SerializeField] int minSampleCount = 0;

    void Start()
    {
        //Mesh tunnel = TunnelMeshGenerator.GenerateTunnelMesh(frontList, backList, depth, minSampleCount);
        Mesh tunnel = TunnelMeshGenerator.GenerateSeamlessTwistedMesh(frontList, backList);

        var go = new GameObject("Tunnel", typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().mesh = tunnel;
        go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
    }
}

public static class TunnelMeshGenerator
{
    // 最大公約数
    static int GCD(int a, int b)
    {
        while (b != 0)
        {
            int r = a % b;
            a = b;
            b = r;
        }
        return a;
    }

    // 最小公倍数
    static int LCM(int a, int b)
    {
        return a / GCD(a, b) * b;
    }

    /// <summary>
    /// shape の周長における各頂点の fraction (0～1) を計算。
    /// </summary>
    static List<float> ComputeFractions(List<Vector2> shape)
    {
        int m = shape.Count;
        var segLengths = new float[m];
        float perim = 0f;
        for (int i = 0; i < m; i++)
        {
            float d = Vector2.Distance(shape[i], shape[(i + 1) % m]);
            segLengths[i] = d;
            perim += d;
        }
        var fractions = new List<float>(m + 1) { 0f };
        float acc = 0f;
        for (int i = 1; i < m; i++)
        {
            acc += segLengths[i - 1];
            fractions.Add(acc / perim);
        }
        fractions.Add(1f);
        return fractions;
    }

    /// <summary>
    /// shape を指定の fractions に従って再サンプリング。
    /// </summary>
    static List<Vector2> SampleByFractions(List<Vector2> shape, List<float> fractions)
    {
        int m = shape.Count;
        var segLengths = new float[m];
        float perim = 0f;
        for (int i = 0; i < m; i++)
        {
            float d = Vector2.Distance(shape[i], shape[(i + 1) % m]);
            segLengths[i] = d;
            perim += d;
        }
        var cumDist = new float[m + 1];
        cumDist[0] = 0f;
        for (int i = 0; i < m; i++)
            cumDist[i + 1] = cumDist[i] + segLengths[i];

        var result = new List<Vector2>(fractions.Count);
        foreach (float f in fractions)
        {
            float target = Mathf.Clamp01(f) * perim;
            int idx = Array.BinarySearch(cumDist, target);
            if (idx < 0) idx = ~idx - 1;
            idx = Mathf.Clamp(idx, 0, m - 1);
            float segStart = cumDist[idx];
            float segLen = segLengths[idx];
            float t = segLen > 0f ? (target - segStart) / segLen : 0f;
            Vector2 p0 = shape[idx];
            Vector2 p1 = shape[(idx + 1) % m];
            result.Add(Vector2.Lerp(p0, p1, t));
        }
        return result;
    }

    /// <summary>
    /// frontShape/backShape を Z 方向に押し出してトンネルメッシュを生成。
    /// 異なる頂点数でも滑らかにつながり、かつ最低サンプル数以上の分割を保証します。
    /// </summary>
    /// <param name="minSampleCount">fraction の最低数（0 以下で元頂点のみ）</param>
    public static Mesh GenerateTunnelMesh(
        List<Vector2> frontShape,
        List<Vector2> backShape,
        float depth,
        int minSampleCount = 0
    )
    {
        if (frontShape == null || backShape == null)
            throw new ArgumentNullException("shape lists must not be null");
        if (frontShape.Count < 2 || backShape.Count < 2)
            throw new ArgumentException("Each shape must have at least 2 points");

        // 元頂点の fraction
        var fracF = ComputeFractions(frontShape);
        var fracB = ComputeFractions(backShape);

        // union 且つ sort
        var allFrac = fracF.Concat(fracB)
                           .Distinct()
                           .OrderBy(f => f)
                           .ToList();
        // 最低サンプル数を満たす
        if (minSampleCount > 1 && allFrac.Count < minSampleCount)
        {
            var even = Enumerable.Range(0, minSampleCount)
                                 .Select(i => i / (float)(minSampleCount - 1))
                                 .ToList();
            allFrac = allFrac.Concat(even)
                             .Distinct()
                             .OrderBy(f => f)
                             .ToList();
        }

        // サンプリング
        var frontS = SampleByFractions(frontShape, allFrac);
        var backS = SampleByFractions(backShape, allFrac);
        int n = allFrac.Count;

        // メッシュ構築
        var verts = new Vector3[n * 2];
        var tris = new List<int>(n * 6);

        for (int i = 0; i < n; i++)
        {
            verts[i] = new Vector3(frontS[i].x, frontS[i].y, 0f);
            verts[i + n] = new Vector3(backS[i].x, backS[i].y, depth);
        }

        // 外側
        for (int i = 0; i < n; i++)
        {
            int ni = (i + 1) % n;

            tris.Add(i);
            tris.Add(i + n);
            tris.Add(ni);

            tris.Add(ni);
            tris.Add(i + n);
            tris.Add(ni + n);
        }

        var mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh GenerateSeamlessTwistedMesh(
    List<Vector3> fromPolygon,
    List<Vector3> toPolygon,
    float zOffset = 5f,
    int interpolateSteps = 20 // 分割数
)
    {
        int fromCount = fromPolygon.Count;
        int toCount = toPolygon.Count;

        // 対応点を同数に補完
        int maxCount = Mathf.Max(fromCount, toCount);
        var fromPoints = ResamplePolygon(fromPolygon, maxCount);
        var toPoints = ResamplePolygon(toPolygon, maxCount);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // 断面ごとに線形補間してメッシュを作る
        for (int i = 0; i <= interpolateSteps; i++)
        {
            float t = i / (float)interpolateSteps;

            for (int j = 0; j < maxCount; j++)
            {
                Vector3 from = fromPoints[j];
                Vector3 to = toPoints[j];
                Vector3 lerp = Vector3.Lerp(from, to, t);
                lerp.z = t * zOffset;
                vertices.Add(lerp);
            }
        }

        // 三角形構成
        for (int i = 0; i < interpolateSteps; i++)
        {
            int baseIndex = i * maxCount;
            for (int j = 0; j < maxCount; j++)
            {
                int current = baseIndex + j;
                int next = baseIndex + (j + 1) % maxCount;
                int upper = current + maxCount;
                int upperNext = next + maxCount;

                // 2 triangles = quad
                triangles.Add(current);
                triangles.Add(upper);
                triangles.Add(upperNext);

                triangles.Add(current);
                triangles.Add(upperNext);
                triangles.Add(next);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static List<Vector3> ResamplePolygon(List<Vector3> points, int targetCount)
    {
        float totalLength = 0f;
        for (int i = 0; i < points.Count; i++)
            totalLength += Vector3.Distance(points[i], points[(i + 1) % points.Count]);

        float segmentLength = totalLength / targetCount;
        List<Vector3> resampled = new List<Vector3>();
        float accumulated = 0f;

        int currentIndex = 0;
        Vector3 current = points[0];
        Vector3 next = points[1];
        float remaining = Vector3.Distance(current, next);

        resampled.Add(current);

        for (int i = 1; i < targetCount; i++)
        {
            float distanceToNext = segmentLength;

            while (distanceToNext > remaining)
            {
                distanceToNext -= remaining;
                currentIndex = (currentIndex + 1) % points.Count;
                current = points[currentIndex];
                next = points[(currentIndex + 1) % points.Count];
                remaining = Vector3.Distance(current, next);
            }

            Vector3 dir = (next - current).normalized;
            Vector3 newPoint = current + dir * distanceToNext;
            resampled.Add(newPoint);
            current = newPoint;
            remaining -= distanceToNext;
        }

        return resampled;
    }
}