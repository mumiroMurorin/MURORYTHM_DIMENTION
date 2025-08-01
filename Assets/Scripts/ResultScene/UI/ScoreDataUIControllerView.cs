using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIInResultScene
{
    public class ScoreDataUIControllerView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI score_text;
        [SerializeField] TextMeshProUGUI perfectNum_text;
        [SerializeField] TextMeshProUGUI greatNum_text;
        [SerializeField] TextMeshProUGUI goodNum_text;
        [SerializeField] TextMeshProUGUI missNum_text;
        [SerializeField] TextMeshProUGUI rank_text;
        [SerializeField] GameObject complete_obj;
        [SerializeField] GameObject comboRank_obj;

        public void SetScoreData(IScoreGetter scoreData)
        {
            if (score_text && scoreData.Score != null) { score_text.text = ((int)scoreData.Score.Value).ToString("N0"); }
            if (perfectNum_text) { perfectNum_text.text = scoreData.PerfectNum.ToString(); }
            if (greatNum_text) { greatNum_text.text = scoreData.GreatNum.ToString(); }
            if (goodNum_text) { goodNum_text.text = scoreData.GoodNum.ToString(); }
            if (missNum_text) { missNum_text.text = scoreData.MissNum.ToString(); }

            if (rank_text) { rank_text.text = scoreData.GetCurrentScoreRankString(); }
        }
    }

}