using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UnityEngine.UI;
using UniRx;

namespace UIInRhythmGameScene
{
    public class RhythmGameUIPresenter : MonoBehaviour
    {
        [SerializeField] ReadyToPlayUIControllerView readyToPlayUIControllerView;

        IScoreGetter scoreGetter_model;
        IMusicDataGetter musicDataGetter_model;

        [Inject]
        public void Constructor(IScoreGetter scoreGetter, IMusicDataGetter musicDataGetter)
        {
            scoreGetter_model = scoreGetter;
            musicDataGetter_model = musicDataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 楽曲データに関連するUI
            musicDataGetter_model?.Music
                .Where(data => data != null)
                .Subscribe(readyToPlayUIControllerView.SetMusicData)
                .AddTo(this.gameObject);

            musicDataGetter_model?.Difficulty
                .Subscribe(readyToPlayUIControllerView.SetDifficulty)
                .AddTo(this.gameObject);
        }
    }

}
