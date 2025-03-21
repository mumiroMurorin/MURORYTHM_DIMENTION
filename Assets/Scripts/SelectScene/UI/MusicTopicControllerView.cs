using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Refactoring.UIInSelectScene
{
    public class MusicTopicControllerView : MonoBehaviour
    {
        [SerializeField] Animator animator;

        bool isMoving;

        public async UniTask OnChangeSelectedTopic(int deltaValue)
        {
            // 倍速
            animator.SetFloat("MoveSpeedMagnitude", Mathf.Abs(deltaValue));

            for(int i = 0; i < Mathf.Abs(deltaValue); i++)
            {
                if(deltaValue > 0) { animator.SetTrigger("Right"); }
                else { animator.SetTrigger("Left"); }

                isMoving = true;
                await UniTask.WaitUntil(() => !isMoving);
            }
        }
        
        /// <summary>
        /// アニメーション側から呼ばれる
        /// </summary>
        public void OnFinishMoveAnimation()
        {
            isMoving = false;
        }
    }

}