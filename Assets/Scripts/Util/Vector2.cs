using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 Average(this Vector2[] vectors)
    {
        if (vectors == null || vectors.Length == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        foreach (var v in vectors)
        {
            sum += v;
        }

        return sum / vectors.Length;
    }
}