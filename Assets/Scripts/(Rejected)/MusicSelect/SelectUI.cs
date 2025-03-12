using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SelectUI : MonoBehaviour
{
    [Header("背景の移り変わり時間")]
    [SerializeField] private float changeBack_interval = 0.5f;
    [Header("オプションカーソル時の背景色")]
    [SerializeField] private Color32 cursolBack_color;
    [Header("オプション通常時の背景色")]
    [SerializeField] private Color32 generalBack_color;
    [Header("楽曲背景")]
    [SerializeField] private GameObject thumbneil_obj;
    [Header("アニメーター")]
    [SerializeField] private Animator musicTopic_anim;
    [Header("音楽トピック(左順)")]
    [SerializeField] private MusicTopic[] musicTopics;

    [Header("設定未完了ポップアップ")]
    [SerializeField] private GameObject dontCompOptionpopUp_obj;
    [Header("ゲーム設定親")]
    [SerializeField] private GameObject option_obj;
    [Header("ノートスピード背景")]
    [SerializeField] private Image noteSpeedBack_ima;
    [Header("ノートスピードTMP")]
    [SerializeField] private TextMeshProUGUI noteSpeed_tmp;
    [Header("オフセット背景")]
    [SerializeField] private Image offsetBack_ima;
    [Header("オフセットTMP")]
    [SerializeField] private TextMeshProUGUI offset_tmp;

    private Image thumbneil_image;
    private Tweener fade_tween;

    //初期化
    public void Init()
    {
        thumbneil_image = thumbneil_obj.GetComponent<Image>();

        dontCompOptionpopUp_obj.SetActive(false);
        option_obj.SetActive(false);
    }

    //--------------MusicTopic関係--------------

    //トピックのアクティブを設定
    public void SetMusicTopicActive(int num, bool b)
    {
        if(ReturnMusicTopicNum() <= num)
        {
            Debug.LogError("値が大きすぎます: " + num);
            return;
        }
        musicTopics[num].SetObjActive(b);
    }

    //トピックにデータをセット
    public void SetMusicDataToTopic(int num, DifficulityName diff, MusicData md)
    {
        musicTopics[num].SetMusicTopic(md, diff);
    }

    //トピックの左右移動
    public void MoveMusicTopicAnim(bool isRight)
    {
        if (isRight) { musicTopic_anim.SetTrigger("right"); }
        else { musicTopic_anim.SetTrigger("left"); }
    }

    //トピックの決定、戻る
    public void SelectMusicTopicAnim(bool isDecision)
    {
        if (isDecision) { musicTopic_anim.SetTrigger("select"); }
        else { musicTopic_anim.SetTrigger("back"); }
    }

    //背景テーマの変更
    public void ChangeBackTheme(Sprite sprite)
    {
        if(fade_tween != null && fade_tween.IsPlaying()) { fade_tween.Kill(); }
        fade_tween = thumbneil_image.DOColor(Color.black, changeBack_interval).OnComplete(() =>
        {
            thumbneil_image.sprite = sprite;
            thumbneil_image.DOColor(Color.white, changeBack_interval);
        });
    }

    //トピックの数を返す
    public int ReturnMusicTopicNum()
    {
        return musicTopics.Length;
    }

    //-----------------設定関係------------------

    //オプションの表示非表示
    public void DisOption(bool b)
    {
        option_obj.SetActive(b);
    }

    //ノートスピードのカーソル表示非表示
    public void PointNoteSpeedOption(bool b)
    {
        if (b) { noteSpeedBack_ima.color = cursolBack_color; }
        else { noteSpeedBack_ima.color = generalBack_color; }
    }
    
    //ノートスピードの変更
    public void SetNoteSpeedTMP(float speed)
    {
        noteSpeed_tmp.text = speed.ToString();
    }

    //オプションのカーソル表示非表示
    public void PointOffsetOption(bool b)
    {
        if (b) { offsetBack_ima.color = cursolBack_color; }
        else { offsetBack_ima.color = generalBack_color; }
    }

    //オフセットの変更
    public void SetOffsetTMP(int offset)
    {
        offset_tmp.text = offset.ToString();
    }

    //-------------------Root-------------------

    //設定未完了のポップアップ表示
    public IEnumerator DisPopUp()
    {
        dontCompOptionpopUp_obj.SetActive(true);
        yield return new WaitForSeconds(1f);
        dontCompOptionpopUp_obj.SetActive(false);
    }
}
