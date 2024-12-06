using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultUI : MonoBehaviour
{
    [Header("�T���l")]
    [SerializeField] private Image music_image;
    [Header("�T���l���")]
    [SerializeField] private Image diff_image;
    [Header("��Փx")]
    [SerializeField] private TextMeshProUGUI diff_tmp;
    [Header("�^�C�g��")]
    [SerializeField] private TextMeshProUGUI title_tmp;
    [Header("�R���|�[�U�[")]
    [SerializeField] private TextMeshProUGUI composer_tmp;
    [Header("�X�R�A")]
    [SerializeField] private TextMeshProUGUI score_tmp;
    [Header("�����N")]
    [SerializeField] private TextMeshProUGUI rank_tmp;
    [Header("perfect��")]
    [SerializeField] private TextMeshProUGUI perfectNum_tmp;
    [Header("great��")]
    [SerializeField] private TextMeshProUGUI greatNum_tmp;
    [Header("good��")]
    [SerializeField] private TextMeshProUGUI goodNum_tmp;
    [Header("miss��")]
    [SerializeField] private TextMeshProUGUI missNum_tmp;
    [Header("Comp")]
    [SerializeField] private GameObject comp_obj;
    [Header("FC")]
    [SerializeField] private GameObject fc_obj;
    [Header("AP")]
    [SerializeField] private GameObject ap_obj;
    [SerializeField] private FadeAnim fade;

    //�y�ȃf�[�^�Z�b�g
    public void SetMusicData(MusicData data, DifficulityName diff, Color diff_color)
    {
        music_image.sprite = data.MusicSprite;
        diff_image.color = diff_color;
        diff_tmp.text = diff.ToString();
        title_tmp.text = data.MusicName;
        composer_tmp.text = data.ComposerName;
    }

    //���U���g�f�[�^�Z�b�g
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

    //--------------�t�F�[�h�֌W--------------

    //�t�F�[�h�C��
    public void FadeInStart()
    {
        fade.FadeIn();
    }

    //�t�F�[�h�A�E�g
    public void FadeOutStart()
    {
        fade.FadeOut();
    }

    //�t�F�[�h�C�����I��������Ԃ�
    public bool IsReturnFadeInFinish()
    {
        return fade.IsReturnFadeInFinish();
    }

    //�t�F�[�h�A�E�g���I��������Ԃ�
    public bool IsReturnFadeOutFinish()
    {
        return fade.IsReturnFadeOutFinish();
    }
}
