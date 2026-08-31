using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class MusicTopic : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] protected TextMeshProUGUI title_tmp;
    [SerializeField] protected TextMeshProUGUI composer_tmp;
    [SerializeField] protected TextMeshProUGUI otherCreator_tmp;
    [SerializeField] protected TextMeshProUGUI diff_tmp;
    [SerializeField] protected TextMeshProUGUI level_tmp;
    [SerializeField] protected TextMeshProUGUI score_tmp;
    [SerializeField] protected Image back_image;
    [SerializeField] protected Image musicTheme_image;
    [SerializeField] protected Image music_image;
    [SerializeField] protected Image scoreLamp_image;
    [SerializeField] protected Image comboLamp_image;
    [SerializeField] protected GameObject NoneChartNoteObj;

    protected MusicData currentMusicData;
    protected Difficulty currentDifficulty;

    [Header("Difficulty Background")]
    [SerializeField] DifficultyToSprite[] difficultyToBackGround;
    [Header("Score Rank Lamp")]
    [SerializeField] ScoreRankToSprite[] rankToLampSprite;
    [Header("Combo Rank Lamp")]
    [SerializeField] ComboRankToSprite[] comboRankToLampSprite;

    /// <summary>
    /// Set music data.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="difficulty"></param>
    public virtual void OnSetMusicTopic(MusicData data)
    {
        currentMusicData = data;
        // Music title
        if (title_tmp != null) { title_tmp.text = data.MusicName; }
        // Composer
        if (composer_tmp != null) { composer_tmp.text = data.ComposerName; }
        // Other creators and chart designer
        UpdateOtherCreatorText();
        // Jacket
        if (music_image != null) { music_image.sprite = data.MusicSprite; }
        // Theme image
        if (musicTheme_image != null) { musicTheme_image.sprite = data.ThemeSprite; }
    }

    /// <summary>
    /// Set difficulty data.
    /// </summary>
    /// <param name="b"></param>
    public virtual void OnSetDifficulty(Difficulty difficulty, int level)
    {
        currentDifficulty = difficulty;
        // Difficulty name
        diff_tmp.text = difficulty.ToString().ToUpper();

        UpdateBackGround(difficulty);
        UpdateLevel(level);
        UpdateOtherCreatorText();
    }

    /// <summary>
    /// Set score data.
    /// </summary>
    /// <param name="record"></param>
    public virtual void OnSetScore(MusicRecord record)
    {
        if (record == null) { return; }

        // Record
        score_tmp.text = record.Score.ToString("N0");

        // Score lamp
        scoreLamp_image.gameObject.SetActive(record.ScoreRank != ScoreRank.None);
        foreach (var spr in rankToLampSprite)
        {
            if (spr.CheckCondition(record.ScoreRank))
            {
                scoreLamp_image.sprite = spr.Sprite;
                break;
            }
        }

        // Combo lamp
        comboLamp_image.gameObject.SetActive(record.ComboRank != ComboRank.None && record.ComboRank != ComboRank.TrackComplete);
        foreach (var spr in comboRankToLampSprite)
        {
            if (spr.CheckCondition(record.ComboRank))
            {
                comboLamp_image.sprite = spr.Sprite;
                break;
            }
        }
    }

    /// <summary>
    /// Update background from difficulty.
    /// </summary>
    /// <param name="data"></param>
    protected void UpdateBackGround(Difficulty difficulty)
    {
        foreach (var back in difficultyToBackGround)
        {
            if (back.CheckCondition(difficulty))
            {
                back_image.sprite = back.Sprite;
                break;
            }
        }
    }

    /// <summary>
    /// Update difficulty level.
    /// </summary>
    /// <param name="level"></param>
    protected void UpdateLevel(int level)
    {
        if (level != -1) { level_tmp.text = level.ToString(); }
        else { level_tmp.text = "-"; }

        NoneChartNoteObj?.SetActive(level <= -1);
    }

    /// <summary>
    /// Toggle visibility.
    /// </summary>
    /// <param name="b"></param>
    public void SetObjActive(bool isActive)
    {
        this.gameObject.SetActive(isActive);
    }

    protected void UpdateOtherCreatorText()
    {
        if (otherCreator_tmp == null || currentMusicData == null) { return; }

        otherCreator_tmp.text = BuildOtherCreatorText(
            currentMusicData.OtherCreator,
            currentMusicData.GetChartDesigner(currentDifficulty)
        );
    }

    protected string BuildOtherCreatorText(string[] otherCreators, string chartDesigner)
    {
        var builder = new StringBuilder();
        if (otherCreators != null)
        {
            foreach (string creator in otherCreators)
            {
                AppendCreatorText(builder, creator);
            }
        }

        AppendCreatorText(builder, string.IsNullOrWhiteSpace(chartDesigner) ? null : $"Chart Designer: {chartDesigner}");
        return builder.ToString();
    }

    protected void AppendCreatorText(StringBuilder builder, string creator)
    {
        if (string.IsNullOrWhiteSpace(creator)) { return; }

        if (builder.Length > 0)
        {
            builder.Append(" / ");
        }

        builder.Append(creator);
    }
}
