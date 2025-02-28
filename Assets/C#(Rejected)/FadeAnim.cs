using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeAnim : MonoBehaviour
{
    [Header("�t�F�[�h�A�j���[�V����")]
    [SerializeField] private Animator fade_anim;

    private bool isFadeInfinish; 
    private bool isFadeOutfinish;

    private void Start()
    {
        gameObject.SetActive(true);
    }

    //�t�F�[�h�C���J�n
    public void FadeIn()
    {
        gameObject.SetActive(true);
        fade_anim.SetTrigger("in");
    }

    //�t�F�[�h�A�E�g�J�n
    public void FadeOut()
    {
        gameObject.SetActive(true);
        fade_anim.SetTrigger("out");
    }

    //�t�F�[�h�C���A�A�E�g�̏I��
    public void FinishFade(int i)
    {
        if (i == 0) { isFadeInfinish = true; }
        else { 
            isFadeOutfinish = true;
            //gameObject.SetActive(false);
        }
    }

    //�t�F�[�h�C�����I���������ǂ���������
    public bool IsReturnFadeInFinish()
    {
        return isFadeInfinish;
    }

    //�t�F�[�h�A�E�g���I���������ǂ���������
    public bool IsReturnFadeOutFinish()
    {
        return isFadeOutfinish;
    }
}
