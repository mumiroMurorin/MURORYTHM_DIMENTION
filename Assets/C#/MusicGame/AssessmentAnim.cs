using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssessmentAnim : MonoBehaviour
{
    //[Header("�A�j���[�V����")]
    //[SerializeField] private Animator anim;

    bool isFinishAnim;

    private void Start()
    {
        
    }

    //�A�j���[�V�����̍Đ�
    public void PlayAnimation()
    {
        gameObject.SetActive(true);
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
}
