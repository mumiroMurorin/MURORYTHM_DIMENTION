using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using VContainer;
using System.Threading;

namespace UIInSelectScene
{
    public class OptionTopicController : MonoBehaviour
    {
        [SerializeField] List<OptionTypeToGameObject> optionPrefabList;
        [SerializeField] GameObject[] optionTopicParents;
        [SerializeField] RectTransform optionObjectParent;
        [SerializeField] Animator animator;

        IOptionGetter optionGetter;
        bool isMoving;
        bool isTopicSwapRequested;

        [Inject]
        public void Construct(IOptionGetter optionGetter)
        {
            this.optionGetter = optionGetter;
        }

        private void Start()
        {
            // オプションプレハブを一斉にインスタンス化
            foreach(var pair in optionPrefabList)
            {
                pair.InstantiatePrefab(optionGetter, optionObjectParent);
            }
        }

        /// <summary>
        /// 選択オプション変更
        /// </summary>
        /// <param name="deltaValue"></param>
        /// <returns></returns>
        public async UniTask OnChangeSelectedOption(int currentIndex, int previousIndex, ISelectSceneDataGetter selectSceneDataGetter)
        {
            int optionCount = selectSceneDataGetter?.OptionCount ?? 0;
            int lastIndex = optionCount - 1;
            bool canLoop = optionCount > 1;

            if (canLoop && previousIndex == lastIndex && currentIndex == 0)
            {
                await PlayMoveAnimation("RightLoop", currentIndex, selectSceneDataGetter);
                return;
            }

            if (canLoop && previousIndex == 0 && currentIndex == lastIndex)
            {
                await PlayMoveAnimation("LeftLoop", currentIndex, selectSceneDataGetter);
                return;
            }

            int deltaValue = currentIndex - previousIndex;

            for (int i = 0; i < Mathf.Abs(deltaValue); i++)
            {
                int nextIndex = deltaValue > 0 ? previousIndex + i + 1 : previousIndex - i - 1;
                await PlayMoveAnimation(deltaValue > 0 ? "Right" : "Left", nextIndex, selectSceneDataGetter);
            }
        }

        private async UniTask PlayMoveAnimation(string triggerName, int nextIndex, ISelectSceneDataGetter selectSceneDataGetter)
        {
            isMoving = true;
            isTopicSwapRequested = false;
            animator.SetTrigger(triggerName);

            await UniTask.WaitUntil(() => isTopicSwapRequested);
            SetOptionDatas(nextIndex, selectSceneDataGetter);

            await UniTask.WaitUntil(() => !isMoving);
        }

        /// <summary>
        /// トピックにオプションデータをセットする
        /// </summary>
        /// <param name="index"></param>
        /// <param name="musicDatas"></param>
        public void SetOptionDatas(int index, ISelectSceneDataGetter selectSceneDataGetter)
        {
            for (int i = 0; i < optionTopicParents.Length; i++)
            {
                int indexLocal = index - optionTopicParents.Length / 2 + i;
                OptionType type = selectSceneDataGetter.GetOptionType(indexLocal);

                // 端っこだった場合表示しない
                if (type == OptionType.None)
                {
                    optionTopicParents[i].SetActive(false);
                    continue;
                }

                optionTopicParents[i].SetActive(true);

                // 各親オブジェクトにインスタンス化したプレハブをセット
                foreach(var pair in optionPrefabList)
                {
                    GameObject obj = pair.CheckAndGetGameObject(type);
                    if (obj == null) { continue; }

                    pair.SetParent(optionTopicParents[i].transform);
                    break;
                }
            }
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
    
        [System.Serializable]
        public class OptionTypeToGameObject
        {
            [SerializeField] OptionType optionType;
            [SerializeField] GameObject prefab;

            private GameObject obj;

            public void InstantiatePrefab(IOptionGetter optionGetter, RectTransform parent = default)
            {
                if (obj) { Destroy(obj); }
                
                obj = Instantiate(prefab, parent);

                if (!obj.TryGetComponent(out IOptionTopicPresenter topic)) { Debug.Log("ふぁ！？"); return; }
                topic.Bind(optionGetter);
            }

            public void SetParent(Transform parent = default)
            {
                if (!obj) { return; }

                obj.transform.SetParent(parent);
                obj.transform.position = parent.position;
                obj.transform.rotation = parent.rotation;
                obj.transform.localScale = Vector3.one;
            }

            public GameObject CheckAndGetGameObject(OptionType type)
            {
                return type == optionType ? obj : null;
            }
        }
    }

}