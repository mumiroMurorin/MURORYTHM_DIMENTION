using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Combo_View : MonoBehaviour
{
    [Header("何コンボから表示するか")]
    [SerializeField] int comboThreshold = 5;
    [Header("コンボ別マテリアル")]
    [SerializeField] TextMaterialPreset[] presets;
    [SerializeField] TextMeshPro textMeshPro;
    [SerializeField] Animator animator;

    public void OnChangeCombo(int comboNum)
    {
        textMeshPro.text = comboNum.ToString();
        textMeshPro.enabled = comboThreshold < comboNum;

        animator.SetTrigger("combo");
    }

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
        [SerializeField] Material fontMaterial;
        [SerializeField] TMP_ColorGradient colorGradient;

        public bool CheckCondition(ComboRank comboRank)
        {
            return this.comboRank == comboRank;
        }

        public void ApplyPreset(TMP_Text tmp)
        {
            tmp.fontMaterial = fontMaterial;
            tmp.colorGradientPreset = colorGradient;
        }
    }
}
