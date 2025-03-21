using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace Refactoring.UIInSelectScene
{
    public class SelectUIPresenter : MonoBehaviour
    {
        [SerializeField] MusicTopicControllerView musicTopicController_view;
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;

        IMusicDataGetter musicData_model;

        [Inject] 
        public void Construct(IMusicDataGetter musicDataGetter)
        {
            musicData_model = musicDataGetter;
        }

        private void Start()
        {
            Bind();
            SetEvent();
        }

        private void Bind()
        {
            // トピックの移動
            musicData_model?.MusicIndexSelected
                .Pairwise()
                .Subscribe(pair => _ = musicTopicController_view.OnChangeSelectedTopic(pair.Current - pair.Previous))
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
