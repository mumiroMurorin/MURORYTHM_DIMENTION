using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIInResultScene
{
    public class AchievementView : MonoBehaviour
    {
        [SerializeField] ComboRankToSprite[] comboRankToSprites;
        [SerializeField] ScoreRankToSprite[] scoreRankToSprites;
        [SerializeField] Image comboLamp_image;
        [SerializeField] Image scoreLamp_image;

        public void OnChangeComboRank(ComboRank comboRank)
        {
            comboLamp_image.gameObject.SetActive(comboRank != ComboRank.None);
            foreach (var spr in comboRankToSprites)
            {
                if (spr.CheckCondition(comboRank))
                {
                    comboLamp_image.sprite = spr.Sprite;
                    return;
                }
            }

            comboLamp_image.gameObject.SetActive(false);
        }

        public void OnChangeScoreRank(ScoreRank scoreRank)
        {
            foreach (var spr in scoreRankToSprites)
            {
                if (spr.CheckCondition(scoreRank))
                {
                    scoreLamp_image.sprite = spr.Sprite;
                    return;
                }
            }

            scoreLamp_image.gameObject.SetActive(false);
        }
    }
}

