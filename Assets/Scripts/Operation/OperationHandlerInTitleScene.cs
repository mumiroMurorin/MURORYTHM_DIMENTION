using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;

public class OperationHandlerInTitleScene : MonoBehaviour
{
    [Label("操作アセットリスト")]
    [SerializeField] OperationListInScene operations;

    [SerializeField] float delaySeconds = 0.5f;
    [SerializeField] OperationDictionary operationDictionary;
    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<TransitionerInTitleScene.IPhaseStatusGetterInTitleScene> phaseStatusGetter;

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

        // フェードアウトしたら操作を破棄
        phaseStatusGetter?.Value?.PhaseStatus
                .Where(value => value == PhaseStatusInTitleScene.FadeOut)
                .Subscribe(_ => operationSetter.Value.Dispose())
                .AddTo(this.gameObject);
    }

    /// <summary>
    /// フェーズチェンジの際、該当する操作があるかチェックしてあったらセットする
    /// </summary>
    /// <param name="phase"></param>
    public void OnChangePhase(PhaseStatusInTitleScene phase)
    {
        foreach (var assets in operations.AssetsList)
        {
            // シーンとフェーズが該当？
            if (assets.CheckCondition(phase))
            {
                cts?.CancelAndDispose();
                cts = DelayUtility.Run(delaySeconds, () => { SetOperation(assets); });

                break;
            }
        }
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
                operationSetter?.Value?.SetOperate(new SliderTouchData(asset, operationDictionary.GetOperation(asset.Tag), coolDownHandler));
            }
        }
    }

    private void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}
