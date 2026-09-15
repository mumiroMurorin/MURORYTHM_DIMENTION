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
    IOptionSetter optionSetter;

    [Inject]
    public void Construct(IOptionGetter optionGetter, IOptionSetter optionSetter)
    {
        this.optionGetter = optionGetter;
        this.optionSetter = optionSetter;
    }

    /// <summary>
    /// BodyTrackingSettingsのロード
    /// </summary>
    public void LoadBodyTrackingSettings()
    {
        if (optionGetter == null) { return; }

        if (!IsExistFile())
        {
            Debug.Log("【System】BodyTrackingSettingsのファイルはありません");
            return;
        }

        if (!Load(out BodyTrackingSettingsDTO dto))
        {
            Debug.LogWarning("【System】BodyTrackingSettingsのロードに失敗しました");
            return;
        }

        optionGetter.TrackingSettings.SetFromDTO(dto);
        optionSetter?.SetCurrentTrackingMode(dto.trackingMode);
        optionSetter?.SetSpaceActionJudgeMagnitudeMultiplier(dto.spaceActionJudgeMagnitudeMultiplier > 0f ? dto.spaceActionJudgeMagnitudeMultiplier : 1f);
        Debug.Log("【System】BodyTrackingSettingsのロードに成功");
    }
}

public static class BodyTrackingSettingsConverter
{
    const string FILE_NAME = "bodyTrackingSettings.json";

    public static bool Save(IOptionGetter optionGetter)
    {
        if (optionGetter == null) { return false; }

        string filePath = Path.Combine(Application.persistentDataPath, FILE_NAME);
        var settings = new BodyTrackingSettingsDTO(
            optionGetter.TrackingSettings,
            optionGetter.CurrentTrackingMode.Value,
            optionGetter.SpaceActionJudgeMagnitudeMultiplier.Value);

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