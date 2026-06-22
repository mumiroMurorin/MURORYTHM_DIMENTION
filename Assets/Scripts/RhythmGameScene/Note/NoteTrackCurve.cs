using System.Collections.Generic;
using Deform;
using UnityEngine;

/// <summary>
/// 【ノーツ軌道】進行距離を円弧上の座標と姿勢へ変換する
/// </summary>
public static class NoteTrackCurve
{
    const float MIN_RADIUS = 0.01f;

    /// <summary>
    /// 【円弧上の位置】判定位置を原点として、奥側ほど上昇する座標を返す
    /// </summary>
    public static Vector3 GetPosition(float distance, float radius)
    {
        float safeRadius = Mathf.Max(radius, MIN_RADIUS);
        float angle = distance / safeRadius;

        return new Vector3(
            0f,
            safeRadius * (1f - Mathf.Cos(angle)),
            safeRadius * Mathf.Sin(angle));
    }

    /// <summary>
    /// 【円弧上の姿勢】ノーツの前方向を円弧の接線へ合わせる
    /// </summary>
    public static Quaternion GetRotation(float distance, float radius)
    {
        float safeRadius = Mathf.Max(radius, MIN_RADIUS);
        float angle = distance / safeRadius;
        return Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, Vector3.right);
    }

    /// <summary>
    /// 【ノーツ配置】単発ノーツを円弧上へ配置する
    /// </summary>
    public static void SetPose(Transform target, float distance, float radius)
    {
        if (target == null) { return; }

        target.localPosition = GetPosition(distance, radius);
        target.localRotation = GetRotation(distance, radius);
    }

    /// <summary>
    /// 【軌道移動】円の中心を回転軸としてノーツ親を進行させる
    /// </summary>
    public static void SetProgress(Transform target, float distance, float radius)
    {
        if (target == null) { return; }

        float safeRadius = Mathf.Max(radius, MIN_RADIUS);
        float angle = distance / safeRadius;
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.right);
        Vector3 pivot = new Vector3(0f, safeRadius, 0f);

        target.localRotation = rotation;
        target.localPosition = pivot - rotation * pivot;
    }

    /// <summary>
    /// 【メッシュ変換】直線上の頂点を、断面形状を保ったまま円弧へ曲げる
    /// </summary>
    public static Vector3 BendVertex(Vector3 vertex, float radius)
    {
        float safeRadius = Mathf.Max(radius, MIN_RADIUS);
        float angle = vertex.z / safeRadius;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        return new Vector3(
            vertex.x,
            safeRadius + (vertex.y - safeRadius) * cos,
            (safeRadius - vertex.y) * sin);
    }

    /// <summary>
    /// 【メッシュ変換】頂点リストをまとめて円弧へ曲げる
    /// </summary>
    public static void BendVertices(List<Vector3> vertices, float radius)
    {
        if (vertices == null) { return; }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = BendVertex(vertices[i], radius);
        }
    }

    /// <summary>
    /// 【Deformer移行】ノーツPrefabに残っているDeformableを生成時に取り除く
    /// </summary>
    public static void RemoveDeformables(GameObject target)
    {
        if (target == null) { return; }

        foreach (Deformable deformable in target.GetComponentsInChildren<Deformable>(true))
        {
            deformable.enabled = false;
            Object.Destroy(deformable);
        }
    }
}
