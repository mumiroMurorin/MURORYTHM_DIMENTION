using UnityEngine;
using UnityEngine.UI;

public class StageController_CyberRoom : StageController
{
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

    protected override void InitializeStage(MusicData musicData, Difficulty difficulty)
    {
        string difficultyTextValue = GetDifficultyText(musicData, difficulty);
        string levelTextValue = GetLevelText(musicData, difficulty);

        SetTitleText(GetTitleText(musicData, titleComposerSeparator));
        SetDifficultyLevelText(GetDifficultyLevelText(difficultyTextValue, levelTextValue));
        ApplyTextRenderQueue();

        SetImages(themeImages, musicData.ThemeSprite);
        SetImages(jacketImages, musicData.MusicSprite);
    }

    void SetTitleText(string text)
    {
        if (titleText == null)
        {
            Debug.LogWarning("[StageController_CyberRoom] Title text controller is not set.");
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

}
