using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using TransitionerInSelectScene;

public class Operation_Option : MonoBehaviour
{
    [Header("各項目に対応するスライダーUIの表示色と表示テキスト")]
    [SerializeField] Color backDetailColor;
    [SerializeField] string backDetailText = "戻る";
    [SerializeField] Color rightMoveColor;
    [SerializeField] string rightMoveText = "右→";
    [SerializeField] Color leftMoveColor;
    [SerializeField] string leftMoveText = "←左";
    [SerializeField] Color nextValueColor;
    [SerializeField] string nextValueText = "設定項目+";
    [SerializeField] Color previousValueColor;
    [SerializeField] string previousValueText = "設定項目-";

    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
    [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;
    [SerializeField] float delaySeconds = 0.5f;
    [SerializeField] float afterMovingCoolTime = 0.1f;
    [SerializeField] float afterChangeValueCoolTime = 0.1f;

    private int[] RIGHT_MOVE_INDICES = new int[] { 11, 12, 13 };
    private int[] LEFT_MOVE_INDICES = new int[] { 2, 3, 4 };
    private int[] NEXT_VALUE_INDICES = new int[] { 8, 9, 10 };
    private int[] PREVIOUS_VALUE_INDICES = new int[] { 5, 6, 7 };
    private int[] BACK_DETAILSELECT_INDICES = new int[] { 14, 15 };

    ISelectSceneDataGetter selectSceneDataGetter;
    ISelectSceneDataSetter selectSceneDataSetter;
    IOptionSetter optionSetter;

    [Inject]
    public void Construct(ISelectSceneDataGetter selectSceneDataGetter, ISelectSceneDataSetter selectSceneDataSetter, IOptionSetter optionSetter)
    {
        this.selectSceneDataGetter = selectSceneDataGetter;
        this.selectSceneDataSetter = selectSceneDataSetter;
        this.optionSetter = optionSetter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        phaseStatusGetter?.Value.PhaseStatus
            .Where(value => value == PhaseStatusInSelectScene.MusicOption)
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
        // 項目の移動
        SliderCoolDownHandler moveCoolDown = new SliderCoolDownHandler(afterMovingCoolTime);
        operationSetter.Value.SetOperate(new SliderTouchData(RIGHT_MOVE_INDICES, () => MoveOptionTopic(+1), rightMoveColor, rightMoveText, moveCoolDown));
        operationSetter.Value.SetOperate(new SliderTouchData(LEFT_MOVE_INDICES, () => MoveOptionTopic(-1), leftMoveColor, leftMoveText, moveCoolDown));

        // 値の加減算
        SliderCoolDownHandler changeValueCoolDown = new SliderCoolDownHandler(afterChangeValueCoolTime);
        operationSetter.Value.SetOperate(new SliderTouchData(NEXT_VALUE_INDICES, () => ChangeTopicValue(+1), nextValueColor, nextValueText, changeValueCoolDown));
        operationSetter.Value.SetOperate(new SliderTouchData(PREVIOUS_VALUE_INDICES, () => ChangeTopicValue(-1), previousValueColor, previousValueText, changeValueCoolDown));

        // 元に戻る
        operationSetter.Value.SetOperate(new SliderTouchData(BACK_DETAILSELECT_INDICES, TransitionDetailSelectPhase, backDetailColor, backDetailText));
    }

    /// <summary>
    /// OptionTopicの移動
    /// </summary>
    /// <param name="index"></param>
    private void MoveOptionTopic(int delta)
    {
        selectSceneDataSetter.SetOptionIndex(selectSceneDataGetter.CurrentOptionIndex.Value + delta);
    }

    private void ChangeTopicValue(int delta)
    {
        OptionType currentType = selectSceneDataGetter.GetOptionType(selectSceneDataGetter.CurrentOptionIndex.Value);
        optionSetter.SetOption(currentType, delta);
    }

    /// <summary>
    /// 楽曲確認画面に戻る
    /// </summary>
    private void TransitionDetailSelectPhase()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect);
    }
}

