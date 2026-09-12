using UnityEngine;
using UnityEngine.UI;

public abstract class StageController : MonoBehaviour, IStageController
{
    [SerializeField] protected SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;

    public void Initialize(IMusicDataGetter musicDataGetter)
    {
        if (musicDataGetter == null || musicDataGetter.Music == null || musicDataGetter.Music.Value == null)
        {
            Debug.LogWarning($"[{GetType().Name}] MusicDataGetter is not set.");
            return;
        }

        MusicData musicData = musicDataGetter.Music.Value;
        Difficulty difficulty = musicDataGetter.Difficulty != null
            ? musicDataGetter.Difficulty.Value
            : Difficulty.Normal;

        InitializeStage(musicData, difficulty);
    }

    protected abstract void InitializeStage(MusicData musicData, Difficulty difficulty);

    protected string GetTitleText(MusicData musicData, string separator = " / ")
    {
        if (musicData == null) { return string.Empty; }

        string title = musicData.MusicName ?? string.Empty;
        string composer = musicData.ComposerName ?? string.Empty;
        return string.IsNullOrWhiteSpace(composer) ? title : $"{title}{separator}{composer}";
    }

    protected string GetDifficultyText(MusicData musicData, Difficulty difficulty)
    {
        if (difficulty != Difficulty.Master)
        {
            return difficulty.ToString().ToUpper();
        }

        SymphonyType symphonyType = musicData != null ? musicData.SymphonyType : SymphonyType.None;
        string text = symphonyTypePresentationDatabase?.GetMasterDifficultyText(symphonyType);
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text.ToUpper();
        }

        Debug.LogWarning($"[{GetType().Name}] Master difficulty text is not set: {symphonyType}");
        return Difficulty.Master.ToString().ToUpper();
    }

    protected static string GetLevelText(MusicData musicData, Difficulty difficulty)
    {
        if (musicData == null) { return string.Empty; }

        int level = musicData.GetDifficulty(difficulty);
        return level >= 0 ? level.ToString() : string.Empty;
    }

    protected static string GetDifficultyLevelText(string difficultyText, string levelText)
    {
        if (string.IsNullOrWhiteSpace(levelText)) { return difficultyText ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(difficultyText)) { return levelText; }
        return $"{difficultyText}  LEVEL {levelText}";
    }

    protected static void SetImages(Image[] images, Sprite sprite)
    {
        if (images == null) { return; }

        foreach (Image image in images)
        {
            if (image != null) { image.sprite = sprite; }
        }
    }
}

public interface IStageController
{
    void Initialize(IMusicDataGetter musicDataGetter);
}
