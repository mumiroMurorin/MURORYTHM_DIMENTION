using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UniRx;
using VContainer;
using Deform;

public class GroundOptionSetter : MonoBehaviour
{
    [SerializeField] GameObject[] divisionLines;
    [SerializeField] BendDeformer bendDeformer;
    [SerializeField] Transform groundObjectRoot;
    [SerializeField, Min(0f)] float visibleDistancePadding;

    IOptionGetter optionGetter;
    readonly List<MeshRenderer> groundRenderers = new List<MeshRenderer>();
    readonly List<Mesh> additionalVertexStreams = new List<Mesh>();
    MaterialPropertyBlock propertyBlock;

    static readonly int GroundClipEnabledId = Shader.PropertyToID("_GroundClipEnabled");
    static readonly int GroundVisibleDistanceId = Shader.PropertyToID("_GroundVisibleDistance");

    [Inject]
    public void Construct(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    private void Start()
    {
        InitializeGroundVisibility();
        Bind();
    }

    private void Bind()
    {
        optionGetter?.GroundDivisionNum
            .Subscribe(SetDivisionLines)
            .AddTo(this.gameObject);

        optionGetter?.NoteCurveRadius
            .Subscribe(SetBendAngle)
            .AddTo(this.gameObject);

        optionGetter?.NoteVisibleDistance
            .Subscribe(SetGroundVisibleDistance)
            .AddTo(this.gameObject);
    }

    private void InitializeGroundVisibility()
    {
        if (bendDeformer == null || groundObjectRoot == null) { return; }

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

    private void SetGroundVisibleDistance(float distance)
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

    private void SetDivisionLines(int divNum)
    {
        if(divisionLines.Length != 17) { return; }

        for (int i = 0; i < divisionLines.Length; i++) 
        {
            if (i == 0 || i == 16) 
            {
                divisionLines[i].SetActive(true);
            }
            else if(i % (16 / divNum) == 0)
            {
                divisionLines[i].SetActive(true);
            }
            else
            {
                divisionLines[i].SetActive(false);
            }
        }
    }

    private void SetBendAngle(float radius)
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

    private void OnDestroy()
    {
        foreach (Mesh vertexStream in additionalVertexStreams)
        {
            if (vertexStream != null) { Destroy(vertexStream); }
        }
    }
}
