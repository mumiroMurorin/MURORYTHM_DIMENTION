using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using UniRx;

public class SelectMusic : MonoBehaviour
{
    [Header("曲リスト")]
    [SerializeField] private List<MusicData> musicData_list;

    [SerializeField] private SelectUI selectUI;
    [SerializeField] private RootUI rootUI;
    [SerializeField] private SelectAudioCtrl audioCtrl;
    [SerializeField] private SliderInput slider;
    [SerializeField] private TouchUI touchUI;
    [SerializeField] private FadeAnim fade;

    private GiveMusicData gmd;
    private int state_num;  //0:待機(特になし) 1:セレクト中 2:決定
    private int state_option_num = 0;
    private int note_speed = 45;
    private int note_offset = 0;
    const int OPTION_TOPIC_NUM = 2;
    const int STATE_MUSIC_SELECTING = 1;
    const int STATE_MUSIC_DESISION = 2;
    const int STATE_FADE_OUT = 3;
    const int STATE_ROOT = -1;

    //それぞれのボタンリスト
    private int[] RIGHT_MOVE_SENSOR_NUMS = new int[2] { 12, 13 };
    private int[] LEFT_MOVE_SENSOR_NUMS = new int[2] { 2, 3 };
    private int[] DIFF_DOWN_SENSOR_NUMS = new int[1] { 0 };
    private int[] DIFF_UP_SENSOR_NUMS = new int[1] { 15 };
    private int[] MUSIC_SELECT_SENSOR_NUMS = new int[6] { 5, 6, 7, 8, 9, 10 };
    private int[] OPTION_NEXT_SENSOR_NUMS = new int[2] { 12, 13 };
    private int[] OPTION_BACK_SENSOR_NUMS = new int[2] { 2, 3 };
    private int[] OPTION_PLUS_SENSOR_NUMS = new int[3] { 8, 9, 10 };
    private int[] OPTION_MINUS_SENSOR_NUMS = new int[3] { 5, 6, 7 };
    private int[] BACK_SELECT_SENSOR_NUMS = new int[2] { 0, 15 };
    private int[] MUSIC_START_SENSOR_NUMS = new int[6] { 5, 6, 7, 8, 9, 10 };

    private static int center_index;
    private static DifficulityName select_diff;
    private static bool isMakeGMD;
    private bool isInit;
    private bool isMovingTopic;

    void Start()
    {
        state_num = STATE_MUSIC_SELECTING;

        Bind();
    }

    void Update()
    {
        if (!isInit)
        {
            if (isMakeGMD) 
            { 
                gmd = GameObject.Find("GiveMusicData").GetComponent<GiveMusicData>();
                rootUI.Init(gmd.ReturnRootOption());
                slider.SetRootOption(gmd.ReturnRootOption());
                NoteSpeedChange((int)(gmd.ReturnNoteSpeed() * 10));
                OffsetChange(gmd.ReturnNoteOffset());
            }
            else
            {
                NoteSpeedChange(note_speed);
                OffsetChange(note_offset);
            }

            selectUI.Init();
            UpdateMusicTopic(center_index, select_diff);
            ChangeMusicTheme(musicData_list[center_index]);
            SetTouchUISelectMusic();
            fade.FadeIn();
            isInit = true;
        }
        else if(fade.IsReturnFadeInFinish())
        {
            InputFunc();
            if (state_num == STATE_FADE_OUT && fade.IsReturnFadeOutFinish()) { ChangeToMusicScene(); }
        }
    }

    private void Bind()
    {
        // 難易度＋
        foreach (int index in DIFF_UP_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => UpDifficulty())
                .AddTo(this.gameObject);
        }

