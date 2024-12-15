using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class HoldNoteStart : MonoBehaviour
{
    const int DEFAULT_MATERIAL_NUM = 0;
    const int TOUCH_MATERIAL_NUM = 1;
    const int MISS_MATERIAL_NUM = 2;

    PlayGame playGame;
    GeneralNote general;
    MeshRenderer meshRenderer_ground;
    Material[] material;    //[0]: default [1]: touch [2]: miss

    private bool isTouch;
    private bool isJudgeComp;
    private float speed;

    void FixedUpdate()
    {
        //TimeAdvance();  //����i�߂�
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //����
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //����J�n���ԁH
        {
            if (IsReturnOverJudgeTime(5)) //���莞�Ԃ��߂�����Miss����𑗂�
            {
                playGame.JudgementNote(this.gameObject, 3, "hold");
                isJudgeComp = true;
            }
            else if (isTouch)   //�Ή�����X���C�_�[�����ꂽ�H
            {
                meshRenderer_ground.material = material[TOUCH_MATERIAL_NUM];
                //Perfect,Great,Good��������ɍs��
                for (int i = 2; i >= 0; i--)
                {
                    if (ReturnInJudgeTime(i)){
                        playGame.JudgementNote(this.gameObject, 2 - i, "hold");
                        isJudgeComp = true;
                        break;
                    }
                }
            }
        }

        isTouch = false;
    }

    //������
    public void Init(PlayGame p, GeneralNote g, GameObject ground, Material[] mate, float s)
    {
        playGame = p;
        general = g;
        speed = s;
        material = mate;
        meshRenderer_ground = ground.GetComponentInChildren<MeshRenderer>();
        meshRenderer_ground.material = material[DEFAULT_MATERIAL_NUM];

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
    
    /*
    //�m�[�g�̎��Ԃ�i�߂�
    private void TimeAdvance()
    {
        this.gameObject.transform.position += Vector3.forward * Time.fixedDeltaTime * speed;
    }
    */
}
