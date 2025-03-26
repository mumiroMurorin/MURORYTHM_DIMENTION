using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using VContainer;

namespace Refactoring.UIInSelectScene
{
    public class SelectUIPresenter : MonoBehaviour
    {
        [SerializeField] MusicTopicControllerView musicTopicController_view;
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter_model;

        ISelectSceneDataGetter selectSceneDataGetter_model;

        [Inject] 
        public void Construct(ISelectSceneDataGetter selectSceneDataGetter)
        {
            selectSceneDataGetter_model = selectSceneDataGetter;
        }

        private void Start()
        {
            Bind();
            SetEvent();
        }

        private void Bind()
        {
            // トピックの移動
            selectSceneDataGetter_model?.CurrentSelectIndex
                .Pairwise()
                .Subscribe(pair => _ = musicTopicController_view.OnChangeSelectedMusic(pair.Current, pair.Previous, selectSceneDataGetter_model))
                .AddTo(this.gameObject);

            // 楽曲の選択(決定)
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.DetailSelect)
                .Subscribe(_ => musicTopicController_view.OnSelectMusic())
                .AddTo(this.gameObject);

            // 楽曲選択に戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicSelect)
                .Subscribe(_ => musicTopicController_view.OnBackSelectPhase())
                .AddTo(this.gameObject);

            // 難易度の変更
            selectSceneDataGetter_model?.Difficulty
                .Subscribe(musicTopicController_view.OnChangeDifficulty)
                .AddTo(this.gameObject);

            // スライダーUI
            // 操作の追加
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value => sliderUnitsController_view.OnChangeSliderData(value.Value))
                .AddTo(this.gameObject);

            // 操作の一新
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveReset()
                .Subscribe(_ => sliderUnitsController_view.OnClearSliderData())
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {

        }
    }
}
