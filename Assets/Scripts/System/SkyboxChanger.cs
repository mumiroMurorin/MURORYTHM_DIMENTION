using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyBoxChanger : MonoBehaviour
{
    [SerializeField] bool playOnAwake;

    [Header("ïœçXå„ÇÃSkyBox")]
    [SerializeField] Material nextSky;
    [SerializeField] Cubemap nextCubemap;

    [Header("âÒì]ê›íË")]
    [SerializeField] float rotationSpeed = 1f;

    bool isSetupSkybox;
    float currentRotation;

    private void Start()
    {
        if (playOnAwake) { ChangeSkyBoxTrigger(); }
    }

    public void ChangeSkyBoxTrigger()
    {
        RenderSettings.skybox = nextSky;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = nextCubemap;
        isSetupSkybox = true;
    }

    private void Update()
    {
        if (RenderSettings.skybox == null) { return; }
        if (!isSetupSkybox) { return; }

        currentRotation += rotationSpeed * Time.deltaTime;
        currentRotation %= 360f;

        RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
    }
}