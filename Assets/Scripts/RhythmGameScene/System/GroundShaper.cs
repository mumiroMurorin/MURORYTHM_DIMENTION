using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Deform;

/// <summary>
/// グラウンドの形を決める: カーブ (BendDeformer の曲がり) と、奥行きの表示範囲 (それより奥のグラウンドをシェーダーで切る)。
/// 分割線の出し入れ (SetDivisionLines) も、ここにまとめてある
///
/// ゲーム本編 (GroundOptionSetter) と、譜面エディタの全画面プレビュー (GroundPreviewOptionSetter) の両方で使う。
/// (エディタの舞台には GroundOptionSetter が無い。ゲーム本編の設定が届かないので、エディタの設定から同じ形にする。2026-11-17)
/// </summary>
public class GroundShaper
{
    readonly BendDeformer bendDeformer;
    readonly Transform groundObjectRoot;
    readonly float visibleDistancePadding;

    readonly List<MeshRenderer> groundRenderers = new List<MeshRenderer>();
    readonly List<Mesh> additionalVertexStreams = new List<Mesh>();
    MaterialPropertyBlock propertyBlock;

    static readonly int GroundClipEnabledId = Shader.PropertyToID("_GroundClipEnabled");
    static readonly int GroundVisibleDistanceId = Shader.PropertyToID("_GroundVisibleDistance");

    public GroundShaper(BendDeformer bendDeformer, Transform groundObjectRoot, float visibleDistancePadding)
    {
        this.bendDeformer = bendDeformer;
        this.groundObjectRoot = groundObjectRoot;
        this.visibleDistancePadding = visibleDistancePadding;
    }

    /// <summary>
    /// 奥行きの表示範囲で切れるように、グラウンドのメッシュごとに「曲がりの軸に沿った距離」を持たせる (一度だけ)
    /// </summary>
    public void InitializeVisibility()
    {
        if (bendDeformer == null || groundObjectRoot == null) { return; }
        if (propertyBlock != null) { return; }

        propertyBlock = new MaterialPropertyBlock();
        groundRenderers.Clear();

        foreach (MeshRenderer renderer in groundObjectRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            Mesh sourceMesh = GetOriginalMesh(renderer);
            if (sourceMesh == null || sourceMesh.vertexCount == 0 || !sourceMesh.isReadable) { continue; }

            try
            {
                Mesh vertexStream = CreateDistanceVertexStream(renderer.transform, sourceMesh);
                renderer.additionalVertexStreams = vertexStream;
                additionalVertexStreams.Add(vertexStream);
                groundRenderers.Add(renderer);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, renderer);
            }
        }
    }

    /// <summary>
    /// 判定位置から、この距離より奥のグラウンドを見せない
    /// </summary>
    public void SetVisibleDistance(float distance)
    {
        if (propertyBlock == null) { return; }

        foreach (MeshRenderer renderer in groundRenderers)
        {
            if (renderer == null) { continue; }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(GroundClipEnabledId, 1f);
            propertyBlock.SetFloat(GroundVisibleDistanceId, distance + visibleDistancePadding);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>
    /// グラウンドを、この半径の円に沿って曲げる (ノーツの並ぶ曲線と同じ)
    /// </summary>
    public void SetCurveRadius(float radius)
    {
        if (bendDeformer == null) { return; }

        float bendLength = Mathf.Abs(bendDeformer.Top - bendDeformer.Bottom);
        float factor = Mathf.Abs(bendDeformer.Factor);
        if (bendLength <= Mathf.Epsilon || factor <= Mathf.Epsilon) { return; }

        float direction = bendDeformer.Angle > 0f ? 1f : -1f;
        bendDeformer.Angle = direction
            * bendLength
            / Mathf.Max(radius, 0.01f)
            * Mathf.Rad2Deg
            / factor;
    }

    /// <summary>
    /// グラウンドの分割線を、分割数に合わせて出す / 隠す (両端の線は、いつも出す)
    /// </summary>
    /// <param name="divisionLines">分割線 (左から順に 17 本。17 本でないときは、何もしない)</param>
    /// <param name="divNum">分割数 (16 を割り切れる数)</param>
    public static void SetDivisionLines(GameObject[] divisionLines, int divNum)
    {
        if (divisionLines == null || divisionLines.Length != 17) { return; }

        for (int i = 0; i < divisionLines.Length; i++)
        {
            if (i == 0 || i == 16)
            {
                divisionLines[i].SetActive(true);
            }
            else if (i % (16 / divNum) == 0)
            {
                divisionLines[i].SetActive(true);
            }
            else
            {
                divisionLines[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 作ったメッシュを捨てる (持ち主が消えるとき)
    /// </summary>
    public void Dispose()
    {
        foreach (Mesh vertexStream in additionalVertexStreams)
        {
            if (vertexStream != null) { Object.Destroy(vertexStream); }
        }
        additionalVertexStreams.Clear();
    }

    private Mesh CreateDistanceVertexStream(Transform meshTransform, Mesh sourceMesh)
    {
        Matrix4x4 meshToBendAxis = bendDeformer.Axis.worldToLocalMatrix * meshTransform.localToWorldMatrix;
        Vector3[] vertices = sourceMesh.vertices;
        List<Vector2> distances = new List<Vector2>(vertices.Length);

        foreach (Vector3 vertex in vertices)
        {
            float distance = meshToBendAxis.MultiplyPoint3x4(vertex).y - bendDeformer.Bottom;
            distances.Add(new Vector2(distance, 0f));
        }

        Mesh vertexStream = new Mesh { name = $"{sourceMesh.name}_GroundVisibilityStream" };
        vertexStream.SetVertexBufferParams(
            vertices.Length,
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2));
        vertexStream.SetVertexBufferData(
            distances,
            0,
            0,
            distances.Count,
            0,
            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
        return vertexStream;
    }

    private static Mesh GetOriginalMesh(MeshRenderer renderer)
    {
        Deformable deformable = renderer.GetComponent<Deformable>();
        if (deformable != null && deformable.GetOriginalMesh() != null)
        {
            return deformable.GetOriginalMesh();
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        return meshFilter != null ? meshFilter.sharedMesh : null;
    }
}
