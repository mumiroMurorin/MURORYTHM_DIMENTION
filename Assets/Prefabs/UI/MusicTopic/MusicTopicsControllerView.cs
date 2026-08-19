using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace UIInSelectScene
{
    public class MusicTopicsControllerView : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] MusicTopicController[] musicTopicUIs;

        bool isMoving;
        bool isTopicSwapRequested;

        /// <summary>
        /// 選択楽曲変更
        /// </summary>
        /// <param name="deltaValue"></param>
        /// <returns></returns>
        public async UniTask OnChangeSelectedMusic(int currentIndex, int previousIndex, IMusicDataListGetter dataGetter)
        {
            int musicCount = dataGetter?.MusicDatasSorted?.Count ?? 0;
            int lastIndex = musicCount - 1;
            bool canLoop = musicCount > 1;

            if (canLoop && previousIndex == lastIndex && currentIndex == 0)
            {
                await PlayMoveAnimation("RightLoop", currentIndex, dataGetter);
                return;
            }

            if (canLoop && previousIndex == 0 && currentIndex == lastIndex)
            {
                await PlayMoveAnimation("LeftLoop", currentIndex, dataGetter);
                return;
            }

            int deltaValue = currentIndex - previousIndex;

            for(int i = 0; i < Mathf.Abs(deltaValue); i++)
            {
                int nextIndex = deltaValue > 0 ? previousIndex + i + 1 : previousIndex - i - 1;
                await PlayMoveAnimation(deltaValue > 0 ? "Right" : "Left", nextIndex, dataGetter);
            }
        }

        private async UniTask PlayMoveAnimation(string triggerName, int nextIndex, IMusicDataListGetter dataGetter)
        {
            isMoving = true;
            isTopicSwapRequested = false;
            animator.SetTrigger(triggerName);

            await UniTask.WaitUntil(() => isTopicSwapRequested);
            SetMusicDatas(nextIndex, dataGetter);

            await UniTask.WaitUntil(() => !isMoving);
        }

        /// <summary>
        /// トピックに楽曲データをセットする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="musicDatas"></param>
        public void SetMusicDatas(int index, IMusicDataListGetter dataGetter)
        {
            for (int i = 0; i < musicTopicUIs.Length; i++) 
            {
                int indexLocal = index - musicTopicUIs.Length / 2 + i;
                var data = dataGetter.GetMusicData(indexLocal);

                if (data == null)
                {
                    musicTopicUIs[i].SetObjActive(false);
                    continue;
                }

                musicTopicUIs[i].SetObjActive(true);
                musicTopicUIs[i].SetMusicTopic(data);
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
        /// オプション選択時
        /// </summary>
        public void OnSelectOption()
        {
            animator.SetTrigger("SelectOption");
        }

        /// <summary>
        /// 楽曲確認画面に戻る
        /// </summary>
        public void OnBackDetailSelectPhase()
        {
            animator.SetTrigger("CloseOption");
        }

        /// <summary>
        /// アニメーション側から呼ばれる
        /// </summary>
        public void OnRequestTopicSwap()
        {
            isTopicSwapRequested = true;
        }

        /// <summary>
        /// アニメーション側から呼ばれる
        /// </summary>
        public void OnFinishMoveAnimation()
        {
            if (!isTopicSwapRequested)
            {
                isTopicSwapRequested = true;
            }

            isMoving = false;
        }
    }

}