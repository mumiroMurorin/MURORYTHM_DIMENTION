using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityFx.Outline;

public class OutlineSetter : MonoBehaviour
{
    [SerializeField] OutlineSettings settings;

    private void Start()
    {
        settings.ApplyOutline(this.gameObject);
    }
}

[System.Serializable]
public class OutlineSettings
{
    [SerializeField] Color color;
    [SerializeField] int width;
    [SerializeField] OutlineResources resources;
    [SerializeField] OutlineRenderFlags flags;
    [SerializeField] LayerMask ignoreRayerMask = -1;
    [SerializeField] CameraEvent renderEvent;

    public Color Color => color;

    public OutlineBehaviour ApplyOutline(GameObject obj)
    {
        return ApplyOutline(obj, color);
    }

    public OutlineBehaviour ApplyOutline(GameObject obj, Color outlineColor)
    {
        if (obj == null) { return null; }

        OutlineBehaviour outline = obj.GetComponent<OutlineBehaviour>();
        if (outline == null)
        {
            outline = obj.AddComponent<OutlineBehaviour>();
        }

        outline.OutlineResources = resources;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = width;
        outline.OutlineRenderMode = flags;
        outline.Camera = Camera.main;
        outline.IgnoreLayerMask = ignoreRayerMask;
        outline.RenderEvent = renderEvent;
        return outline;
    }
}
