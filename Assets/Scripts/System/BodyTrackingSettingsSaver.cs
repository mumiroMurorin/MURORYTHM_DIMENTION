using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BodyTrackingSettingsConverter;
using VContainer;
using System.IO;

public class BodyTrackingSettingsSaver : MonoBehaviour
{
    IOptionGetter optionGetter;

    [Inject]
    public void Construct(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    /// <summary>
    /// BodyTrackingSettingsの保存
    /// </summary>
    public void SaveBodyTrackingSettings()
    {
        if(optionGetter == null) { return; }

        if (!Save(optionGetter))
        {
            Debug.LogWarning("【System】BodyTrackingSettingsの保存に失敗しました");
        }

        Debug.Log("【System】BodyTrackingSettingsの保存に成功");
    }
}