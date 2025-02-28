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
        //�f�[�^�̈��p��
        if (!isFindMusicData)
        {
            gmd = GameObject.Find("GiveMusicData").GetComponent<GiveMusicData>();
            playGame.SetMusicGameOption(gmd.ReturnNoteSpeed(), gmd.ReturnNoteOffset());
            playGame.SetSliderOption(gmd.ReturnRootOption());
            playGame.SetKinectOption(gmd.ReturnRootOption());
            playGame.SetSpaceSensitivityOption(gmd.ReturnRootOption());
            isFindMusicData = true;
        }
        //CSV�A���y�t�@�C���̓ǂݍ��݁A��������UI�̃Z�b�g
        else if (isFindMusicData && !isLoadProcessComp)
        {
            readData.FirstFunc(gmd.GetChart());
            audioCtrl.AudioLoad(gmd.GetMusicClip());
            uiCtrl.FirstFunc(gmd.ReturnMusicdata());
            isLoadProcessComp = true;
        }
        //CSV�̓ǂݍ��݊���������LoadData�Ƀf�[�^�𑗂����
        else if (readData.isReturnReady() && audioCtrl.IsReturnLoadcomp() && !isFadeIn)
        {
            playGame.SetGameData(readData.ReturnGameData());    //�f�[�^�̃Z�b�g
            playGame.GenerateNoteInAdvance();   //�m�[�g���O�ǂݍ���
            uiCtrl.FadeInStart();        //�t�F�[�h�C��
            isFadeIn = true;
        }
        //�t�F�[�h�C����A�̔F���A�j���[�V�����̒���
        else if (uiCtrl.IsReturnFadeInFinish() && !isRecogAnim)
        {
            uiCtrl.RecogAnimStart();
            isRecogAnim = true;
        }
        //�̂̔F��������
        else if (playGame.IsReturnKinectTracking() && uiCtrl.IsReturnFinishOpenRecog() && !isCompRecog)
        {
            uiCtrl.RecogAnimFinish();
            isCompRecog = true;
        }
        //�uReady?�v
        else if (uiCtrl.IsReturnFinishCloseRecog() && !isStartReadyAnim)
        {
            uiCtrl.ReadyAnimStart();     //�uReady?�v
            isStartReadyAnim = true;
        }
        //�Q�[���X�^�[�g
        else if (uiCtrl.IsReturnFinishReady() && !isStartGame)
        {
            audioCtrl.PlayAudioSource(); //���y�Đ�
            playGame.StartGame();        //�Q�[���X�^�[�g
            isStartGame = true;
        }
        //�Ō�̃m�[�g����������A���o�A�j���[�V�����𒃓�
        else if (isStartGame && playGame.IsReturnJudgeLastNote() && !isStartFinishAnim)
        {
            ComboRank combo_rank = playGame.ReturnComboRank();
            //�A�j���[�V�����̒���
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
        //�A�j���[�V�����I����A�t�F�[�h�A�E�g
        else if (!isFadingOut && uiCtrl.IsReturnFinishAssessment())
        {
            uiCtrl.FadeOutStart();
            isFadingOut = true;
        }
        //�V�[����J��
        else if (uiCtrl.IsReturnFadeOutFinish())
        {
            ChangeToResultScene();
        }
    }

    //���U���g�V�[���ւ̑J��
    private void ChangeToResultScene()
    {
        SceneManager.LoadScene("ResultScene");
    }
}
