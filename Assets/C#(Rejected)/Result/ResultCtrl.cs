using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultCtrl : MonoBehaviour
{
    [SerializeField] private SliderInput slider;
    [SerializeField] private ResultUI uiCtrl;
    [SerializeField] private TouchUI touchUI;
    [SerializeField] private ResultAudioCtrl audioCtrl;

    private int[] FINISH_MUSIC_SENSOR_NUMS = new int[16] { 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 };
    private bool isFindMusicData;
    private bool isSetResultUI;
    private bool isTouch;
    
    GiveMusicData gmd;

    void Update()
    {
        //データの引継ぎ
        if (!isFindMusicData)
        {
            gmd = GameObject.Find("GiveMusicData").GetComponent<GiveMusicData>();
            slider.SetRootOption(gmd.ReturnRootOption());
            isFindMusicData = true;
        }
        else if (isFindMusicData && !isSetResultUI) 
        {
            touchUI.SetFlagColors(new int[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, "null");
            touchUI.SetFlagColors(FINISH_MUSIC_SENSOR_NUMS,"main");
            audioCtrl.PlayResultBGM();
            uiCtrl.SetMusicData(gmd.ReturnMusicdata(), gmd.ReturnDifficulity(), gmd.ReturnDifficulityColor32(gmd.ReturnDifficulity()));
            uiCtrl.SetResultData(gmd.ReturnRecord());
            uiCtrl.FadeInStart();
            isSetResultUI = true;
        }
        else if (uiCtrl.IsReturnFadeOutFinish())
        {
            ChangeToSelectScene();
        }
        InputFunc();//入力
    }

    //入力関係
    private void InputFunc()
    {
        //リザルト表示後、タッチを待つ
        if (!isTouch && slider.IsReturnSlidersTouching(FINISH_MUSIC_SENSOR_NUMS) && uiCtrl.IsReturnFadeInFinish())
        {
            touchUI.TouchFlag(FINISH_MUSIC_SENSOR_NUMS);
            uiCtrl.FadeOutStart();
            audioCtrl.PlayTouchSE();
            audioCtrl.FadeOutResultBGM();
            isTouch = true;
        }
    }

    //音楽セレクトに遷移
    private void ChangeToSelectScene()
    {
        SceneManager.LoadScene("MusicSelectScene");
    }
}
