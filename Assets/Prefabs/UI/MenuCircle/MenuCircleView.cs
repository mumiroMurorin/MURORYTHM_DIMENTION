using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInRhythmGameScene
{
    public class MenuCircleView : MonoBehaviour
    {
        [SerializeField] PhaseToText[] phaseToTexts;
        [SerializeField] DifficultyToTMPColorGradient[] difficultyToTMPColorGradients;
        [SerializeField] DifficultyToColor[] difficultyToColors;
        [SerializeField] Image[] changableColorImages;
        [SerializeField] TextMeshProUGUI mainText;
        [SerializeField] TextMeshProUGUI sortTagText;
        [SerializeField] TextMeshProUGUI subText;

        public void OnChangePhase(PhaseStatusInSelectScene phase)
        {
            if (phaseToTexts != null)
            {
                foreach (var t in phaseToTexts)
                {
                    if (t.CheckCondition(phase)) 
                    {
                        t.Apply(mainText);
                        break;
                    }
                }
            }
        }

        public void OnChangeDifficulty(Difficulty difficulty)
        {
            subText.text = difficulty.ToString().ToUpper();

            // 文字色の変更
            if (difficultyToTMPColorGradients != null)
            {
                foreach (var t in difficultyToTMPColorGradients)
                {
                    if (t.CheckCondition(difficulty))
                    {
                        t.SetGradient(subText);
                        break;
                    }
                }
            }

            // 各種色の変更
            if (changableColorImages != null)
            {
                var color = Color.white;
                foreach (var c in difficultyToColors)
                {
                    // 適切な色を取り出す
                    if (c.CheckCondition(difficulty)) 
                    { 
                        color = c.Color;
                        break;
                    }
                }

                // アタッチ
                foreach (var i in changableColorImages)
                {
                    // 透明度は保護
                    i.color = new Color(color.r, color.g, color.b, i.color.a);
                }
            }
        }

        [System.Serializable]
        class PhaseToText
        {
            [SerializeField] PhaseStatusInSelectScene phase;
            [SerializeField] string text;

            public bool CheckCondition(PhaseStatusInSelectScene phase)
            {
                return this.phase == phase;
            }

            public void Apply(TMP_Text tmp)
            {
                tmp.text = text;
            }
        }
    }

}
