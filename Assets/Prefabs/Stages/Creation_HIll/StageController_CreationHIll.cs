using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageController_CreationHill : MonoBehaviour, IStageController
{
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;

    [Header("Title Text")]
    [SerializeField] WorldLoopingClippedTMPText titleText;
    [SerializeField] string titleComposerSeparator = " / ";

    [Header("Difficulty Level Loop Text")]
    [SerializeField] WorldLoopingClippedTMPText difficultyLevelText;

    [Header("Text Render Queue")]
    [SerializeField] bool overrideTextRenderQueue;
    [SerializeField] int titleTextRenderQueue = 3000;
    [SerializeField] int difficultyLevelTextRenderQueue = 3000;

    [Header("Theme Images")]
    [SerializeField] Image[] themeImages;

    [Header("Jacket Images")]
    [SerializeField] Image[] jacketImages;

    void IStageController.Initialize(IMusicDataGetter musicDataGetter)
    {
        if (musicDataGetter == null || musicDataGetter.Music == null || musicDataGetter.Music.Value == null)
        {
            Debug.LogWarning("[StageController_Hill] MusicDataGetter is not set.");
            return;
        }

        MusicData musicData = musicDataGetter.Music.Value;
        Difficulty difficulty = musicDataGetter.Difficulty != null ? musicDataGetter.Difficulty.Value : Difficulty.Normal;
        string difficultyTextValue = GetDifficultyText(musicData, difficulty);
        string levelTextValue = GetLevelText(musicData, difficulty);

        SetTitleText(GetTitleText(musicData));
        SetDifficultyLevelText(GetDifficultyLevelText(difficultyTextValue, levelTextValue));
        ApplyTextRenderQueue();

        SetImages(themeImages, musicData.ThemeSprite);
        SetImages(jacketImages, musicData.MusicSprite);
    }

    void SetTitleText(string text)
    {
        if (titleText == null)
        {
            Debug.LogWarning("[StageController_Hill] Title text controller is not set.");
            return;
        }

        titleText.SetText(text);
    }

    void SetDifficultyLevelText(string text)
    {
        if (difficultyLevelText == null)
        {
            return;
        }

        difficultyLevelText.SetText(text);
    }

    void ApplyTextRenderQueue()
    {
        if (!overrideTextRenderQueue) { return; }

        if (titleText != null)
        {
            titleText.SetRenderQueue(titleTextRenderQueue);
        }

        if (difficultyLevelText != null)
        {
            difficultyLevelText.SetRenderQueue(difficultyLevelTextRenderQueue);
        }
    }

    void SetImages(Image[] images, Sprite sprite)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            if (image == null)
            {
                continue;
            }

            image.sprite = sprite;
        }
    }

    string GetTitleText(MusicData musicData)
    {
        if (musicData == null)
        {
            return string.Empty;
        }

        string title = musicData.MusicName ?? string.Empty;
        string composer = musicData.ComposerName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(composer))
        {
            return title;
        }

        return $"{title}{titleComposerSeparator}{composer}";
    }

    string GetDifficultyText(MusicData musicData, Difficulty difficulty)
    {
        if (difficulty != Difficulty.Master)
        {
            return difficulty.ToString().ToUpper();
        }

        SymphonyType symphonyType = musicData != null ? musicData.SymphonyType : SymphonyType.None;
        string masterDifficultyText = symphonyTypePresentationDatabase?.GetMasterDifficultyText(symphonyType).ToUpper();
        if (!string.IsNullOrEmpty(masterDifficultyText))
        {
            return masterDifficultyText;
        }

        Debug.LogWarning($"[StageController_Hill] Master difficulty text is not set: {symphonyType}");
        return Difficulty.Master.ToString().ToUpper();
    }

    string GetLevelText(MusicData musicData, Difficulty difficulty)
    {
        if (musicData == null)
        {
            return string.Empty;
        }

        int level = musicData.GetDifficulty(difficulty);
        return level >= 0 ? level.ToString() : string.Empty;
    }

    string GetDifficultyLevelText(string difficultyTextValue, string levelTextValue)
    {
        if (string.IsNullOrWhiteSpace(levelTextValue))
        {
            return difficultyTextValue ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(difficultyTextValue))
        {
            return levelTextValue;
        }

        return $"{difficultyTextValue}  LEVEL {levelTextValue}";
    }
}
