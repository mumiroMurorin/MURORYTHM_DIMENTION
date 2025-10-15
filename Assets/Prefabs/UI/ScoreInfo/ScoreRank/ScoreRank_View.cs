using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreRank_View : MonoBehaviour
{
    [Header("コンボランク別マテリアル")]
    [SerializeField] TextMaterialPreset[] presetRank;
    [SerializeField] TextMeshPro textMeshPro;

    public void OnChangeScoreRank(ScoreRank scoreRank)
    {
        Debug.Log(scoreRank);

        if(presetRank == null) { return; }

        foreach(var preset in presetRank)
        {
            if (preset.ScoreRank != scoreRank) { continue; }

            preset.ApplyPreset(textMeshPro);
        }
    }

    [System.Serializable]
    private class TextMaterialPreset
    {
        [SerializeField] ScoreRank scoreRank;
        [SerializeField] string text;
        [SerializeField] Material fontMaterial;
        [SerializeField] VertexGradient colorGradient;

        public ScoreRank ScoreRank { get { return scoreRank; } }

        public void ApplyPreset(TextMeshPro tmp)
        {
            tmp.text = text;
            tmp.fontMaterial = fontMaterial;
            tmp.colorGradient = colorGradient;
        }
    }
}
