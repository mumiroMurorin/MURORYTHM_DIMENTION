using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReadyToPlayUIControllerView : MonoBehaviour
{
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;

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

        SymphonyTypePresentationData presentationData = symphonyTypePresentationDatabase?.Get(symphonyType);
        Sprite sprite = presentationData?.GetReadyDifficultySprite(difficulty);
        if (sprite == null)
        {
            Debug.LogWarning($"[ReadyToPlayUIControllerView] Ready difficulty sprite is not set: {symphonyType}, {difficulty}");
            return;
        }

        difficultyImage.sprite = sprite;
    }
}