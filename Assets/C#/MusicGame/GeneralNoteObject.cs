using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralNoteObject : MonoBehaviour
{
    PlayGame playGame;
    GeneralNote general;

    private bool isTouch;
    private bool isJudgeComp;
    private float speed;

    private void Update()
    {
        isTouch = IsTouchSlider();
    }

    void FixedUpdate()
    {
        TimeAdvance();  //時を進める
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //仮※
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //判定開始時間？
        {
            if (IsReturnOverJudgeTime(5)) //判定時間を過ぎたらMiss判定を送り消滅
            {
                playGame.JudgementNote(this.gameObject, 3, "general");
                isJudgeComp = true;
                Destroy(this.gameObject);
            }
            else if (isTouch)   //対応するスライダー押された？
            {
                //Perfect,Great,Good判定を順に行う
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

    //初期化
    public void Init(PlayGame p, GeneralNote g, float s)
    {
        playGame = p;
        general = g;
        speed = s;
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
        if(general.judge_time[num] <= now) { return true; }
        return false;
    }

    //判スライダーが押されたかどうか
    private bool IsTouchSlider()
    {
        //判定レーン全てについて調べる
        for (int i = general.judge_lane[0]; i <= general.judge_lane[1]; i++){
            if (playGame.IsReturnSliderFirstTouch(i)) { return true; }
        }
        return false;
    }

    //------------------進行関係-------------------
    
    //ノートの時間を進める
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }

}
