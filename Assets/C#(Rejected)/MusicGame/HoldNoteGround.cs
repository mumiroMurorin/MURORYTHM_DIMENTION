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
        //TimeAdvance();  //時を進める
        if (this.gameObject.transform.position.z < -100) { Destroy(this.gameObject); }  //仮※
        else if (hold.isJudge && IsReturnOverJudgeTime(0) && !isJudgeComp)  //判定開始時間？
        {
            //判定時間を過ぎたら判定を送る
            if (IsReturnOverJudgeTime(5))
            {
                playGame.JudgementNote(this.gameObject, judge, "hold");
                isJudgeComp = true;
            }

            //タッチされていない場合帰れ
            if (!isTouch) {
                meshRenderer.material = material[DEFAULT_MATERIAL_NUM];
                return; 
            }

            //マテリアルの設定
            meshRenderer.material = material[TOUCH_MATERIAL_NUM];

            //Perfect,Great,Good判定を順に行う
            for (int i = 2; i >= 0; i--)
            {
                if (ReturnInJudgeTime(i) && judge > 2 - i)
                {
                    judge = 2 - i;
                    break;
                }
            }

            //perfectの場合、即判定出す
            if (judge == 0)
            {
                playGame.JudgementNote(this.gameObject, 0, "hold");
                isJudgeComp = true;
            }
        }
    }

    //初期化
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

    //-----------------判定関係-------------------

    //判定時間内かどうか返す
    private bool ReturnInJudgeTime(int num)
    {
        if (IsReturnOverJudgeTime(num) && !IsReturnOverJudgeTime(5 - num)){ return true; }
        return false;
    }

    //判定時間を過ぎたかどうか返す
    private bool IsReturnOverJudgeTime(int num)
    {
        float now = playGame.ReturnNowTime();
        if(hold.judge_time[num] <= now) { return true; }
        return false;
    }

    //判スライダーが押されているかどうか
    private bool IsTouchSlider()
    {
        //判定レーン全てについて調べる
        for (int i = (int)hold.judge_lane[0]; i <= hold.judge_lane[1]; i++)
        { if (playGame.IsReturnSliderTouching(i)) { return true; } }
        return false;
    }

    //------------------進行関係-------------------

    //マテリアルを貼り替える(後続のノートもすべて)
    public void ChangeMaterial(int num)
    {
       meshRenderer.material = material[num];
       if (hold.next != null && !hold.next.isGoal) { holdNoteGround_next.ChangeMaterial(num); }
    }

    /*
    //ノートの時間を進める
    private void TimeAdvance()
    {
        this.gameObject.transform.position += Vector3.forward * Time.fixedDeltaTime * speed;
    }
    */
}
