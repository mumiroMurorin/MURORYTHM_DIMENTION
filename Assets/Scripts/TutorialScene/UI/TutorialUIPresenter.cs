using UniRx;
using UnityEngine;

namespace UIInTutorialScene
{
    public class TutorialUIPresenter : MonoBehaviour
    {
        [SerializeField] private SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] private SliderTopicTextsControllerView topicTextsController_view;
        [SerializeField] private SerializeInterface<IOperationGetter> operationGetter_model;

        private void Start()
        {
            BindSliderUI();
        }

        private void BindSliderUI()
        {
            var operationGetter = operationGetter_model?.Value;
            if (operationGetter == null) { return; }

            operationGetter.OnTouchSliderListner
                .Subscribe(sliderTouchData => sliderUnitsController_view?.OnTouchSlider(sliderTouchData))
                .AddTo(this.gameObject);

            operationGetter.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value =>
                {
                    sliderUnitsController_view?.OnChangeSliderData(value.Value);
                    topicTextsController_view?.OnChangeSliderData(value.Value);
                })
                .AddTo(this.gameObject);

            operationGetter.SliderTouchDatas
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
