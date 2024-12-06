using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicTopic : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title_tmp;
    [SerializeField] private TextMeshProUGUI composer_tmp;
    [SerializeField] private TextMeshProUGUI diff_tmp;
    [SerializeField] private TextMeshProUGUI level_tmp;
    [SerializeField] private TextMeshProUGUI score_tmp;
    [SerializeField] private Image back_image;
    [SerializeField] private Image music_image;
    [SerializeField] private GameObject comp_obj;
    [SerializeField] private GameObject fc_obj;
    [SerializeField] private GameObject ap_obj;

    //データをセット
    public void SetMusicTopic(MusicData data, DifficulityName diff)
    {
        title_tmp.text = data.MusicName; 
        composer_tmp.text = data.ComposerName;
        music_image.sprite = data.MusicSprite;
        diff_tmp.text = diff.ToString().ToUpper(); //大文字に
        if (data.GetDifficulity(diff) != 0) { level_tmp.text = data.GetDifficulity(diff).ToString(); }
        else { level_tmp.text = "-"; }
        score_tmp.text = data.GetMusicRecord(diff).score.ToString();
        back_image.color = ReturnDifficulityColor32(diff, data.GetDifficulity(diff));
        //TRACK COMPLETE
        comp_obj.SetActive(data.GetMusicRecord(diff).combo_rank == ComboRank.TrackComplete);
        //FULL COMBO
        fc_obj.SetActive(data.GetMusicRecord(diff).combo_rank == ComboRank.FullCombo);
        //ALL PERFECT
        ap_obj.SetActive(data.GetMusicRecord(diff).combo_rank == ComboRank.AllPerfect);
    }

    //表示非表示
    public void SetObjActive(bool b)
    {
        this.gameObject.SetActive(b);
    }

    //難易度から色を返す
    private Color32 ReturnDifficulityColor32(DifficulityName diff, int level)
    {
        //プレイ不可
        if (level == 0) { return new Color32(180, 180, 180, 255); }
        switch ((int)diff)
        {
            case 0:
                return new Color32(0, 99, 21, 255);
            case 1:
                return new Color32(118, 118, 27, 255);
            case 2:
                return new Color32(121, 38, 156, 255);
            case 3:
                return new Color32(0, 0, 0, 255);
            default:
                Debug.LogWarning("知らん難易度入ってきた:" + diff);
                return new Color32(0, 0, 0, 0);
        }
    }
}
