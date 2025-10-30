using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreRank_View : MonoBehaviour
{
    [Header("コンボランク別マテリアル")]
    [SerializeField] ScoreRankTextMaterialPreset[] presetRank;
    [SerializeField] TextMeshPro textMeshPro;
    [SerializeField] DoScalePulseBuilder scalePulseBuilder;

    public void OnChangeScoreRank(ScoreRank scoreRank)
    {
        if(presetRank == null) { return; }

        foreach(var preset in presetRank)
        {
            if (preset.ScoreRank != scoreRank) { continue; }

            preset.ApplyPreset(textMeshPro, true);
        }

        scalePulseBuilder?.ApplyScalePulse(textMeshPro.transform);
    }
}
