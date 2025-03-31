using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace UIInRhythmGameScene
{
    public class RhythmGameUIPresenter : MonoBehaviour
    {
        [SerializeField] Combo_View combo_view;
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
            // 楽曲データ → Ready?UI
            musicDataGetter_model?.Music
                .Subscribe(value => readyToPlayUIControllerView.SetMusicData(value))
                .AddTo(this.gameObject);
            
            // コンボ数
            scoreGetter_model?.Combo
                .Subscribe(combo_view.OnChangeCombo)
                .AddTo(this.gameObject);
        }
    }

}
