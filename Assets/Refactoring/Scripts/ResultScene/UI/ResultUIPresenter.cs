using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;


namespace Refactoring.UIInResultScene
{
    public class ResultUIPresenter : MonoBehaviour
    {
        [SerializeField] MusicDataUIControllerView musicDataUIController_view;
        [SerializeField] ScoreDataUIControllerView scoreDataUIController_view;

        IScoreGetter scoreGetter_model;
        IMusicDataGetter musicDataGetter_model;

        [Inject] 
        public void Construct(IScoreGetter scoreGetter, IMusicDataGetter musicDataGetter)
        {
            scoreGetter_model = scoreGetter;
            musicDataGetter_model = musicDataGetter;
        }

        private void Start()
        {
            Bind();
            SetEvent();
        }

        private void Bind()
        {
            if (musicDataGetter_model.Music != null) { musicDataUIController_view.SetMusicData(musicDataGetter_model.Music.Value); }
            scoreDataUIController_view.SetScoreData(scoreGetter_model);
        }

        private void SetEvent()
        {

        }
    }
}
