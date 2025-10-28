using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class DifficultyCreationView : DifficultyView
    {
        public override void OnChangeDifficulty(Difficulty difficulty)
        {
            base.OnChangeDifficulty(difficulty);

            if (difficulty == Difficulty.Master)
            {
                difficultyTmp.text = "GENESIS";
            }
        }
    }
}