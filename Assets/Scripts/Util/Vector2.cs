using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 Average(this Vector2[] vectors)
    {
        if (vectors == null || vectors.Length == 0) { return Vector2.zero; }

        Vector2 sum = Vector2.zero;
        foreach (var v in vectors)
        {
            sum += v;
        }

        return sum / vectors.Length;
    }

    public static float Distance(this Vector2[] vertices)
    {
        if (vertices == null || vertices.Length == 0) { return 0f; }

        float sum = 0;
        for (int i = 1; i < vertices.Length; i++)
        {
            sum += Vector2.Distance(vertices[i - 1], vertices[i]);
        }

        return sum;
    }

    public static Vector2 ClampToUnitCircle(this Vector2 pos)
    {
        // magnitude（長さ）が1以下ならそのまま返す
        if (pos.sqrMagnitude <= 1f)
            return pos;

        // 単位ベクトルに正規化し、長さ1の円周上に制限
        return pos.normalized;
    }

    public static Vector2 Center(this Vector2[] vertices)
    {
        if(vertices == null || vertices.Length == 0) { return Vector2.zero; }

        var sum = new Vector2();
        foreach(var v in vertices)
        {
            sum += v;
        }

        return sum / vertices.Length;
    }

    public static float AngleBetweenVectors(Vector2 a, Vector2 b, Vector2 center)
    {
        Vector2 dirA = a - center;
        Vector2 dirB = b - center;

        // -180～180度
        float angle = Vector2.SignedAngle(dirA, dirB);
        return angle;
    }

    public static Vector2 RotatePoint(this Vector2 point, Vector2 center, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector2 translated = point - center;

        float x = translated.x * cos - translated.y * sin;
        float y = translated.x * sin + translated.y * cos;

        return new Vector2(x, y) + center;
    }

    public static Vector2 ScalePoint(this Vector2 point, Vector2 center, float magnitude)
    {
        return center + (point - center) * magnitude;
    }

    public static Vector2 Mirror(this Vector2 point, Vector2 linePointA, Vector2 linePointB)
    {
        // 線分の方向ベクトル（正規化）
        Vector2 lineDir = (linePointB - linePointA).normalized;

        // 線分上のpointAからのベクトル
        Vector2 fromAtoPoint = point - linePointA;

        // 線分への射影点（ベクトル）
        Vector2 projection = Vector2.Dot(fromAtoPoint, lineDir) * lineDir;

        // 射影点のワールド座標
        Vector2 foot = linePointA + projection;

        // 対称点 = 射影点を中心に反転
        return foot * 2 - point;
    }
}

/// <summary>
/// JsonファイルにシリアライズしやすいVector2
/// </summary>
[System.Serializable]
public class SimpleVector2
{
    public float x;
    public float y;

    public SimpleVector2() { }

    public SimpleVector2(Vector2 v)
    {
        x = v.x;
        y = v.y;
    }

    public Vector2 ToVector2() => new Vector2(x, y);
}

/// <summary>
/// JsonファイルにシリアライズしやすいVector3
/// </summary>
[System.Serializable]
public class SimpleVector3
{
    public float x;
    public float y;
    public float z;

    public SimpleVector3() { }

    public SimpleVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector2 ToVector3() => new Vector3(x, y, z);
}