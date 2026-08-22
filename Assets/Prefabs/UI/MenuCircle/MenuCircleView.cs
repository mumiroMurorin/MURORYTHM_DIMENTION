using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UIInRhythmGameScene
{
    public class MenuCircleView : MonoBehaviour
    {
        [SerializeField] PhaseToText[] phaseToTexts;
        [SerializeField] TableReference localizedTextTable = "SliderTopic_MusicSelect";
        [SerializeField] DifficultyToTMPColorGradient[] difficultyToTMPColorGradients;
        [SerializeField] DifficultyToColor[] difficultyToColors;
        [SerializeField] Image[] changableColorImages;
        [SerializeField] TextMeshProUGUI mainText;
        [SerializeField] TextMeshProUGUI sortTagText;
        [SerializeField] TextMeshProUGUI subText;

        PhaseStatusInSelectScene currentPhase;
        bool hasCurrentPhase;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        public void OnChangePhase(PhaseStatusInSelectScene phase)
        {
            currentPhase = phase;
            hasCurrentPhase = true;
            ApplyPhaseText(phase);
        }

        private void OnSelectedLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            if (!hasCurrentPhase) { return; }

            ApplyPhaseText(currentPhase);
        }

        private void ApplyPhaseText(PhaseStatusInSelectScene phase)
        {
            if (phaseToTexts != null)
            {
                foreach (var t in phaseToTexts)
                {
                    if (t.CheckCondition(phase)) 
                    {
                        t.Apply(mainText, localizedTextTable);
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
            [SerializeField] string textKey;

            public bool CheckCondition(PhaseStatusInSelectScene phase)
            {
                return this.phase == phase;
            }

            public void Apply(TMP_Text tmp, TableReference tableReference)
            {
                if (tmp == null) { return; }

                tmp.text = ResolveText(tableReference);
            }

            private string ResolveText(TableReference tableReference)
            {
                if (string.IsNullOrWhiteSpace(textKey) || string.IsNullOrEmpty(tableReference.TableCollectionName))
                {
                    return text;
                }

                var localizedText = LocalizationSettings.StringDatabase.GetLocalizedString(tableReference, textKey);
                return string.IsNullOrEmpty(localizedText) ? text : localizedText;
            }
        }
    }

}
