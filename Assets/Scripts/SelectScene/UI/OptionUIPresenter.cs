using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using VContainer;
using TransitionerInSelectScene;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace UIInSelectScene
{
    public class OptionUIPresenter : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] GameObject[] optionTopicPrefabs;
        [SerializeField] GameObject[] topics;
        [SerializeField] OptionTopicControllerView optionTopicController_view;
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter_model;

        ISelectSceneDataGetter selectSceneDataGetter_model;
        bool isMoving;

        [Inject]
        public void Construct(ISelectSceneDataGetter selectSceneDataGetter)
        {
            selectSceneDataGetter_model = selectSceneDataGetter;
        }

        void Start()
        {
            BindOptionTopic();
        }

        private void BindOptionTopic()
        {
            // オプションの選択
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicOption)
                .Subscribe(_ => {
                    optionTopicController_view.OnSelectOption();
                })
                .AddTo(this.gameObject);

            // オプションから戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Pairwise()
                .Where(pair => pair.Previous == PhaseStatusInSelectScene.MusicOption && pair.Current == PhaseStatusInSelectScene.DetailSelect)
                .Subscribe(_ => {
                    optionTopicController_view.OnBackDetailSelectPhase();
                })
                .AddTo(this.gameObject);
        }

        ///// <summary>
        ///// 選択楽曲変更
        ///// </summary>
        ///// <param name="deltaValue"></param>
        ///// <returns></returns>
        //public async UniTask OnChangeSelectedMusic(int currentIndex, int previousIndex, ISelectSceneDataGetter selectSceneDataGetter)
        //{
        //    int deltaValue = currentIndex - previousIndex;

        //    // 倍速
        //    animator.SetFloat("MoveSpeedMagnitude", Mathf.Abs(deltaValue));

        //    for (int i = 0; i < Mathf.Abs(deltaValue); i++)
        //    {
        //        if (deltaValue > 0) { animator.SetTrigger("Right"); }
        //        else { animator.SetTrigger("Left"); }

        //        isMoving = true;
        //        await UniTask.WaitUntil(() => !isMoving);

        //        SetMusicDatas(deltaValue > 0 ? previousIndex + i + 1 : previousIndex - i - 1, selectSceneDataGetter);
        //    }
        //}

        ///// <summary>
        ///// トピックに楽曲データをセットする
        ///// </summary>
        ///// <param name="index"></param>
        ///// <param name="musicDatas"></param>
        //public void SetMusicDatas(int index, ISelectSceneDataGetter selectSceneDataGetter)
        //{
        //    for (int i = 0; i < topics.Length; i++)
        //    {
        //        int indexLocal = index - topics.Length / 2 + i;
        //        MusicData data = selectSceneDataGetter.GetMusicData(indexLocal);

        //        if (data == null)
        //        {
        //            musicTopicUIs[i].SetObjActive(false);
        //            continue;
        //        }

        //        musicTopicUIs[i].SetObjActive(true);
        //        musicTopicUIs[i].SetMusicTopic(data);
        //    }

        //}
    }

    public interface IOptionTopicPresenter
    {
        //public void Bind(INoteSpawnDataOptionHolder)
    }
}

