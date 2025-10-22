using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ComboRank_View : MonoBehaviour
{
    [Header("コンボランク別マテリアル")]
    [SerializeField] TextMaterialPreset[] presets;
    [SerializeField] TextMeshPro textMeshPro;

    public void OnChangeComboRank(ComboRank comboRank)
    {
        if (presets != null)
        {
            foreach (var preset in presets)
            {
                if (preset.CheckCondition(comboRank))
                {
                    preset.ApplyPreset(textMeshPro);
                }
            }
        }
    }

    [System.Serializable]
    private class TextMaterialPreset
    {
        [SerializeField] ComboRank comboRank;
        [SerializeField] string text;
        [SerializeField] Material fontMaterial;
        [SerializeField] TMP_ColorGradient colorGradient;

        public bool CheckCondition(ComboRank comboRank)
        {
            return this.comboRank == comboRank;
        }

        public void ApplyPreset(TMP_Text tmp)
        {
            tmp.text = text;
            tmp.fontMaterial = fontMaterial;
            tmp.colorGradientPreset = colorGradient;
        }
    }
}
