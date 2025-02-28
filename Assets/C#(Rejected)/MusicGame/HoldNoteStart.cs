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
        //TimeAdvance();  //時を進める
        if (this.gameObject.transform.position.z < -50) { Destroy(this.gameObject); }  //仮※
        else if (IsReturnOverJudgeTime(0) && !isJudgeComp)  //判定開始時間？
        {
            if (IsReturnOverJudgeTime(5)) //判定時間を過ぎたらMiss判定を送る
            {
                playGame.JudgementNote(this.gameObject, 3, "hold");
                isJudgeComp = true;
            }
            else if (isTouch)   //対応するスライダー押された？
            {
                meshRenderer_ground.material = material[TOUCH_MATERIAL_NUM];
                //Perfect,Great,Good判定を順に行う
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

    //初期化
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

    //------------------進行関係-------------------
    
    /*
    //ノートの時間を進める
    private void TimeAdvance()
    {
        this.gameObject.transform.position += Vector3.forward * Time.fixedDeltaTime * speed;
    }
    */
}
