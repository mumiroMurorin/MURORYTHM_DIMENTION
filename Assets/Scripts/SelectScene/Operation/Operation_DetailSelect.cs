using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using TransitionerInSelectScene;

public class Operation_DetailSelect : MonoBehaviour
{
    [Header("各項目に対応するスライダーUIの表示色と表示テキスト")]
    [SerializeField] Color musicStartColor;
    [SerializeField] string musicStartText = "楽曲スタート！";
    [SerializeField] Color musicUnstartableColor;
    [SerializeField] string musicUnstartableText = "";
    [SerializeField] Color backSelectColor;
    [SerializeField] string backSelectText = "楽曲選択に戻る";
    [SerializeField] Color difficultyUpColor;
    [SerializeField] string difficultyUpText = "難易度UP";
    [SerializeField] Color difficultyDownColor;
    [SerializeField] string difficultyDownText = "難易度DOWN";
    [SerializeField] Color optionColor;
    [SerializeField] string optionText = "設定";

    [Header("クールタイム")]
    [Tooltip("難易度変更後のクールタイム")]
    [SerializeField] float afterChangeDifficultyCoolTime = 0.1f;
    [Tooltip("最初に触れるようになるまでのクールタイム")]
    [SerializeField] float delaySeconds = 0.5f;

    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
    [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;

    private int[] OPTION_INDICES = new int[] { 14, 15 };
    private int[] BACK_SELECT_INDICES = new int[] { 0, 1 };
    private int[] MUSIC_START_INDICES = new int[] { 5, 6, 7, 8, 9, 10 };
    private int[] DIFF_UP_INDICES = new int[] { 11, 12, 13 };
    private int[] DIFF_DOWN_INDICES = new int[] { 2, 3, 4 };

    IMusicDataListGetter musicDataListGetter;
    IMusicDataListSetter musicDataListSetter;
    IMusicDataSetter musicDataSetter;

    [Inject]
    public void Construct(IMusicDataSetter musicDataSetter, IMusicDataListGetter musicDataListGetter, IMusicDataListSetter musicDataListSetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.musicDataListGetter = musicDataListGetter;
        this.musicDataListSetter = musicDataListSetter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        phaseStatusGetter?.Value.PhaseStatus
            .Where(value => value == PhaseStatusInSelectScene.DetailSelect)
            .Subscribe(_ => UpdateOperation())
            .AddTo(this.gameObject);
    }

    private void UpdateOperation()
    {
        operationSetter.Value.Dispose();

        // 少し入力許可を遅らせる
        _ = DelayedExecutor.ExecuteAfterDelay(delaySeconds, () => SetOperation());
    }

    private void SetOperation()
    {
        // 楽曲スタート
        var startMusicTouchData = new SliderTouchData(MUSIC_START_INDICES, TransitionRhythmGamePhase, musicStartColor, musicStartText);
        operationSetter.Value.SetOperate(startMusicTouchData);
        // 難易度変更で更新
        musicDataListGetter.Difficulty
            .Subscribe(diff => {
                UpdateMusicStartTopic(startMusicTouchData);
            })
            .AddTo(this.gameObject);

        // 楽曲選択に戻る
        operationSetter.Value.SetOperate(new SliderTouchData(BACK_SELECT_INDICES, TransitionMusicSelectPhase, backSelectColor, backSelectText));

        // 難易度変更
        SliderCoolDownHandler DifficultyCoolDown = new SliderCoolDownHandler(afterChangeDifficultyCoolTime);
        operationSetter.Value.SetOperate(new SliderTouchData(DIFF_UP_INDICES, () => ChangeDifficulty(+1), difficultyUpColor, difficultyUpText, DifficultyCoolDown));
        operationSetter.Value.SetOperate(new SliderTouchData(DIFF_DOWN_INDICES, () => ChangeDifficulty(-1), difficultyDownColor, difficultyDownText, DifficultyCoolDown));

        // オプション選択
        operationSetter.Value.SetOperate(new SliderTouchData(OPTION_INDICES, TransitionOptionPhase, optionColor, optionText));
    }

    private void UpdateMusicStartTopic(SliderTouchData sliderTouchData)
    {
        Difficulty difficulty = musicDataListGetter.Difficulty.Value;
        int numOfDifficulty = musicDataListGetter.CurrentMusicData.Value.GetDifficulity(difficulty);

        // 譜面がない場合
        if(numOfDifficulty == -1)
        {
            sliderTouchData.SetImageColor(musicUnstartableColor);
            sliderTouchData.SetText(musicUnstartableText);
            sliderTouchData.DisposeAction();
        }
        // 譜面がある場合
        else
        {
            sliderTouchData.SetImageColor(musicStartColor);
            sliderTouchData.SetText(musicStartText);
            sliderTouchData.DisposeAction();
            sliderTouchData.AddCallback(TransitionRhythmGamePhase);
        }
    }

    /// <summary>
    /// 難易度の変更
    /// </summary>
    /// <param name="diff"></param>
    private void ChangeDifficulty(int delta)
    {
        musicDataListSetter.SetDifficulty(musicDataListGetter.Difficulty.Value + delta);
    }

    /// <summary>
    /// 楽曲選択に戻る
    /// </summary>
    private void TransitionMusicSelectPhase()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicSelect);
    }

    /// <summary>
    /// オプション
    /// </summary>
    private void TransitionOptionPhase()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicOption);
    }

    /// <summary>
    /// 楽曲決定
    /// </summary>
    private void TransitionRhythmGamePhase()
    {
        // 難易度と楽曲の確定
        musicDataSetter.SetDifficulty(musicDataListGetter.Difficulty.Value);
        musicDataSetter.SetMusicData(musicDataListGetter.CurrentMusicData.Value);

        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeOut);
    }
}

