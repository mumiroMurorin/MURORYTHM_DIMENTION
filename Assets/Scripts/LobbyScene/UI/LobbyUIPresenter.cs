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
            // ‘€ì‚Ì’Ç‰Á
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value =>
                {
                    SetInteractionSliderEvent(value.Value);
                    sliderUnitsController_view?.OnChangeSliderData(value.Value);
                    topicTextsController_view?.OnChangeSliderData(value.Value);
                })
                .AddTo(this.gameObject);

            // ‘€ì‚ÌˆêV
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveReset()
                .Subscribe(_ =>
                {
                    sliderUnitsController_view?.OnClearSliderData();
                    topicTextsController_view?.OnClearSliderData();
                })
                .AddTo(this.gameObject);
        }

        private void SetInteractionSliderEvent(SliderTouchData sliderTouchData)
        {
            sliderTouchData.AddCallback(() => sliderUnitsController_view.OnTouchSlider(sliderTouchData));
        }
    }

}
