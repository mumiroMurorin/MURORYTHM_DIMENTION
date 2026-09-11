using UniRx;

namespace UIInSelectScene
{
    public class OptionTopicPresenter_NoteVisibleDistance : UnityEngine.MonoBehaviour, IOptionTopicPresenter
    {
        [UnityEngine.SerializeField] OptionTopicView_NoteVisibleDistance view;

        public void Bind(IOptionGetter optionGetter)
        {
            optionGetter.NoteVisibleDistance
                .Subscribe(_ => view.OnChangeDistance(optionGetter.NoteVisibleDistanceDisplay))
                .AddTo(gameObject);
        }

        public void SetEvent(IOptionGetter optionGetter)
        {
        }
    }
}
