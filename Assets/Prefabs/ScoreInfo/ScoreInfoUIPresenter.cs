using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UnityEngine.UI;
using UniRx;

public class ScoreInfoUIPresenter : MonoBehaviour
{
    [Inject] IOptionGetter optionGetter_model;
    [Inject] IScoreGetter scoreGetter_model;

    [Header("======== メイン情報 ========")]
    [SerializeField] Combo_View[] combo_view;
    [SerializeField] ScoreRank_View scoreRank_view;
    [SerializeField] ScoreRank_View scoreRankSubtraction_view;
    
    [SerializeField] InfoTypeMainToGameObject[] mainInfos;

    [Header("======== サブ情報 ========")]
    [SerializeField] ComboRank_View comboRankSub_view;
    [SerializeField] Score_View scoreSub_view;
    [SerializeField] Score_View scoreSubtractionSub_view;
    [SerializeField] Breakdown_View breakdown_view;

    [SerializeField] InfoTypeSubToGameObject[] subInfos;

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        if (optionGetter_model == null) { return; }
        if (scoreGetter_model == null) { return; }

        // ========== 表示情報 ==========
        optionGetter_model.MainInfo
            .Subscribe(type => { 
                foreach (var obj in mainInfos)
                {
                    obj.SetActive(type);
                }
            })
            .AddTo(this.gameObject);

        optionGetter_model.SubInfo
            .Subscribe(type => {
                foreach (var obj in subInfos)
                {
                    obj.SetActive(type);
                }
            })
            .AddTo(this.gameObject);

        // ========== コンボ数 ========== 
        scoreGetter_model?.Combo
            .Subscribe(combo => { 
                foreach (var view in combo_view) 
                {
                    view.OnChangeCombo(combo);
                }
            })
            .AddTo(this.gameObject);

        // ========== スコアランク (加算) ==========
        scoreGetter_model?.CurrentScoreRank
            .Subscribe(scoreRank_view.OnChangeScoreRank)
            .AddTo(this.gameObject);

        // ========== スコアランク (減算) ==========
        scoreGetter_model?.CurrentScoreRankSubtraction
            .Subscribe(scoreRankSubtraction_view.OnChangeScoreRank)
            .AddTo(this.gameObject);

        // ========== コンボランク ========== 
        scoreGetter_model?.CurrentComboRank
            .Subscribe(rank => {
                foreach (var view in combo_view)
                {
                    view.OnChangeComboRank(rank);
                }
            })
            .AddTo(this.gameObject);

        scoreGetter_model?.CurrentComboRank
            .Subscribe(comboRankSub_view.OnChangeComboRank)
            .AddTo(this.gameObject);

        // ========== スコア (加算) ==========
        scoreGetter_model?.Score
            .Subscribe(scoreSub_view.OnChangeScore)
            .AddTo(this.gameObject);

        scoreGetter_model?.CurrentScoreRank
            .Subscribe(scoreSub_view.OnChangeScoreRank)
            .AddTo(this.gameObject);

        // ========== スコア (減算) ==========
        scoreGetter_model?.ScoreSubtraction
            .Subscribe(scoreSubtractionSub_view.OnChangeScore)
            .AddTo(this.gameObject);

        scoreGetter_model?.CurrentScoreRankSubtraction
            .Subscribe(scoreSubtractionSub_view.OnChangeScoreRank)
            .AddTo(this.gameObject);

        // ========== 内訳 ==========
        scoreGetter_model.PerfectNum
            .Subscribe(breakdown_view.OnChangePerfectCount)
            .AddTo(this.gameObject);

        scoreGetter_model.GreatNum
            .Subscribe(breakdown_view.OnChangeGreatCount)
            .AddTo(this.gameObject);

        scoreGetter_model.GoodNum
            .Subscribe(breakdown_view.OnChangeGoodCount)
            .AddTo(this.gameObject);

        scoreGetter_model.MissNum
            .Subscribe(breakdown_view.OnChangeMissCount)
            .AddTo(this.gameObject);
    }

    [System.Serializable]
    class InfoTypeMainToGameObject
    {
        [SerializeField] InfoTypeMain type;
        [SerializeField] GameObject obj;

        public bool SetActive(InfoTypeMain type)
        {
            obj.SetActive(this.type == type);
            return this.type == type;
        }
    }

    [System.Serializable]
    class InfoTypeSubToGameObject
    {
        [SerializeField] InfoTypeSub type;
        [SerializeField] GameObject obj;

        public bool SetActive(InfoTypeSub type)
        {
            obj.SetActive(this.type == type);
            return this.type == type;
        }
    }
}
