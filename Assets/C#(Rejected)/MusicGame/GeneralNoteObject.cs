using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class GeneralNoteObject : MonoBehaviour
{
    PlayGame playGame;
    GeneralNote general;

    private bool isTouch;
    private bool isJudgeComp;
    private float speed;

    void FixedUpdate()
    {
        TimeAdvance();  //����i�߂�
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //����
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //����J�n���ԁH
        {
            if (IsReturnOverJudgeTime(5)) //���莞�Ԃ��߂�����Miss����𑗂����
            {
                playGame.JudgementNote(this.gameObject, 3, "general");
                isJudgeComp = true;
                Destroy(this.gameObject);
            }
            else if (isTouch)   //�Ή�����X���C�_�[�����ꂽ�H
            {
                //Perfect,Great,Good��������ɍs��
                for (int i = 2; i >= 0; i--)
                {
                    if (ReturnInJudgeTime(i)){
                        playGame.JudgementNote(this.gameObject, 2 - i, "general");
                        break;
                    }
                }
                Destroy(this.gameObject);
            }
        }

        isTouch = false;
    }

    //������
    public void Init(PlayGame p, GeneralNote g, float s)
    {
        playGame = p;
        general = g;
        speed = s;

        Bind();
    }

    private void Bind()
    {
        for (int i = general.judge_lane[0]; i <= general.judge_lane[1]; i++)
        {
            playGame.GetSliderInputReactiveProperty(i)
                .Where(isTouch => isTouch)
                .Subscribe(_ => isTouch = true)
                .AddTo(this.gameObject);

        }
    }

    //-----------------����֌W-------------------

    //���莞�ԓ����ǂ����Ԃ�
    private bool ReturnInJudgeTime(int num)
    {
        if (IsReturnOverJudgeTime(num) && !IsReturnOverJudgeTime(5 - num)){ return true; }
        return false;
    }

    //���莞�Ԃ��߂������ǂ����Ԃ�
    private bool IsReturnOverJudgeTime(int num)
    {
        float now = playGame.ReturnNowTime();
        if(general.judge_time[num] <= now) { return true; }
        return false;
    }

    //------------------�i�s�֌W-------------------
    
    //�m�[�g�̎��Ԃ�i�߂�
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }

}
