using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using TransitionerInSelectScene;

public class Operation_MusicSelect : MonoBehaviour
{
    [Header("各項目に対応するスライダーUIの表示色と表示テキスト")]
    [SerializeField] Color musicSelectColor;
    [SerializeField] string musicSelectText = "楽曲選択";
    [SerializeField] Color rightMoveColor;
    [SerializeField] string rightMoveText = "右→";
    [SerializeField] Color leftMoveColor;
    [SerializeField] string leftMoveText = "←左";
    [SerializeField] Color difficultyUpColor;
    [SerializeField] string difficultyUpText = "難易度UP";
    [SerializeField] Color difficultyDownColor;
    [SerializeField] string difficultyDownText = "難易度DOWN";

    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
    [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;

    [Header("クールタイム")]
    [Tooltip("トピック移動中のクールタイム")]
    [SerializeField] float afterMovingCoolTime = 0.1f;
    [Tooltip("難易度変更後のクールタイム")]
    [SerializeField] float afterChangeDifficultyCoolTime = 0.1f;
    [Tooltip("最初に触れるようになるまでのクールタイム")]
    [SerializeField] float firstDelaySeconds = 0.5f;

    private int[] RIGHT_MOVE_INDICES = new int[] { 14, 15 };
    private int[] LEFT_MOVE_INDICES = new int[] { 0, 1 };
    private int[] DIFF_UP_INDICES = new int[] { 11, 12, 13 };
    private int[] DIFF_DOWN_INDICES = new int[] { 2, 3, 4 };
    private int[] MUSIC_SELECT_INDICES = new int[] { 5, 6, 7, 8, 9, 10 };

    IMusicDataListSetter musicDataListSetter;
    IMusicDataListGetter musicDataListGetter;

    [Inject]
    public void Construct(IMusicDataListSetter musicDataListSetter, IMusicDataListGetter musicDataListGetter)
    {
        this.musicDataListSetter = musicDataListSetter;
        this.musicDataListGetter = musicDataListGetter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        phaseStatusGetter?.Value.PhaseStatus
            .Where(value => value == PhaseStatusInSelectScene.MusicSelect)
            .Subscribe(_ => UpdateOperation())
            .AddTo(this.gameObject);
    }

    private void UpdateOperation()
    {
        operationSetter.Value.Dispose();

        // 少し入力許可を遅らせる
        _ = DelayedExecutor.ExecuteAfterDelay(firstDelaySeconds, () => SetOperation());
    }

    private void SetOperation()
    {
        // 楽曲選択
        operationSetter.Value.SetOperate(new SliderTouchData(MUSIC_SELECT_INDICES, TransitionNextPhase, musicSelectColor, musicSelectText));

        // 難易度変更
        SliderCoolDownHandler difficultyCoolDown = new SliderCoolDownHandler(afterMovingCoolTime);
        operationSetter.Value.SetOperate(new SliderTouchData(DIFF_UP_INDICES, () => ChangeDifficulty(+1), difficultyUpColor, difficultyUpText, difficultyCoolDown));
        operationSetter.Value.SetOperate(new SliderTouchData(DIFF_DOWN_INDICES, () => ChangeDifficulty(-1), difficultyDownColor, difficultyDownText, difficultyCoolDown));

        // トピックの移動
        SliderCoolDownHandler topicCoolDown = new SliderCoolDownHandler(afterMovingCoolTime);
        operationSetter.Value.SetOperate(new SliderTouchData(RIGHT_MOVE_INDICES, () => MoveMusicTopic(+1), rightMoveColor, rightMoveText, topicCoolDown));
        operationSetter.Value.SetOperate(new SliderTouchData(LEFT_MOVE_INDICES, () => MoveMusicTopic(-1), leftMoveColor, leftMoveText, topicCoolDown));
    }

    /// <summary>
    /// MusicTopicの移動
    /// </summary>
    /// <param name="index"></param>
    private void MoveMusicTopic(int delta)
    {
        musicDataListSetter.SetMusicIndex(musicDataListGetter.CurrentMusicIndex.Value + delta);
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
    /// 次のフェーズへの移動
    /// </summary>
    private void TransitionNextPhase()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect);
    }
}
