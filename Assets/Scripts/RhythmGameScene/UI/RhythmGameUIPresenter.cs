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
