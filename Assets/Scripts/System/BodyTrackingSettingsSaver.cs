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
    /// BodyTrackingSettings‚Ì•Û‘¶
    /// </summary>
    public void SaveBodyTrackingSettings()
    {
        if(optionGetter == null) { return; }

        if (!Save(optionGetter.TrackingSettings))
        {
            Debug.LogWarning("ySystemzBodyTrackingSettings‚Ì•Û‘¶‚É¸”s‚µ‚Ü‚µ‚½");
        }

        Debug.Log("ySystemzBodyTrackingSettings‚Ì•Û‘¶‚É¬Œ÷");
    }
}