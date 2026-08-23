using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace UIInResultScene
{
    public class RankingItemView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI rankingNumberText;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] GameObject currentPlayMarker;

        public void SetRankingData(int rankingNumber, int score, bool isCurrentPlay)
        {
            if (rankingNumberText != null)
            {
                rankingNumberText.text = rankingNumber.ToString("#0");
            }

            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0");
            }

            if (currentPlayMarker != null)
            {
                currentPlayMarker.SetActive(isCurrentPlay);
            }
        }
    }

}
