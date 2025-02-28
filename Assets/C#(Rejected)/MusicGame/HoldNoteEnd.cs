using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

public class HoldNoteEnd : MonoBehaviour
{
    PlayGame playGame;
    HoldNote hold;
    Deformable deformable;

    private bool isTouch;
    private bool isJudgeComp;
    private int judge = 3; //0p 1g 2g 3m
    private float speed;

    void Start()
    {
        
    }

    private void Update()
    {
        isTouch = IsTouchSlider();
    }

    void FixedUpdate()
    {
        //TimeAdvance();  //����i�߂�
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //����
        else if (judge == 0 && this.gameObject.transform.position.z < 0) {
            playGame.JudgementNote(this.gameObject, judge, "hold");
            Destroy(this.gameObject); 
        }
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //����J�n���ԁH
        {
            //���莞�Ԃ��߂����画��𑗂�
            if (IsReturnOverJudgeTime(5))
            {
                playGame.JudgementNote(this.gameObject, judge, "hold");
                isJudgeComp = true;
            }

            //�^�b�`����Ă��Ȃ��ꍇ�A��
            if (!isTouch) { return; }

            //Perfect,Great,Good��������ɍs��
            for (int i = 2; i >= 0; i--)
            {
                if (ReturnInJudgeTime(i) && judge > 2 - i)
                {
                    judge = 2 - i;
                    break;
                }
            }

            //perfect�̏ꍇ�A������o��
            if (judge == 0)
            {
                playGame.JudgementNote(this.gameObject, 0, "hold");
                isJudgeComp = true;
            }
        }
    }

    //������
    public void Init(PlayGame p, HoldNote h, float s)
    {
        playGame = p;
        hold = h;
        speed = s;
        deformable = GetComponentInChildren<Deformable>();
        //Debug.Log($"P�O:{hold.judge_time[2]} P:{hold.time} P��:{hold.judge_time[3]}");
    }

    //-----------------����֌W-------------------

    //���莞�ԓ����ǂ����Ԃ�
    private bool ReturnInJudgeTime(int num)
    {
        //if(num == 2) { Debug.Log($"�O:{hold.judge_time[num]} ,{IsReturnOverJudgeTime(num)} ��:{playGame.ReturnNowTime()} ��:{hold.judge_time[3]} ,{IsReturnOverJudgeTime(5 - num)}"); }
        if (IsReturnOverJudgeTime(num) && !IsReturnOverJudgeTime(5 - num)){ return true; }
        return false;
    }

    //���莞�Ԃ��߂������ǂ����Ԃ�
    private bool IsReturnOverJudgeTime(int num)
    {
        float now = playGame.ReturnNowTime();
        if(hold.judge_time[num] <= now) { return true; }
        return false;
    }

    //���X���C�_�[��������Ă��邩�ǂ���
    private bool IsTouchSlider()
    {
        //���背�[���S�Ăɂ��Ē��ׂ�
        for (int i = (int)hold.judge_lane[0]; i <= hold.judge_lane[1]; i++)
        { if (playGame.IsReturnSliderTouching(i)) { return true; } }
        return false;
    }

    //------------------�i�s�֌W-------------------

    /*
    //�m�[�g�̎��Ԃ�i�߂�
    private void TimeAdvance()
    {
        this.gameObject.transform.position += Vector3.forward * Time.fixedDeltaTime * speed;
    }
    */
}
