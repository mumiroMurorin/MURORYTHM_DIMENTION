using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReadyToPlayUIControllerView : MonoBehaviour
{
    [SerializeField] StageDataToSprite[] dataToBackSprites;

    [SerializeField] TextMeshProUGUI titleTmp;
    [SerializeField] TextMeshProUGUI composerTmp;
    [SerializeField] Image musicImage;
    [SerializeField] Image difficultyImage;

    Difficulty difficulty;
    SymphonyType symphonyType;
    bool isSetDifficulty;
    bool isSetSymphonyType;

    public void SetDifficulty(Difficulty difficulty)
    {
        this.difficulty = difficulty;
        isSetDifficulty = true;

        if(isSetDifficulty && isSetSymphonyType) { SetDifficultyBack(difficulty, symphonyType); }
    }

    public void SetMusicData(MusicData musicData)
    {
        if (musicData == null) { return; }

        if (titleTmp) { titleTmp.text = musicData.MusicName; }
        if (composerTmp) { composerTmp.text = musicData.ComposerName; }
        if (musicImage) { musicImage.sprite = musicData.MusicSprite; }

        this.symphonyType = musicData.SymphonyType;
        isSetSymphonyType = true;

        if (isSetDifficulty && isSetSymphonyType) { SetDifficultyBack(difficulty, symphonyType); }
    }

    private void SetDifficultyBack(Difficulty difficulty, SymphonyType symphonyType)
    {
        if (difficultyImage == null) { return; }

        foreach (var pair in dataToBackSprites)
        {
            if (pair.CheckCondition(difficulty, symphonyType))
            {
                difficultyImage.sprite = pair.Sprite;
            }
        }
    }

    [System.Serializable]
    public class StageDataToSprite
    {
        [SerializeField] Difficulty difficulty;
        [SerializeField] SymphonyType symphonyType;
        [SerializeField] Sprite sprite;

        public bool CheckCondition(Difficulty difficulty, SymphonyType symphonyType)
        {
            return this.difficulty == difficulty && this.symphonyType == symphonyType;
        }

        public Sprite Sprite { get { return sprite; } }
    }
}
