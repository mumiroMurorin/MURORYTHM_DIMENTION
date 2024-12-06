using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicNoteObject : MonoBehaviour
{
    PlayGame playGame;
    DynamicNote dynamic;

    private bool isAction;
    private bool isJudgeComp;
    private int judge = 3; //0p 1g 2g 3m
    private float speed;

    private void Update()
    {
        isAction = IsSpaceAction();
    }

    void FixedUpdate()
    {
        TimeAdvance();  //����i�߂�
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //����
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //����J�n���ԁH
        {
            //���莞�Ԃ��߂����画��𑗂�(����U����)
            if (IsReturnOverJudgeTime(5))
            {
                playGame.JudgementNote(this.gameObject, judge, "dynamic_space");
                isJudgeComp = true;
                Destroy(this.gameObject);
            }

            //�A�N�V��������Ă��Ȃ��ꍇ�A��
            if (!isAction) { return; }

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
                playGame.JudgementNote(this.gameObject, 0, "dynamic_space");
                isJudgeComp = true;
            }
        }
    }

    //������
    public void Init(PlayGame p, DynamicNote d, float s)
    {
        playGame = p;
        dynamic = d;
        speed = s;
    }

    //-----------------����֌W-------------------

    //���莞�ԓ����ǂ����Ԃ�
    private bool ReturnInJudgeTime(int num)
    {
        if (IsReturnOverJudgeTime(num) && !IsReturnOverJudgeTime(5 - num)) { return true; }
        return false;
    }

    //���莞�Ԃ��߂������ǂ����Ԃ�
    private bool IsReturnOverJudgeTime(int num)
    {
        float now = playGame.ReturnNowTime();
        if (dynamic.judge_time[num] <= now) { return true; }
        return false;
    }

    //���A�N�V�������s���Ă��邩�ǂ���
    private bool IsSpaceAction()
    {
        return playGame.IsReturnSpaceAction(dynamic.judge_vector);
    }

    //------------------�i�s�֌W-------------------

    //�m�[�g�̎��Ԃ�i�߂�
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }

}
