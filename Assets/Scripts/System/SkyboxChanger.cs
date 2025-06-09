using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyBoxChanger : MonoBehaviour
{
    [Header("ïœçXå„ÇÃSkyBox")]
    [SerializeField] Material nextSky;
    [SerializeField] Cubemap nextCubemap;

    public void ChangeSkyBoxTrigger()
    {
        RenderSettings.skybox = nextSky;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = nextCubemap;
    }
}