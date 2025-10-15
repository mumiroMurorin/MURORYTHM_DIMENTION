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
        [SerializeField] BackGround_View backGround_view;
        [SerializeField] Combo_View combo_view;
        [SerializeField] ComboRank_View comboRank_view;
        [SerializeField] ScoreRank_View scoreRank_view;
        [SerializeField] Score_View score_view;
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
            // コンボ数
            scoreGetter_model?.Combo
                .Subscribe(combo_view.OnChangeCombo)
                .AddTo(this.gameObject);

            // コンボランク
            scoreGetter_model?.CurrentComboRank
                .Subscribe(combo_view.OnChangeComboRank)
                .AddTo(this.gameObject);

            scoreGetter_model?.CurrentComboRank
                .Subscribe(comboRank_view.OnChangeComboRank)
                .AddTo(this.gameObject);

            // スコア
            scoreGetter_model?.Score
                .Subscribe(score_view.OnChangeScore)
                .AddTo(this.gameObject);

            scoreGetter_model?.CurrentScoreRank
                .Subscribe(score_view.OnChangeScoreRank)
                .AddTo(this.gameObject);

            // スコアランク
            scoreGetter_model?.CurrentScoreRank
                .Subscribe(scoreRank_view.OnChangeScoreRank)
                .AddTo(this.gameObject);

            // 楽曲データに関連するUI
            musicDataGetter_model?.Music
                .Where(data => data != null)
                .Subscribe(OnSetMusicData)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 楽曲データに関するUI情報をセット
        /// </summary>
        /// <param name="musicData"></param>
        private void OnSetMusicData(MusicData musicData)
        {
            readyToPlayUIControllerView.SetMusicData(musicData);
            backGround_view.OnSetMusicData(musicData);
        }
    }

}
