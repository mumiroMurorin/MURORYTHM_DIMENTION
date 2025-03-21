using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace Refactoring.UIInSelectScene
{
    public class SelectUIPresenter : MonoBehaviour
    {
        [SerializeField] MusicTopicController musicTopicController_view;

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
            musicData_model?.MusicIndexSelected
                .Pairwise()
                .Subscribe(pair => _ = musicTopicController_view.OnChangeSelectedTopic(pair.Current - pair.Previous))
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {

        }
    }
}
