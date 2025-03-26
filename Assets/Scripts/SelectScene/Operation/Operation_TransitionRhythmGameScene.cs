using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace Refactoring
{
    public class Operation_TransitionRhythmGameScene : MonoBehaviour
    {
        [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            phaseStatusGetter?.Value.PhaseStatus
                .Where(value => value == PhaseStatusInSelectScene.FadeOut || value == PhaseStatusInSelectScene.TransitionRhythmGameScene)
                .Subscribe(_ => UpdateOperation())
                .AddTo(this.gameObject);
        }

        private void UpdateOperation()
        {
            operationSetter.Value.Dispose();
        }
    }

}