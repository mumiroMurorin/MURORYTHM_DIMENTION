using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Score_View : MonoBehaviour
{
    [Header("ランク別マテリアル")]
    [SerializeField] TextMaterialPreset[] presetRank;
    [SerializeField] TextMeshPro textMeshPro;

    public void OnChangeScoreRank(ScoreRank scoreRank)
    {
        if (presetRank == null) { return; }

        foreach(var preset in presetRank)
        {
            if (preset.ScoreRank != scoreRank) { continue; }

            preset.ApplyPreset(textMeshPro);
        }
    }

    public void OnChangeScore(float score)
    {
        textMeshPro.text = score.ToString("N0");
    }

    [System.Serializable]
    private class TextMaterialPreset
    {
        [SerializeField] ScoreRank scoreRank;
        [SerializeField] Material fontMaterial;
        [SerializeField] TMP_ColorGradient colorGradient;

        public ScoreRank ScoreRank { get { return scoreRank; } }

        public void ApplyPreset(TextMeshPro tmp)
        {
            tmp.fontMaterial = fontMaterial;
            tmp.colorGradientPreset = colorGradient;
        }
    }
}
