using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SelectUI : MonoBehaviour
{
    [Header("�w�i�̈ڂ�ς�莞��")]
    [SerializeField] private float changeBack_interval = 0.5f;
    [Header("�I�v�V�����J�[�\�����̔w�i�F")]
    [SerializeField] private Color32 cursolBack_color;
    [Header("�I�v�V�����ʏ펞�̔w�i�F")]
    [SerializeField] private Color32 generalBack_color;
    [Header("�y�Ȕw�i")]
    [SerializeField] private GameObject thumbneil_obj;
    [Header("�A�j���[�^�[")]
    [SerializeField] private Animator musicTopic_anim;
    [Header("���y�g�s�b�N(����)")]
    [SerializeField] private MusicTopic[] musicTopics;

    [Header("�ݒ薢�����|�b�v�A�b�v")]
    [SerializeField] private GameObject dontCompOptionpopUp_obj;
    [Header("�Q�[���ݒ�e")]
    [SerializeField] private GameObject option_obj;
    [Header("�m�[�g�X�s�[�h�w�i")]
    [SerializeField] private Image noteSpeedBack_ima;
    [Header("�m�[�g�X�s�[�hTMP")]
    [SerializeField] private TextMeshProUGUI noteSpeed_tmp;
    [Header("�I�t�Z�b�g�w�i")]
    [SerializeField] private Image offsetBack_ima;
    [Header("�I�t�Z�b�gTMP")]
    [SerializeField] private TextMeshProUGUI offset_tmp;

    private Image thumbneil_image;
    private Tweener fade_tween;

    //������
    public void Init()
    {
        thumbneil_image = thumbneil_obj.GetComponent<Image>();

        dontCompOptionpopUp_obj.SetActive(false);
        option_obj.SetActive(false);
    }

    //--------------MusicTopic�֌W--------------

    //�g�s�b�N�̃A�N�e�B�u��ݒ�
    public void SetMusicTopicActive(int num, bool b)
    {
        if(ReturnMusicTopicNum() <= num)
        {
            Debug.LogError("�l���傫�����܂�: " + num);
            return;
        }
        musicTopics[num].SetObjActive(b);
    }

    //�g�s�b�N�Ƀf�[�^���Z�b�g
    public void SetMusicDataToTopic(int num, DifficulityName diff, MusicData md)
    {
        musicTopics[num].SetMusicTopic(md, diff);
    }

    //�g�s�b�N�̍��E�ړ�
    public void MoveMusicTopicAnim(bool isRight)
    {
        if (isRight) { musicTopic_anim.SetTrigger("right"); }
        else { musicTopic_anim.SetTrigger("left"); }
    }

    //�g�s�b�N�̌���A�߂�
    public void SelectMusicTopicAnim(bool isDecision)
    {
        if (isDecision) { musicTopic_anim.SetTrigger("select"); }
        else { musicTopic_anim.SetTrigger("back"); }
    }

    //�w�i�e�[�}�̕ύX
    public void ChangeBackTheme(Sprite sprite)
    {
        if(fade_tween != null && fade_tween.IsPlaying()) { fade_tween.Kill(); }
        fade_tween = thumbneil_image.DOColor(Color.black, changeBack_interval).OnComplete(() =>
        {
            thumbneil_image.sprite = sprite;
            thumbneil_image.DOColor(Color.white, changeBack_interval);
        });
    }

    //�g�s�b�N�̐���Ԃ�
    public int ReturnMusicTopicNum()
    {
        return musicTopics.Length;
    }

    //-----------------�ݒ�֌W------------------

    //�I�v�V�����̕\����\��
    public void DisOption(bool b)
    {
        option_obj.SetActive(b);
    }

    //�m�[�g�X�s�[�h�̃J�[�\���\����\��
    public void PointNoteSpeedOption(bool b)
    {
        if (b) { noteSpeedBack_ima.color = cursolBack_color; }
        else { noteSpeedBack_ima.color = generalBack_color; }
    }
    
    //�m�[�g�X�s�[�h�̕ύX
    public void SetNoteSpeedTMP(float speed)
    {
        noteSpeed_tmp.text = speed.ToString();
    }

    //�I�v�V�����̃J�[�\���\����\��
    public void PointOffsetOption(bool b)
    {
        if (b) { offsetBack_ima.color = cursolBack_color; }
        else { offsetBack_ima.color = generalBack_color; }
    }

    //�I�t�Z�b�g�̕ύX
    public void SetOffsetTMP(int offset)
    {
        offset_tmp.text = offset.ToString();
    }

    //-------------------Root-------------------

    //�ݒ薢�����̃|�b�v�A�b�v�\��
    public IEnumerator DisPopUp()
    {
        dontCompOptionpopUp_obj.SetActive(true);
        yield return new WaitForSeconds(1f);
        dontCompOptionpopUp_obj.SetActive(false);
    }
}
