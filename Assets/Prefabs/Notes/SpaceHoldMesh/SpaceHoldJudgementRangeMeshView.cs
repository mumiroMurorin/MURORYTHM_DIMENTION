using System.Collections.Generic;
using UnityEngine;
using MeshGenerate;

public class SpaceHoldJudgementRangeMeshView : MonoBehaviour
{
    const int PriorityEffectsLayerFallback = 15;

    [SerializeField] bool showJudgementRange = true;
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Material material;
    [SerializeField] string layerName = "PriorityEffects";
    [SerializeField] float lineWidth = 0.05f;
    [SerializeField] float zOffset = -0.02f;
    [SerializeField] float normalizeRadius = 10f;

    Mesh currentMesh;

    public void SetShowJudgementRange(bool show)
    {
        showJudgementRange = show;
        if (!showJudgementRange)
        {
            SetVisible(false);
        }
    }

    public void Initialize(Transform lineParent)
    {
        SetupMeshObject(lineParent);
        SetVisible(false);
    }

    public void UpdateRange(Vector2[] points, bool isVisible)
    {
        if (!isVisible || !showJudgementRange)
        {
            SetVisible(false);
            return;
        }

        if (meshFilter == null || meshRenderer == null)
        {
            SetVisible(false);
            return;
        }

        if (points == null || points.Length < 2)
        {
            SetVisible(false);
            return;
        }

        List<Vector3> linePoints = new List<Vector3>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 point = points[i];
            linePoints.Add(MeshGenerator.Normalize(new Vector3(point.x, point.y, zOffset), Vector3.zero, normalizeRadius));
        }

        Mesh previousMesh = currentMesh;
        currentMesh = MeshGenerator.GenerateLineMesh(linePoints, lineWidth, true);
        meshFilter.mesh = currentMesh;

        if (previousMesh != null)
        {
            Destroy(previousMesh);
        }

        SetVisible(true);
    }

    public void SetVisible(bool isVisible)
    {
        if (meshRenderer == null) { return; }
        meshRenderer.enabled = showJudgementRange && isVisible;
    }

    private void SetupMeshObject(Transform lineParent)
    {
        if (meshFilter == null || meshRenderer == null) { return; }

        Transform lineTransform = meshFilter.transform;
        lineTransform.SetParent(lineParent);
        lineTransform.position = Vector3.zero;
        lineTransform.rotation = Quaternion.identity;
        lineTransform.localScale = Vector3.one;
        lineTransform.gameObject.layer = GetPriorityEffectsLayer();

        if (material != null)
        {
            meshRenderer.material = material;
        }
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (currentMesh != null)
        {
            Destroy(currentMesh);
        }
    }

    private int GetPriorityEffectsLayer()
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : PriorityEffectsLayerFallback;
    }
}
