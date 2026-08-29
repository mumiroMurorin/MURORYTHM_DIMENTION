using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class MusicTopic : MonoBehaviour
{
    [Header("コンポーネントの参照")]
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

    [Header("難易度別背景")]
    [SerializeField] DifficultyToSprite[] difficultyToBackGround;
    [Header("スコアランク別ランプ")]
    [SerializeField] ScoreRankToSprite[] rankToLampSprite;
    [Header("コンボランク別ランプ")]
    [SerializeField] ComboRankToSprite[] comboRankToLampSprite;

    /// <summary>
    /// 楽曲データのセット
    /// </summary>
    /// <param name="data"></param>
    /// <param name="difficulty"></param>
    public virtual void OnSetMusicTopic(MusicData data)
    {
        // 楽曲名
        if (title_tmp != null) { title_tmp.text = data.MusicName; }
        // コンポーザー
        if (composer_tmp != null) { composer_tmp.text = data.ComposerName; }
        // その他制作者
        if (otherCreator_tmp != null) { otherCreator_tmp.text = BuildOtherCreatorText(data.OtherCreator, data.ChartDesigner); }
        // サムネ
        if (music_image != null) { music_image.sprite = data.MusicSprite; }
        // 楽曲テーマ
        if (musicTheme_image != null) { musicTheme_image.sprite = data.ThemeSprite; }
    }

    /// <summary>
    /// 難易度のセット
    /// </summary>
    /// <param name="b"></param>
    public virtual void OnSetDifficulty(Difficulty difficulty, int level)
    {
        // 難易度名
        diff_tmp.text = difficulty.ToString().ToUpper(); //大文字に

        UpdateBackGround(difficulty);
        UpdateLevel(level);
    }

    /// <summary>
    /// スコアのセット
    /// </summary>
    /// <param name="record"></param>
    public virtual void OnSetScore(MusicRecord record)
    {
        if (record == null) { return; }

        // レコード
        score_tmp.text = record.Score.ToString("N0");

        // スコアランプ
        scoreLamp_image.gameObject.SetActive(record.ScoreRank != ScoreRank.None);
        foreach (var spr in rankToLampSprite)
        {
            if (spr.CheckCondition(record.ScoreRank))
            {
                scoreLamp_image.sprite = spr.Sprite;
                break;
            }
        }

        // コンボランプ
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
    /// 引数から背景を変更
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
    /// 難易度(レベル)の更新
    /// </summary>
    /// <param name="level"></param>
    protected void UpdateLevel(int level)
    {
        if (level != -1) { level_tmp.text = level.ToString(); }
        else { level_tmp.text = "-"; }

        NoneChartNoteObj?.SetActive(level <= -1);
    }

    /// <summary>
    /// 表示非表示切り替え
    /// </summary>
    /// <param name="b"></param>
    public void SetObjActive(bool isActive)
    {
        this.gameObject.SetActive(isActive);
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

        AppendCreatorText(builder, string.IsNullOrWhiteSpace(chartDesigner) ? null : $"譜面制作者: {chartDesigner}");
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
