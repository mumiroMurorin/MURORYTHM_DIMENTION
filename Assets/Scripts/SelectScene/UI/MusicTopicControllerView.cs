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
        [SerializeField] MusicTopicUI[] musicTopicUIs;

        bool isMoving;

        /// <summary>
        /// 選択楽曲変更
        /// </summary>
        /// <param name="deltaValue"></param>
        /// <returns></returns>
        public async UniTask OnChangeSelectedMusic(int deltaValue)
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
        /// 難易度変更
        /// </summary>
        /// <param name="difficulty"></param>
        public void OnChangeDifficulty(Difficulty difficulty)
        {
            foreach(var musicTopic in musicTopicUIs)
            {
                musicTopic.SetDifficulty(difficulty);
            }
        }
        
        /// <summary>
        /// 楽曲選択時
        /// </summary>
        public void OnSelectMusic()
        {
            animator.SetTrigger("Select");
        }

        /// <summary>
        /// 楽曲選択画面に戻る
        /// </summary>
        public void OnBackSelectPhase()
        {
            animator.SetTrigger("SelectBack");
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