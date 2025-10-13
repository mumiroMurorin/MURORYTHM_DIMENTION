using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ComboRank_View : MonoBehaviour
{
    [Header("コンボランク別マテリアル")]
    [SerializeField] TextMaterialPreset presetAllPerfect;
    [SerializeField] TextMaterialPreset presetFullCombo;
    [SerializeField] TextMaterialPreset presetDefault;
    [SerializeField] TextMeshPro textMeshPro;

    public void OnChangeComboRank(ComboRank comboRank)
    {
        switch (comboRank)
        {
            case ComboRank.AllPerfect:
                presetAllPerfect.ApplyPreset(textMeshPro);
                break;
            case ComboRank.FullCombo:
                presetFullCombo.ApplyPreset(textMeshPro);
                break;
            case ComboRank.TrackComplete:
                presetDefault.ApplyPreset(textMeshPro);
                break;
        }
    }

    [System.Serializable]
    private class TextMaterialPreset
    {
        [SerializeField] string text;
        [SerializeField] Material fontMaterial;
        [SerializeField] VertexGradient colorGradient;

        public void ApplyPreset(TextMeshPro tmp)
        {
            tmp.text = text;
            tmp.fontMaterial = fontMaterial;
            tmp.colorGradient = colorGradient;
        }
    }
}
