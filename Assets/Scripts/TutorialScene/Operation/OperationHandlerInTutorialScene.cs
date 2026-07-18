using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UniRx;
using UnityEngine;

public class OperationHandlerInTutorialScene : MonoBehaviour
{
    [Label("Operation Asset List")]
    [SerializeField] OperationListInScene operations;

    [SerializeField] OperationDictionary operationDictionary;
    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<TransitionerInTutorialScene.IPhaseStatusGetterInTutorialScene> phaseStatusGetter;

    CancellationTokenSource cts;

    void Start()
    {
        Bind();
    }

    void Bind()
    {
        phaseStatusGetter?.Value?.PhaseStatus
            .Subscribe(OnChangePhase)
            .AddTo(gameObject);
    }

    public void OnChangePhase(PhaseStatusInTutorialScene phase)
    {
        if (operations == null || operationSetter?.Value == null) { return; }

        foreach (var assets in operations.AssetsList)
        {
            if (assets.CheckCondition(phase))
            {
                operationSetter.Value.Dispose();

                cts?.CancelAndDispose();
                cts = DelayUtility.Run(assets.DelaySeconds, () => { SetOperation(assets); });
                return;
            }
        }

        operationSetter.Value.Dispose();
    }

    void SetOperation(OperationInPhase assets)
    {
        foreach (var group in assets.AssetGroups)
        {
            var coolDownHandler = group.SliderCoolDownHandler;

            foreach (var asset in group.Operations)
            {
                var operation = operationDictionary != null ? operationDictionary.GetOperation(asset.Tag) : null;
                operationSetter.Value.SetOperate(new SliderTouchData(asset, operation, operations.TextTableReference, coolDownHandler));
            }
        }
    }

    void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}
