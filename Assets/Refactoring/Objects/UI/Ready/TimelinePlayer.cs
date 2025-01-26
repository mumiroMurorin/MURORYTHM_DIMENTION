using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

namespace Refactoring
{
    public class TimelinePlayer : MonoBehaviour, ITimelinePlayer
    {
        [Header("���o�^�C�����C��")]
        [SerializeField] PlayableDirector playableDirector;

        CancellationTokenSource cts;

        void ITimelinePlayer.PlayAnimation(Action callback)
        {
            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, callback).Forget();
        }

        /// <summary>
        /// �A�j���[�V�����̍Đ�
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if (playableDirector == null)
            {
                callback.Invoke();
                return;
            }

            playableDirector.gameObject.SetActive(true);
            playableDirector.Play();

            try
            {
                playableDirector.Play();

                // �^�C�����C���̍Đ����I�����邩�L�����Z�������܂őҋ@
                await UniTask.WaitUntil(() => playableDirector.state != PlayState.Playing, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("�ySystem�z�^�C�����C���̍Đ����L�����Z������܂���");
            }

            callback.Invoke();
        }
    }

}