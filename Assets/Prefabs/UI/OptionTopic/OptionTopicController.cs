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
            int deltaValue = currentIndex - previousIndex;

            // 倍速
            animator.SetFloat("MoveSpeedMagnitude", Mathf.Abs(deltaValue));

            for (int i = 0; i < Mathf.Abs(deltaValue); i++)
            {
                if (deltaValue > 0) { animator.SetTrigger("Right"); }
                else { animator.SetTrigger("Left"); }

                isMoving = true;
                await UniTask.WaitUntil(() => !isMoving);

                SetOptionDatas(deltaValue > 0 ? previousIndex + i + 1 : previousIndex - i - 1, selectSceneDataGetter);
            }
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
        public void OnFinishMoveAnimation()
        {
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