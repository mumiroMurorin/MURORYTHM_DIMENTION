using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicTopicDestruction : MusicTopic
{
    [SerializeField] Image levelBackImage;
    [SerializeField] DifficultyToSprite[] difficultyToLevelBackSprites;
    [SerializeField] DifficultyToTMPColorGradient[] difficultyToTMPColors;

    public override void OnSetDifficulty(Difficulty difficulty, int level)
    {
        base.OnSetDifficulty(difficulty, level);

        // 難易度背景、変更
        foreach(var dts in difficultyToLevelBackSprites)
        {
            if (dts.CheckCondition(difficulty)) 
            {
                levelBackImage.sprite = dts.Sprite;
            }
        }

        // レベルテキスト色変更
        foreach (var dtc in difficultyToTMPColors)
        {
            if (dtc.CheckCondition(difficulty))
            {
                dtc.SetGradient(level_tmp);
            }
        }

        // 難易度名
        if (difficulty == Difficulty.Master)
        {
            diff_tmp.text = "APOCALYPSE";
        } 
    }
}
