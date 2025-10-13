using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Combo_View : MonoBehaviour
{
    [Header("何コンボから表示するか")]
    [SerializeField] int comboThreshold = 5;
    [Header("コンボ別マテリアル")]
    [SerializeField] TextMaterialPreset presetAllPerfect;
    [SerializeField] TextMaterialPreset presetFullCombo;
    [SerializeField] TextMaterialPreset presetDefault;
    [SerializeField] TextMeshPro textMeshPro;
    [SerializeField] Animator animator;

    public void OnChangeCombo(int comboNum)
    {
        textMeshPro.text = comboNum.ToString();

        if (comboThreshold > comboNum)
        {
            textMeshPro.gameObject.SetActive(false);
        }
        else
        {
            textMeshPro.gameObject.SetActive(true);
            animator.SetTrigger("combo");
        }
    }

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
        [SerializeField] Material fontMaterial;
        [SerializeField] VertexGradient colorGradient;

        public void ApplyPreset(TextMeshPro tmp)
        {
            tmp.fontMaterial = fontMaterial;
            tmp.colorGradient = colorGradient;
        }
    }
}
