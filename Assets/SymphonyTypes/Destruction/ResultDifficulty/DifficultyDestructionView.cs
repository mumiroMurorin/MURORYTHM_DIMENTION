using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class DifficultyDestructionView : DifficultyView
    {
        public override void OnChangeDifficulty(Difficulty difficulty)
        {
            base.OnChangeDifficulty(difficulty);

            if (difficulty == Difficulty.Master)
            {
                difficultyTmp.text = "APOCALYPSE";
            }
        }
    }
}