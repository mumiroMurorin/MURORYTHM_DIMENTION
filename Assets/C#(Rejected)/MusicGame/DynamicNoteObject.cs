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
        TimeAdvance();  //時を進める
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //仮※
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //判定開始時間？
        {
            //判定時間を過ぎたら判定を送る(※一旦消滅)
            if (IsReturnOverJudgeTime(5))
            {
                playGame.JudgementNote(this.gameObject, judge, "dynamic_space");
                isJudgeComp = true;
                Destroy(this.gameObject);
            }

            //アクションされていない場合帰れ
            if (!isAction) { return; }

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
                playGame.JudgementNote(this.gameObject, 0, "dynamic_space");
                isJudgeComp = true;
            }
        }
    }

    //初期化
    public void Init(PlayGame p, DynamicNote d, float s)
    {
        playGame = p;
        dynamic = d;
        speed = s;
    }

    //-----------------判定関係-------------------

    //判定時間内かどうか返す
    private bool ReturnInJudgeTime(int num)
    {
        if (IsReturnOverJudgeTime(num) && !IsReturnOverJudgeTime(5 - num)) { return true; }
        return false;
    }

    //判定時間を過ぎたかどうか返す
    private bool IsReturnOverJudgeTime(int num)
    {
        float now = playGame.ReturnNowTime();
        if (dynamic.judge_time[num] <= now) { return true; }
        return false;
    }

    //宙アクションが行われているかどうか
    private bool IsSpaceAction()
    {
        return playGame.IsReturnSpaceAction(dynamic.judge_vector);
    }

    //------------------進行関係-------------------

    //ノートの時間を進める
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }

}
