using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using UniRx;

namespace UIInResultScene
{
    public class ResultUIPresenter : MonoBehaviour
    {
        [SerializeField] Image[] themeImages;
        [SerializeField] GameObject[] hideUIs;
        [SerializeField] MusicInfoView musicInfo_view;
        [SerializeField] ScoreView score_view;
        [SerializeField] ScoreRankView scoreRank_view;
        [SerializeField] DifficultyViewController difficulty_view;
        [SerializeField] BreakdownView breakdown_view;
        [SerializeField] AchievementView achievement_view;
        [SerializeField] CircleController circleController_view;

        [Space(20)]
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SliderTopicTextsControllerView topicTextsController_view;

        [Space(20)]
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;

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
            Initialize();
            Bind();
            BindForSliderUI();
            SetEvent();
        }

        private void Initialize()
        {
            // テーマ画像
            if (themeImages != null)
            {
                foreach (var image in themeImages)
                {
                    image.sprite = musicDataGetter_model.Music.Value.ThemeSprite;
                }
            }

            foreach (var obj in hideUIs)
            {
                obj.SetActive(false);
            }
        }

        private void Bind()
        {
            // 楽曲データ
            musicDataGetter_model?.Music
                .Subscribe(musicInfo_view.OnChangeMusicData)
                .AddTo(this.gameObject);

            musicDataGetter_model?.Difficulty
                .Subscribe(musicInfo_view.OnChangeDifficulty)
                .AddTo(this.gameObject);

            // 難易度データ
            musicDataGetter_model?.Music
                .Subscribe(difficulty_view.OnChangeMusicData)
                .AddTo(this.gameObject);

            musicDataGetter_model.Difficulty
                .Subscribe(difficulty_view.OnChangeDifficulty)
                .AddTo(this.gameObject);

            // スコアデータ
            scoreGetter_model?.Score
                .Subscribe(score_view.OnChangeScore)
                .AddTo(this.gameObject);

            // スコアランクデータ
            scoreGetter_model?.CurrentScoreRank
                .Subscribe(score_view.OnChangeScoreRank)
                .AddTo(this.gameObject);

            scoreGetter_model?.CurrentScoreRank
                .Subscribe(scoreRank_view.OnChangeScoreRank)
                .AddTo(this.gameObject);

            // 内訳データ
            scoreGetter_model?.PerfectNum
                .Subscribe(breakdown_view.OnChangePerfectCount)
                .AddTo(this.gameObject);

            scoreGetter_model?.GreatNum
                .Subscribe(breakdown_view.OnChangeGreatCount)
                .AddTo(this.gameObject);

            scoreGetter_model?.GoodNum
                .Subscribe(breakdown_view.OnChangeGoodCount)
                .AddTo(this.gameObject);

            scoreGetter_model?.MissNum
                .Subscribe(breakdown_view.OnChangeMissCount)
                .AddTo(this.gameObject);

            // 達成
            scoreGetter_model?.CurrentComboRank
                .Subscribe(achievement_view.OnChangeComboRank)
                .AddTo(this.gameObject);

            scoreGetter_model?.CurrentScoreRank
                .Subscribe(achievement_view.OnChangeScoreRank)
                .AddTo(this.gameObject);

            // 背景
            musicDataGetter_model?.Music
                .Where(data => data != null)
                .Take(1)
                .Subscribe(circleController_view.OnChangeMusicData)
                .AddTo(this.gameObject);
        }

        private void BindForSliderUI()
        {
            // タッチされた時のUI挙動
            operationGetter_model?.Value.OnTouchSliderListner
                .Subscribe(sliderUnitsController_view.OnTouchSlider)
                .AddTo(this.gameObject);

            // 操作の追加
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value => {
                    sliderUnitsController_view.OnChangeSliderData(value.Value);
                    topicTextsController_view?.OnChangeSliderData(value.Value);
                })
                .AddTo(this.gameObject);

            // 操作の一新
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveReset()
                .Subscribe(_ => {
                    sliderUnitsController_view.OnClearSliderData();
                    topicTextsController_view?.OnClearSliderData();
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
           
        }

        public void SetNewRecordActive(bool isScoreNewRecord, bool isScoreRankNewRecord)
        {
            score_view?.SetNewRecordActive(isScoreNewRecord);
            scoreRank_view?.SetNewRecordActive(isScoreRankNewRecord);
        }
    }
}
