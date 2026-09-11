using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_NoteCurveRadius : UnityEngine.MonoBehaviour, IOptionTopicPresenter
    {
        [UnityEngine.SerializeField] OptionTopicView_NoteCurveRadius view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.NoteCurveRadius
                .Subscribe(_ => view.OnChangeRadius(optionGetter.NoteCurveRadiusDisplay))
                .AddTo(gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {
        }
    }
}
