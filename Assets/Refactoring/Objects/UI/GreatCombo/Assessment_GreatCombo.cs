using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Refactoring
{
    public class Assessment_GreatCombo : MonoBehaviour, IAssessmentPlayer
    {
        [Header("�I�����o�^�C�����C��")]
        [SerializeField] PlayableDirector endPlayable;

        readonly ComboRank rank = ComboRank.GreatCombo;
        CancellationTokenSource cts;

        bool IAssessmentPlayer.ConditionChecker(ComboRank rank)
        {
            return rank == this.rank;
        }

        void IAssessmentPlayer.PlayAnimation(Action callback)
        {
            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, callback).Forget();
        }

        /// <summary>
        /// �G���h�A�j���[�V�����̍Đ�
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if (endPlayable == null)
            {
                callback.Invoke();
                return;
            }

            endPlayable.gameObject.SetActive(true);
            endPlayable.Play();

            try
            {
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
    }

}
