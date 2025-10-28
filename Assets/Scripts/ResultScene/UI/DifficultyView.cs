using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInResultScene
{
    public abstract class DifficultyView : MonoBehaviour
    {
        [SerializeField] DifficultyToSprite[] difficultyToSprites;
        [SerializeField] DifficultyToTMPColorGradient[] difficultyToTMPColors;
        [SerializeField] protected Image difficultyBackGround;
        [SerializeField] protected TextMeshProUGUI levelTmp;
        [SerializeField] protected TextMeshProUGUI difficultyTmp;

        public virtual void OnChangeDifficulty(Difficulty difficulty)
        {
            ChangeDifficultyText(difficulty);
            ChangeLevelBack(difficulty);
        }

        protected virtual void ChangeDifficultyText(Difficulty difficulty)
        {
            if (difficultyTmp == null) { return; }
            
            difficultyTmp.text = difficulty.ToString().ToUpper();

            foreach (var d in difficultyToTMPColors)
            {
                if (d.CheckCondition(difficulty))
                {
                    d.SetGradient(levelTmp);
                }
            }
        }

        protected virtual void ChangeLevelBack(Difficulty difficulty)
        {
            if (difficultyBackGround == null) { return; }

            foreach (var d in difficultyToSprites)
            {
                if (d.CheckCondition(difficulty))
                {
                    difficultyBackGround.sprite = d.Sprite;
                }
            }
        }

        public virtual void OnChangeLevel(int level)
        {
            levelTmp.text = level.ToString();
        }
    }
}