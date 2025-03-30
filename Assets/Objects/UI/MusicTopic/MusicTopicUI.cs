using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicTopicUI : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyToSprite
    {
        [SerializeField] Difficulty difficulty;
        [SerializeField] Sprite sprite;

        public bool CheckCondition(Difficulty difficulty) { return this.difficulty == difficulty; }

        public Sprite Sprite { get { return sprite; } }
    }

    [Header("コンポーネントの参照")]
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

    [Header("難易度別背景")]
    [SerializeField] DifficultyToSprite[] difficultyToSprites;

    MusicData currentSetData;
    Difficulty currentSetDifficulty;

    /// <summary>
    /// 楽曲データのセット
    /// </summary>
    /// <param name="data"></param>
    /// <param name="difficulty"></param>
    public void SetMusicTopic(MusicData data)
    {
        currentSetData = data;

        // 楽曲名
        title_tmp.text = data.MusicName;
        // コンポーザー
        composer_tmp.text = data.ComposerName;
        // サムネ
        music_image.sprite = data.MusicSprite;

        // レコード
        score_tmp.text = data.GetMusicRecord(currentSetDifficulty).Score.ToString();

        //TRACK COMPLETE
        comp_obj.SetActive(data.GetMusicRecord(currentSetDifficulty).ComboRank == ComboRank.TrackComplete);
        //FULL COMBO
        fc_obj.SetActive(data.GetMusicRecord(currentSetDifficulty).ComboRank == ComboRank.FullCombo);
        //ALL PERFECT
        ap_obj.SetActive(data.GetMusicRecord(currentSetDifficulty).ComboRank == ComboRank.AllPerfect);


    }

    /// <summary>
    /// 難易度のセット
    /// </summary>
    /// <param name="b"></param>
    public void SetDifficulty(Difficulty difficulty)
    {
        currentSetDifficulty = difficulty;

        // 難易度名
        diff_tmp.text = difficulty.ToString().ToUpper(); //大文字に

        // 難易度(レベル)
        //if (currentSetData.GetDifficulity(difficulty) != 0) { level_tmp.text = currentSetData.GetDifficulity(difficulty).ToString(); }
        //else { level_tmp.text = "-"; }

        // 難易度背景
        foreach (var difficultyToSprite in difficultyToSprites)
        {
            if (difficultyToSprite.CheckCondition(difficulty))
            {
                back_image.sprite = difficultyToSprite.Sprite;
                break;
            }
        }
    }

    //表示非表示
    public void SetObjActive(bool b)
    {
        this.gameObject.SetActive(b);
    }
}