using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_EndAnimation : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [Header("�I�����o�^�C�����C��")]
        [SerializeField] PlayableDirector endPlayable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.EndAnimation;
        CancellationTokenSource cts;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("�yTransition�zTransition to \"EndAnimation\"");

            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, TransitionNextPhase).Forget();
        }

        /// <summary>
        /// �X�^�[�g�A�j���[�V�����̍Đ�
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if (endPlayable == null)
            {
                callback.Invoke();
                return;
            }
            endPlayable.Play();

            try
            {
                // �^�C�����C�����Đ�
                endPlayable.Play();

                // �^�C�����C���̍Đ����I�����邩�L�����Z�������܂őҋ@
                await UniTask.WaitUntil(() => endPlayable.state != PlayState.Playing, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("�yTransition�z�^�C�����C���̍Đ����L�����Z������܂���");
            }

            callback.Invoke();
        }

        /// <summary>
        /// ���̃t�F�[�Y�ւ̈ړ�
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.TransitionResultScene);
        }
    }

}
