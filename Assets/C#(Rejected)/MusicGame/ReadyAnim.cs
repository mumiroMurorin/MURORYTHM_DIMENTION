using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReadyAnim : MonoBehaviour
{
    [Header("�uReady?�v�A�j���[�V����")]
    [SerializeField] private Animator ready_anim;

    [Header("�Ȗ�")]
    [SerializeField] private TextMeshProUGUI musicName_tmp;

    [Header("�R���|�[�U�[��")]
    [SerializeField] private TextMeshProUGUI composerName_tmp;

    bool isFinishAnim;

    private void Start()
    {
        //gameObject.SetActive(false);
    }

    public void Init()
    {
        this.gameObject.SetActive(false);
    }

    //�f�[�^�̃Z�b�g
    public void DataSet(MusicData md)
    {
        musicName_tmp.text = md.MusicName;
        composerName_tmp.text = md.ComposerName;
    }

    //�A�j���[�V�����̍Đ�
    public void PlayAnimation()
    {
        gameObject.SetActive(true);
        ready_anim.SetTrigger("start");
    }

    //�A�j���[�V�������I���������Ԃ�
    public bool IsReturnFinishAnim()
    {
        return isFinishAnim;
    }

    //�A�j���[�V�����I�����̌Ăяo���C�x���g
    public void FinishAnimation()
    {
        isFinishAnim = true;
    }

    //�E��
    public void Kill()
    {
        Destroy(this.gameObject);
    }
}
