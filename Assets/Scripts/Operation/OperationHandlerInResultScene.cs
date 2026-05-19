using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;

public class OperationHandlerInResultScene : MonoBehaviour
{
    [Label("操作アセットリスト")]
    [SerializeField] OperationListInScene operations;

    [SerializeField] OperationDictionary operationDictionary;
    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<TransitionerInResultScene.IPhaseStatusGetterInResultScene> phaseStatusGetter;

    CancellationTokenSource cts;

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        phaseStatusGetter?.Value?.PhaseStatus
            .Subscribe(OnChangePhase)
            .AddTo(this.gameObject);
    }

    /// <summary>
    /// フェーズチェンジの際、該当する操作があるかチェックしてあったらセットする
    /// </summary>
    /// <param name="phase"></param>
    public void OnChangePhase(PhaseStatusInResultScene phase)
    {
        foreach (var assets in operations.AssetsList)
        {
            // シーンとフェーズが該当？
            if (assets.CheckCondition(phase))
            {
                // 操作の破棄
                operationSetter.Value.Dispose();

                cts?.CancelAndDispose();
                cts = DelayUtility.Run(assets.DelaySeconds, () => { SetOperation(assets); });

                return;
            }
        }

        // 操作の破棄
        operationSetter.Value.Dispose();
    }

    /// <summary>
    /// そのフェーズ内の操作を全てセット
    /// </summary>
    /// <param name="assets"></param>
    private void SetOperation(OperationInPhase assets)
    {
        // そのフェーズの操作をセット
        foreach (var group in assets.AssetGroups)
        {
            var coolDownHandler = group.SliderCoolDownHandler;

            foreach (var asset in group.Operations)
            {
                operationSetter?.Value?.SetOperate(new SliderTouchData(asset, operationDictionary.GetOperation(asset.Tag), operations.TextTableReference, coolDownHandler));
            }
        }
    }

    private void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}


