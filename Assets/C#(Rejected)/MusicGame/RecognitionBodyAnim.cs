using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecognitionBodyAnim : MonoBehaviour
{
    [Header("�F���A�j���[�V����")]
    [SerializeField] private Animator recognition_anim;

    bool isFinishOpenAnim;
    bool isFinishCloseAnim;

    private void Start()
    {
        //this.gameObject.SetActive(false);   
    }

    public void Init() 
    {
        this.gameObject.SetActive(false);
    }

    //�I�[�v���A�j���[�V�����̍Đ�
    public void PlayOpenAnimation()
    {
        gameObject.SetActive(true);
        recognition_anim.SetTrigger("start");
    }

    //�I���A�j���[�V�����̍Đ�
    public void PlayCloseAnimation()
    {
        gameObject.SetActive(true);
        recognition_anim.SetTrigger("finish");
    }

    //�A�j���[�V�������I���������Ԃ�
    public bool IsReturnFinishOpenAnim()
    {
        return isFinishOpenAnim;
    }

    //�A�j���[�V�������I���������Ԃ�
    public bool IsReturnFinishCloseAnim()
    {
        return isFinishCloseAnim;
    }

    //�A�j���[�V�����I�����̌Ăяo���C�x���g
    public void FinishOpenAnimation()
    {
        isFinishOpenAnim = true;
    }

    //�A�j���[�V�����I�����̌Ăяo���C�x���g
    public void FinishCloseAnimation()
    {
        isFinishCloseAnim = true;
    }
}
