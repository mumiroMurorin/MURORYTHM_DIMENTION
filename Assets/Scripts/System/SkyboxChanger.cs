using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SkyBoxChanger : MonoBehaviour
{
    [SerializeField] bool playOnAwake;

    [Header("変更後のSkyBox")]
    [SerializeField] Material nextSky;
    [SerializeField] Cubemap nextCubemap;

    [Header("回転設定")]
    [SerializeField] int initialRotation;
    [FormerlySerializedAs("legacyRotationSpeed")]
    [FormerlySerializedAs("rotationSpeed")]
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
        currentRotation = initialRotation;
        ApplySkyboxRotation();
        isSetupSkybox = true;
    }

    private void Update()
    {
        if (RenderSettings.skybox == null) { return; }
        if (!isSetupSkybox) { return; }

        currentRotation += rotationSpeed * Time.deltaTime;
        currentRotation %= 360f;

        ApplySkyboxRotation();
    }

    void ApplySkyboxRotation()
    {
        if (RenderSettings.skybox == null) { return; }
        if (!RenderSettings.skybox.HasProperty("_Rotation")) { return; }

        RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
    }
}