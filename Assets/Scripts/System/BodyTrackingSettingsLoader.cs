using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static JsonUtil.JsonWriter;
using static JsonUtil.JsonLoader;
using static BodyTrackingSettingsConverter;
using VContainer;
using System.IO;

public class BodyTrackingSettingsLoader : MonoBehaviour
{
    IOptionGetter optionGetter;

    [Inject]
    public void Construct(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    /// <summary>
    /// BodyTrackingSettingsÇÃÉçÅ[Éh
    /// </summary>
    public void LoadBodyTrackingSettings()
    {
        if (optionGetter == null) { return; }

        if (!IsExistFile())
        {
            Debug.Log("ÅySystemÅzBodyTrackingSettingsÇÃÉtÉ@ÉCÉãÇÕÇ†ÇËÇ‹ÇπÇÒ");
            return;
        }

        if (!Load(out BodyTrackingSettingsDTO dto))
        {
            Debug.LogWarning("ÅySystemÅzBodyTrackingSettingsÇÃÉçÅ[ÉhÇ…é∏îsÇµÇ‹ÇµÇΩ");
            return;
        }

        optionGetter.TrackingSettings.SetFromDTO(dto);
        Debug.Log("ÅySystemÅzBodyTrackingSettingsÇÃÉçÅ[ÉhÇ…ê¨å˜");
    }
}

public static class BodyTrackingSettingsConverter
{
    const string FILE_NAME = "bodyTrackingSettings.json";

    public static bool Save(BodyTrackingSettings trackingSettings)
    {
        string filePath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        var settings = new BodyTrackingSettingsDTO(trackingSettings);

        return TrySaveToJsonFile(settings, filePath);
    }

    public static bool Load(out BodyTrackingSettingsDTO settingsDTO)
    {
        string filePath = Path.Combine(Application.persistentDataPath, FILE_NAME);

        return TryLoadFromJsonFile(filePath, out settingsDTO);
    }

    public static bool IsExistFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        return File.Exists(filePath);
    }
}
