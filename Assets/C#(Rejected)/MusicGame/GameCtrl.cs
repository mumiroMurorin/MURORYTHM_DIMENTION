using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameCtrl : MonoBehaviour
{
    [SerializeField] private ReadData readData;
    [SerializeField] private PlayGame playGame;
    [SerializeField] private AudioCtrl audioCtrl;
    [SerializeField] private UICtrl uiCtrl;

    GiveMusicData gmd;

    private bool isFindMusicData;
    private bool isLoadProcessComp;
    private bool isFadeIn;
    private bool isRecogAnim;
    private bool isCompRecog;
    private bool isStartReadyAnim;
    private bool isStartGame;
    private bool isStartFinishAnim;
    private bool isFadingOut;

    // Update is called once per frame
    void Update()
    {
        //データの引継ぎ
        if (!isFindMusicData)
        {
            gmd = GameObject.Find("GiveMusicData").GetComponent<GiveMusicData>();
            playGame.SetMusicGameOption(gmd.ReturnNoteSpeed(), gmd.ReturnNoteOffset());
            playGame.SetSliderOption(gmd.ReturnRootOption());
            playGame.SetKinectOption(gmd.ReturnRootOption());
            playGame.SetSpaceSensitivityOption(gmd.ReturnRootOption());
            isFindMusicData = true;
        }
        //CSV、音楽ファイルの読み込み、初期化とUIのセット
        else if (isFindMusicData && !isLoadProcessComp)
        {
            readData.FirstFunc(gmd.GetChart());
            audioCtrl.AudioLoad(gmd.GetMusicClip());
            uiCtrl.FirstFunc(gmd.ReturnMusicdata());
            isLoadProcessComp = true;
        }
        //CSVの読み込み完了したらLoadDataにデータを送りつける
        else if (readData.isReturnReady() && audioCtrl.IsReturnLoadcomp() && !isFadeIn)
        {
            playGame.SetGameData(readData.ReturnGameData());    //データのセット
            playGame.GenerateNoteInAdvance();   //ノート事前読み込み
            uiCtrl.FadeInStart();        //フェードイン
            isFadeIn = true;
        }
        //フェードイン後、体認識アニメーションの茶道
        else if (uiCtrl.IsReturnFadeInFinish() && !isRecogAnim)
        {
            uiCtrl.RecogAnimStart();
            isRecogAnim = true;
        }
        //体の認識完了後
        else if (playGame.IsReturnKinectTracking() && uiCtrl.IsReturnFinishOpenRecog() && !isCompRecog)
        {
            uiCtrl.RecogAnimFinish();
            isCompRecog = true;
        }
        //「Ready?」
        else if (uiCtrl.IsReturnFinishCloseRecog() && !isStartReadyAnim)
        {
            uiCtrl.ReadyAnimStart();     //「Ready?」
            isStartReadyAnim = true;
        }
        //ゲームスタート
        else if (uiCtrl.IsReturnFinishReady() && !isStartGame)
        {
            audioCtrl.PlayAudioSource(); //音楽再生
            playGame.StartGame();        //ゲームスタート
            isStartGame = true;
        }
        //最後のノートが処理され、演出アニメーションを茶道
        else if (isStartGame && playGame.IsReturnJudgeLastNote() && !isStartFinishAnim)
        {
            ComboRank combo_rank = playGame.ReturnComboRank();
            //アニメーションの茶道
            switch (combo_rank)
            {
                case ComboRank.TrackComplete://Comp
                    uiCtrl.TrackCompAnimStart();
                    break;
                case ComboRank.FullCombo://FC
                    uiCtrl.FullComboAnimStart();
                    break;
                case ComboRank.AllPerfect://AP
                    uiCtrl.AllPerfectAnimStart();
                    break;
            }

            MusicRecord record = new MusicRecord 
            { 
                score = playGame.ReturnGameScore(), 
                combo_rank = combo_rank, 
                rank = playGame.ReturnScoreRank(playGame.ReturnGameScore()), 
                judge_num = playGame.ReturnJudgeNums()
            };

            gmd.InitToResultScene(record);
            isStartFinishAnim = true;
        }
        //アニメーション終了後、フェードアウト
        else if (!isFadingOut && uiCtrl.IsReturnFinishAssessment())
        {
            uiCtrl.FadeOutStart();
            isFadingOut = true;
        }
        //シーンを遷移
        else if (uiCtrl.IsReturnFadeOutFinish())
        {
            ChangeToResultScene();
        }
    }

    //リザルトシーンへの遷移
    private void ChangeToResultScene()
    {
        SceneManager.LoadScene("ResultScene");
    }
}
