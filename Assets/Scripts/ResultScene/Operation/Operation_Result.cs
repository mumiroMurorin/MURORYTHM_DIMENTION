using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using TransitionerInResultScene;

public class Operation_Result : MonoBehaviour
{
    [Header("各項目に対応するスライダーUIの表示色")]
    [SerializeField] Color finishResultColor;
    [SerializeField] string finishResultText = "楽曲選択へ戻る";

    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
    [SerializeField] SerializeInterface<IPhaseStatusGetterInResultScene> phaseStatusGetter;
    [SerializeField] float delaySeconds = 0.5f;

    private int[] RESULT_SKIP_INDICES = new int[] { 4, 5, 6, 7, 8, 9, 10, 11 };

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        phaseStatusGetter?.Value.PhaseStatus
            .Where(value => value == PhaseStatusInResultScene.Result)
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
        operationSetter.Value.SetOperate(new SliderTouchData(RESULT_SKIP_INDICES, TransitionNextPhase, finishResultColor, finishResultText));
    }

    /// <summary>
    /// 次のフェーズへの移動
    /// </summary>
    private void TransitionNextPhase()
    {
        phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.FadeOut);
    }
}
