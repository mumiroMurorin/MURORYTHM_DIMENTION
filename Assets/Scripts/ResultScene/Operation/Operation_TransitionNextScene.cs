using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using TransitionerInResultScene;

public class Operation_TransitionNextScene : MonoBehaviour
{
    [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
    [SerializeField] SerializeInterface<IPhaseStatusGetterInResultScene> phaseStatusGetter;

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        phaseStatusGetter?.Value.PhaseStatus
            .Where(value => value == PhaseStatusInResultScene.FadeOut)
            .Subscribe(_ => UpdateOperation())
            .AddTo(this.gameObject);
    }

    private void UpdateOperation()
    {
        operationSetter.Value.Dispose();
    }
}