        // 難易度-
        foreach (int index in DIFF_DOWN_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => DownDifficulty())
                .AddTo(this.gameObject);
        }

        // 次オプション
        foreach (int index in OPTION_NEXT_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => NextOption())
                .AddTo(this.gameObject);
        }

        // 前オプション
        foreach (int index in OPTION_BACK_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => BackOption())
                .AddTo(this.gameObject);
        }

        // 楽曲選択に戻る
        foreach (int index in BACK_SELECT_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => BackSelectingMusic())
                .AddTo(this.gameObject);
        }

        // 設定+
        foreach (int index in OPTION_PLUS_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => PlusOptionValue())
                .AddTo(this.gameObject);
        }

        // 設定-
        foreach (int index in OPTION_MINUS_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => MinusOptionValue())
                .AddTo(this.gameObject);
        }

        // 楽曲決定
        foreach (int index in MUSIC_START_SENSOR_NUMS)
        {
            slider.GetSliderReactiveProperty(index)
                .Where(isTouch => isTouch)
                .Subscribe(_ => DesicionMusic())
                .AddTo(this.gameObject);
        }
    }

    private void UpDifficulty()
    {
        if (state_num != STATE_MUSIC_SELECTING) { return; }
        if (isMovingTopic) { return; }
        if ((int)select_diff >= Enum.GetNames(typeof(DifficulityName)).Length - 1) { return; }

        touchUI.TouchFlag(DIFF_UP_SENSOR_NUMS);
        ChangeDifficulity(++select_diff);
    }

    private void DownDifficulty()
    {
        if (state_num != STATE_MUSIC_SELECTING) { return; }
        if (isMovingTopic) { return; }
        if ((int)select_diff <= 0) { return; }

        touchUI.TouchFlag(DIFF_DOWN_SENSOR_NUMS);
        ChangeDifficulity(--select_diff);
    }

    private void NextOption()
    {
        if (state_num != STATE_MUSIC_SELECTING) { return; }
        if (isMovingTopic) { return; }

        touchUI.TouchFlag(OPTION_NEXT_SENSOR_NUMS);
        ChangeSelectOption(true);
    }

    private void BackOption()
    {
        if (state_num != STATE_MUSIC_SELECTING) { return; }
        if (isMovingTopic) { return; }

        touchUI.TouchFlag(OPTION_BACK_SENSOR_NUMS);
        ChangeSelectOption(false);
    }

    private void BackSelectingMusic()
    {
        if (state_num != STATE_MUSIC_DESISION) { return; }
        if (isMovingTopic) { return; }

        touchUI.TouchFlag(BACK_SELECT_SENSOR_NUMS);
        BackMusicSelect();
    }

    private void PlusOptionValue()
    {
        if (state_num != STATE_MUSIC_DESISION) { return; }
        if (state_option_num == 0) { return; }
        if (isMovingTopic) { return; }

        touchUI.TouchFlag(OPTION_PLUS_SENSOR_NUMS);
        ChangeOption(true);
    }

    private void MinusOptionValue()
    {
        if (state_num != STATE_MUSIC_DESISION) { return; }
        if (state_option_num != 0) { return; }
        if (isMovingTopic) { return; }

        touchUI.TouchFlag(OPTION_MINUS_SENSOR_NUMS);
        ChangeOption(false);
    }

    private void DesicionMusic()
    {
        if (state_num != STATE_MUSIC_DESISION) { return; }
        if (state_option_num != 0) { return; }
        if (isMovingTopic) { return; }

        touchUI.TouchFlag(MUSIC_START_SENSOR_NUMS);
        MusicDesision();
    }

    //入力関係
    private void InputFunc()
    {
        //左移動
        if (slider.IsReturnSlidersTouching(LEFT_MOVE_SENSOR_NUMS) && center_index > 0 && state_num == STATE_MUSIC_SELECTING && !isMovingTopic)
        {
            touchUI.TouchFlag(LEFT_MOVE_SENSOR_NUMS);
            MoveMusicTopic(false);
        }
        //右移動
        else if (slider.IsReturnSlidersTouching(RIGHT_MOVE_SENSOR_NUMS) && center_index < ReturnMusicDataCount() - 1 && state_num == STATE_MUSIC_SELECTING && !isMovingTopic)
        {
            touchUI.TouchFlag(RIGHT_MOVE_SENSOR_NUMS);
            MoveMusicTopic(true);
        }
        //音楽決定
        else if (slider.IsReturnSlidersTouching(MUSIC_SELECT_SENSOR_NUMS) && state_num == STATE_MUSIC_SELECTING && !isMovingTopic
            && ReturnMusicData(center_index).GetDifficulity(select_diff) != 0)
        {
            touchUI.TouchFlag(MUSIC_SELECT_SENSOR_NUMS);
            SelectMusicTopic();
        }
        //ここから楽曲決定系


        //管理者画面関係
        if (Input.GetKeyDown(KeyCode.Escape) && state_num == STATE_MUSIC_SELECTING && !isMovingTopic) {
            DisRootCanvas(true);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && state_num == STATE_ROOT && !isMovingTopic) {
            DisRootCanvas(false);
        }
    }

    //タッチUIの色変更
    private void SetTouchUISelectMusic()
    {
        touchUI.SetFlagColors(new int[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, "null");
        touchUI.SetFlagColors(MUSIC_SELECT_SENSOR_NUMS, "main");
        touchUI.SetFlagColors(LEFT_MOVE_SENSOR_NUMS, "horizon");
        touchUI.SetFlagColors(RIGHT_MOVE_SENSOR_NUMS, "horizon");
        touchUI.SetFlagColors(DIFF_UP_SENSOR_NUMS, "up");
        touchUI.SetFlagColors(DIFF_DOWN_SENSOR_NUMS, "down");
    }

    private void SetTouchUIDecisionMusic()
    {
        touchUI.SetFlagColors(new int[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, "null");
        touchUI.SetFlagColors(MUSIC_START_SENSOR_NUMS, "main");
        touchUI.SetFlagColors(OPTION_NEXT_SENSOR_NUMS, "horizon");
        touchUI.SetFlagColors(OPTION_BACK_SENSOR_NUMS, "horizon");
        touchUI.SetFlagColors(BACK_SELECT_SENSOR_NUMS, "down");
    }

    private void SetTouchUIMusicOption()
    {
        touchUI.SetFlagColors(new int[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, "null");
        touchUI.SetFlagColors(OPTION_NEXT_SENSOR_NUMS, "horizon");
        touchUI.SetFlagColors(OPTION_BACK_SENSOR_NUMS, "horizon");
        touchUI.SetFlagColors(OPTION_PLUS_SENSOR_NUMS, "up");
        touchUI.SetFlagColors(OPTION_MINUS_SENSOR_NUMS, "down");
        touchUI.SetFlagColors(BACK_SELECT_SENSOR_NUMS, "down");
    }

    //-----------------MusicTopic関係-----------------

    //曲移動
    private void MoveMusicTopic(bool isRight)
    {
        isMovingTopic = true;
        audioCtrl.PlaySelectAudio();
        selectUI.MoveMusicTopicAnim(isRight);
    }

    //トピック移動終了時の呼び出しイベント
    public void FinishMoveTopic(bool isRight)
    {
        if (isRight) { 
            center_index = Mathf.Clamp(++center_index, 0, ReturnMusicDataCount());
        }
        else { 
            center_index = Mathf.Clamp(--center_index, 0, ReturnMusicDataCount());
        }
        UpdateMusicTopic(center_index, select_diff);
        ChangeMusicTheme(musicData_list[center_index]);
        isMovingTopic = false;
    }

    //トピックアニメーション終了時の呼び出しイベント
    public void FinishAnimEvent()
    {
        isMovingTopic = false;
    }

    //MusicTopicの更新
    private void UpdateMusicTopic(int index, DifficulityName diff)
    {
        for (int i = 0; i < selectUI.ReturnMusicTopicNum(); i++)
        {
            if (index + i - 3 < 0) {
                selectUI.SetMusicTopicActive(i, false);
                continue; }
            else if(index + i - 3 >= ReturnMusicDataCount()) {
                selectUI.SetMusicTopicActive(i, false);
                continue; }
            selectUI.SetMusicTopicActive(i, true);
            selectUI.SetMusicDataToTopic(i, diff, ReturnMusicData(index + i - 3));
        }
    }

    //MusicTopicの選択
    private void SelectMusicTopic()
    {
        isMovingTopic = true;
        state_option_num = 0;
        audioCtrl.PlayBackAudio();
        selectUI.SelectMusicTopicAnim(true);
        state_num = STATE_MUSIC_DESISION;
        selectUI.DisOption(true);
        SetTouchUIDecisionMusic();
    }

    //表示難易度の変更
    private void ChangeDifficulity(DifficulityName diff)
    {
        audioCtrl.PlayUpDownAudio();
        UpdateMusicTopic(center_index, select_diff);
    }

    //楽曲のテーマに変更
    private void ChangeMusicTheme(MusicData data)
    {
        selectUI.ChangeBackTheme(data.ThemeSprite);
        audioCtrl.PlayMusic(data.SampleClip);
    }

    //---------------楽曲最終決定----------------

    //選択画面に戻る
    private void BackMusicSelect()
    {
        isMovingTopic = true;
        state_num = STATE_MUSIC_SELECTING;
        audioCtrl.PlayBackAudio();
        selectUI.SelectMusicTopicAnim(false);
        selectUI.DisOption(false);
        selectUI.PointOffsetOption(false);
        selectUI.PointNoteSpeedOption(false);
        SetTouchUISelectMusic();
    }

    //次前オプションへ移る
    private void ChangeSelectOption(bool isNext)
    {
        audioCtrl.PlaySelectAudio();
        state_option_num = Mathf.Clamp(isNext ? ++state_option_num : --state_option_num, -OPTION_TOPIC_NUM / 2, OPTION_TOPIC_NUM / 2);
        switch (state_option_num)
        {
            case -1://スピード設定
                selectUI.PointNoteSpeedOption(true);
                SetTouchUIMusicOption();
                break;
            case 0://ゲーム開始
                selectUI.PointOffsetOption(false);
                selectUI.PointNoteSpeedOption(false);
                SetTouchUIDecisionMusic();
                break;
            case 1://オフセット設定
                selectUI.PointOffsetOption(true);
                SetTouchUIMusicOption();
                break;
            default:
                Debug.LogError("オプショントピックの数以上以下の引数が入ってきたよ: " + state_option_num);
                break;
        }
    }

    //オプション変更
    private void ChangeOption(bool isPlus)
    {
        audioCtrl.PlayUpDownAudio();
        switch (state_option_num)
        {
            case -1://スピード設定
                NoteSpeedChange(isPlus ? note_speed + 1 : note_speed - 1);
                break;
            case 0://ゲーム開始
                Debug.LogError("それはおかしいやろ");
                break;
            case 1://オフセット設定
                OffsetChange(isPlus ? note_offset + 1 : note_offset - 1);
                break;
            default:
                Debug.LogError("オプショントピックの数以上以下の引数が入ってきたよ: " + state_option_num);
                break;
        }
    }

    //ノートスピード変更
    private void NoteSpeedChange(int speed)
    {
        if (speed < 20 || speed > 75) { return; }
        note_speed = speed;
        selectUI.SetNoteSpeedTMP((float)speed / 10f);
    }

    //オフセット変更
    private void OffsetChange(int offset)
    {
        if (offset < -50 || offset > 50) { return; }
        note_offset = offset;
        selectUI.SetOffsetTMP(offset);
    }

    //最終決定
    private void MusicDesision()
    {
        //管理者設定が完了していない
        if (!rootUI.IsReturnSetRootOption())
        {
            StartCoroutine(selectUI.DisPopUp());
            return;
        }
        audioCtrl.PlayDecisionAudio();
        state_num = STATE_FADE_OUT;
        fade.FadeOut();
    }

    //音楽データを返す
    public MusicData ReturnMusicData(int index)
    {
        if (index >= musicData_list.Count)
        {
            Debug.LogError("溢れたお: " + index);
            return null;
        }
        return musicData_list[index];
    }

    //音楽データの大きさを返す
    public int ReturnMusicDataCount() { return musicData_list.Count; }

    //-----------------root関係-------------------

    //管理者画面の表示非表示
    private void DisRootCanvas(bool isDis)
    {
        rootUI.DisRootScreen(isDis);
        if (isDis) { state_num = STATE_ROOT; }
        else 
        {
            state_num = STATE_MUSIC_SELECTING;
            if (rootUI.IsReturnSetRootOption()) { slider.SetRootOption(rootUI.ReturnRootOption()); }
        }
    }

    //--------------------GMD--------------------

    //GMDにデータをのせて音楽シーンに遷移
    private void ChangeToMusicScene()
    {
        if (isMakeGMD)
        {
            gmd.InitToMusicScene(ReturnMusicData(center_index), select_diff, (float)note_speed / 10f, note_offset, rootUI.ReturnRootOption());
        }
        else
        {
            GenerateGiveMusicDataObj(ReturnMusicData(center_index), select_diff);
            isMakeGMD = true;
        }
        SceneManager.LoadScene("MusicScene");
    }

    //音楽ゲームシーンに渡すインタンス生成
    private GameObject GenerateGiveMusicDataObj(MusicData data, DifficulityName diff)
    {
        GameObject obj = new GameObject("GiveMusicData");
        obj.AddComponent<GiveMusicData>().InitToMusicScene(data, diff, (float)note_speed / 10f, note_offset, rootUI.ReturnRootOption());
        return obj;
    }
}