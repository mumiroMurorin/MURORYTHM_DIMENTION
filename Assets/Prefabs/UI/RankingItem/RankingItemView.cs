using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInResultScene
{
    public class RankingItemView : MonoBehaviour
    {
        [System.Serializable]
        class RankingColorData
        {
            [SerializeField] int rankingNumber = 1;
            [SerializeField] Color flameColor = Color.white;
            [SerializeField] TMP_ColorGradient rankingNumberGradient;

            public int RankingNumber => rankingNumber;
            public Color FlameColor => flameColor;
            public TMP_ColorGradient RankingNumberGradient => rankingNumberGradient;
        }

        [SerializeField] TextMeshProUGUI rankingNumberText;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] GameObject currentPlayMarker;
        [SerializeField] Image flameImage;
        [SerializeField] Color fallbackFlameColor = Color.white;
        [SerializeField] TMP_ColorGradient fallbackRankingNumberGradient;
        [SerializeField] List<RankingColorData> rankingColorDatas = new List<RankingColorData>();

        public void SetRankingData(int rankingNumber, int score, bool isCurrentPlay)
        {
            if (rankingNumberText != null)
            {
                rankingNumberText.text = rankingNumber.ToString("'#'0");
            }

            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0");
            }

            if (currentPlayMarker != null)
            {
                currentPlayMarker.SetActive(isCurrentPlay);
            }

            ApplyRankingColor(rankingNumber);
        }

        void ApplyRankingColor(int rankingNumber)
        {
            RankingColorData colorData = FindRankingColorData(rankingNumber);

            if (flameImage != null)
            {
                flameImage.color = colorData != null ? colorData.FlameColor : fallbackFlameColor;
            }

            if (rankingNumberText == null) { return; }

            TMP_ColorGradient gradient = colorData != null && colorData.RankingNumberGradient != null
                ? colorData.RankingNumberGradient
                : fallbackRankingNumberGradient;

            if (gradient == null) { return; }

            rankingNumberText.enableVertexGradient = true;
            rankingNumberText.colorGradientPreset = gradient;
        }

        RankingColorData FindRankingColorData(int rankingNumber)
        {
            for (int i = 0; i < rankingColorDatas.Count; i++)
            {
                RankingColorData colorData = rankingColorDatas[i];
                if (colorData == null) { continue; }
                if (colorData.RankingNumber == rankingNumber) { return colorData; }
            }

            return null;
        }
    }

}
