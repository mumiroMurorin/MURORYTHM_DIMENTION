using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace UIInResultScene
{
    public class ScoreRankView : MonoBehaviour
    {
        [SerializeField] ScoreRankTextMaterialPreset[] presets;
        [SerializeField] TextMeshProUGUI tmp;

        public void OnChangeScoreRank(ScoreRank scoreRank)
        {
            if (presets == null) { return; }

            foreach (var preset in presets)
            {
                if (preset.ScoreRank != scoreRank) { continue; }

                preset.ApplyPreset(tmp, true);
            }
        }
    }

}