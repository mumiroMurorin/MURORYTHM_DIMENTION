using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TransitionerInLobbyScene;
using VContainer;
using UniRx;

namespace UIInLobbyScene
{
    public class LobbyUIPresenter : MonoBehaviour
    {
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SliderTopicTextsControllerView topicTextsController_view;

        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInLobbyScene> phaseStatusGetter_model;

        private void Start()
        {
            BindSliderUI();
        }

        private void BindSliderUI()
        {
            // タッチされた時のUI挙動
            operationGetter_model?.Value.OnTouchSliderListner
                .Subscribe(sliderUnitsController_view.OnTouchSlider)
                .AddTo(this.gameObject);

            // 操作の追加
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value =>
                {
                    sliderUnitsController_view?.OnChangeSliderData(value.Value);
                    topicTextsController_view?.OnChangeSliderData(value.Value);
                })
                .AddTo(this.gameObject);

            // 操作の一新
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveReset()
                .Subscribe(_ =>
                {
                    sliderUnitsController_view?.OnClearSliderData();
                    topicTextsController_view?.OnClearSliderData();
                })
                .AddTo(this.gameObject);
        }
    }

}
