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