using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class MusicTopic : MonoBehaviour
{
    [Header("コンポーネントの参照")]
    [SerializeField] protected TextMeshProUGUI title_tmp;
    [SerializeField] protected TextMeshProUGUI composer_tmp;
    [SerializeField] protected TextMeshProUGUI diff_tmp;
    [SerializeField] protected TextMeshProUGUI level_tmp;
    [SerializeField] protected TextMeshProUGUI score_tmp;
    [SerializeField] protected Image back_image;
    [SerializeField] protected Image music_image;
    [SerializeField] protected GameObject comp_obj;
    [SerializeField] protected GameObject fc_obj;
    [SerializeField] protected GameObject ap_obj;

    [Header("難易度別背景")]
    [SerializeField] DifficultyToSprite[] difficultyToBackGround;

    /// <summary>
    /// 楽曲データのセット
    /// </summary>
    /// <param name="data"></param>
    /// <param name="difficulty"></param>
    public virtual void OnSetMusicTopic(MusicData data)
    {
        // 楽曲名
        title_tmp.text = data.MusicName;
        // コンポーザー
        composer_tmp.text = data.ComposerName;
        // サムネ
        music_image.sprite = data.MusicSprite;
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

        //TRACK COMPLETE
        comp_obj.SetActive(record.ComboRank == ComboRank.TrackComplete);
        //FULL COMBO
        fc_obj.SetActive(record.ComboRank == ComboRank.FullCombo);
        //ALL PERFECT
        ap_obj.SetActive(record.ComboRank == ComboRank.AllPerfect);
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
    }

    /// <summary>
    /// 表示非表示切り替え
    /// </summary>
    /// <param name="b"></param>
    public void SetObjActive(bool isActive)
    {
        this.gameObject.SetActive(isActive);
    }
}
