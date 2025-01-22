using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_StartAnimation : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [Header("�X�^�[�g���o�^�C�����C��")]
        [SerializeField] PlayableDirector startPlayable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.StartAnimation;
        CancellationTokenSource cts;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("�yTransition�zTransition to \"StartAnimation\"");
            
            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, TransitionNextPhase).Forget();
        }

        /// <summary>
        /// �X�^�[�g�A�j���[�V�����̍Đ�
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if(startPlayable == null) 
            {
                callback.Invoke();
                return; 
            }
            startPlayable.Play();

            try
            {
                // �^�C�����C�����Đ�
                startPlayable.Play();

                // �^�C�����C���̍Đ����I�����邩�L�����Z�������܂őҋ@
                await UniTask.WaitUntil(() => startPlayable.state != PlayState.Playing, cancellationToken: token);
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
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.Play);
        }
    }

}
