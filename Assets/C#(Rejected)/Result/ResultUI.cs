using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultUI : MonoBehaviour
{
    [Header("サムネ")]
    [SerializeField] private Image music_image;
    [Header("サムネ後ろ")]
    [SerializeField] private Image diff_image;
    [Header("難易度")]
    [SerializeField] private TextMeshProUGUI diff_tmp;
    [Header("タイトル")]
    [SerializeField] private TextMeshProUGUI title_tmp;
    [Header("コンポーザー")]
    [SerializeField] private TextMeshProUGUI composer_tmp;
    [Header("スコア")]
    [SerializeField] private TextMeshProUGUI score_tmp;
    [Header("ランク")]
    [SerializeField] private TextMeshProUGUI rank_tmp;
    [Header("perfect数")]
    [SerializeField] private TextMeshProUGUI perfectNum_tmp;
    [Header("great数")]
    [SerializeField] private TextMeshProUGUI greatNum_tmp;
    [Header("good数")]
    [SerializeField] private TextMeshProUGUI goodNum_tmp;
    [Header("miss数")]
    [SerializeField] private TextMeshProUGUI missNum_tmp;
    [Header("Comp")]
    [SerializeField] private GameObject comp_obj;
    [Header("FC")]
    [SerializeField] private GameObject fc_obj;
    [Header("AP")]
    [SerializeField] private GameObject ap_obj;
    [SerializeField] private FadeAnim fade;

    //楽曲データセット
    public void SetMusicData(MusicData data, DifficulityName diff, Color diff_color)
    {
        music_image.sprite = data.MusicSprite;
        diff_image.color = diff_color;
        diff_tmp.text = diff.ToString();
        title_tmp.text = data.MusicName;
        composer_tmp.text = data.ComposerName;
    }

    //リザルトデータセット
    public void SetResultData(MusicRecord record)
    {
        score_tmp.text = record.score.ToString();
        rank_tmp.text = record.rank.ToString();

        perfectNum_tmp.text = record.judge_num[(int)TimingJudge.perfect].ToString();
        greatNum_tmp.text = record.judge_num[(int)TimingJudge.great].ToString();
        goodNum_tmp.text = record.judge_num[(int)TimingJudge.good].ToString();
        missNum_tmp.text = record.judge_num[(int)TimingJudge.miss].ToString();

        comp_obj.SetActive(record.combo_rank == ComboRank.TrackComplete);
        fc_obj.SetActive(record.combo_rank == ComboRank.FullCombo);
        ap_obj.SetActive(record.combo_rank == ComboRank.AllPerfect);
    }

    //--------------フェード関係--------------

    //フェードイン
    public void FadeInStart()
    {
        fade.FadeIn();
    }

    //フェードアウト
    public void FadeOutStart()
    {
        fade.FadeOut();
    }

    //フェードインが終わったか返す
    public bool IsReturnFadeInFinish()
    {
        return fade.IsReturnFadeInFinish();
    }

    //フェードアウトが終わったか返す
    public bool IsReturnFadeOutFinish()
    {
        return fade.IsReturnFadeOutFinish();
    }
}
