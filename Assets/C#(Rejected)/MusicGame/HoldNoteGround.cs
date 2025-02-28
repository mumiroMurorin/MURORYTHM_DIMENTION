using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

public class HoldNoteGround : MonoBehaviour
{
    const int DEFAULT_MATERIAL_NUM = 0;
    const int TOUCH_MATERIAL_NUM = 1;
    const int MISS_MATERIAL_NUM = 2;

    Material[] material;    //[0]: default [1]: touch [2]: miss
    MeshRenderer meshRenderer;
    PlayGame playGame;
    HoldNote hold;
    HoldNoteGround holdNoteGround_next;
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
        if (this.gameObject.transform.position.z < -100) { Destroy(this.gameObject); }  //����
        else if (hold.isJudge && IsReturnOverJudgeTime(0) && !isJudgeComp)  //����J�n���ԁH
        {
            //���莞�Ԃ��߂����画��𑗂�
            if (IsReturnOverJudgeTime(5))
            {
                playGame.JudgementNote(this.gameObject, judge, "hold");
                isJudgeComp = true;
            }

            //�^�b�`����Ă��Ȃ��ꍇ�A��
            if (!isTouch) {
                meshRenderer.material = material[DEFAULT_MATERIAL_NUM];
                return; 
            }

            //�}�e���A���̐ݒ�
            meshRenderer.material = material[TOUCH_MATERIAL_NUM];

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
    public void Init(PlayGame p, HoldNote h, float s, Material[] mate)
    {
        playGame = p;
        hold = h;
        speed = s;
        material = mate;
        deformable = GetComponentInChildren<Deformable>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshRenderer.material = material[DEFAULT_MATERIAL_NUM];
        //if (hold.next != null && !hold.next.isGoal)
        //{
        //    holdNoteGround_next = hold.next.obj.GetComponentInParent<HoldNoteGround>();
        //}
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

    //�}�e���A����\��ւ���(�㑱�̃m�[�g�����ׂ�)
    public void ChangeMaterial(int num)
    {
       meshRenderer.material = material[num];
       if (hold.next != null && !hold.next.isGoal) { holdNoteGround_next.ChangeMaterial(num); }
    }

    /*
    //�m�[�g�̎��Ԃ�i�߂�
    private void TimeAdvance()
    {
        this.gameObject.transform.position += Vector3.forward * Time.fixedDeltaTime * speed;
    }
    */
}
