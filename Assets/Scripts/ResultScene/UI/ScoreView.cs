using UnityEngine;
using TMPro;

namespace UIInResultScene
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] ScoreRankTextMaterialPreset[] presets;
        [SerializeField] TextMeshProUGUI tmp;
        [SerializeField] RectTransform newRecordRoot;

        public void OnChangeScoreRank(ScoreRank scoreRank)
        {
            if (presets == null) { return; }

            foreach (var preset in presets)
            {
                if (preset.ScoreRank != scoreRank) { continue; }

                preset.ApplyPreset(tmp);
            }
        }

        public void OnChangeScore(float score)
        {
            tmp.text = score.ToString("N0");
        }

        public void SetNewRecordActive(bool isActive)
        {
            if (newRecordRoot == null) { return; }

            newRecordRoot.gameObject.SetActive(isActive);
        }
    }

}
