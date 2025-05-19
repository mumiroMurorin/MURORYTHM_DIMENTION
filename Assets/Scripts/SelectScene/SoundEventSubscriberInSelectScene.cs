using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using TransitionerInSelectScene;

public class SoundEventSubscriberInSelectScene : MonoBehaviour
{
    [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;

    ISelectSceneDataGetter selectSceneDataGetter;
    SoundManager soundManager;

    [Inject]
    public void Construct(ISelectSceneDataGetter selectSceneDataGetter)
    {
        this.selectSceneDataGetter = selectSceneDataGetter;
    }

    void Start()
    {
        soundManager = SoundManager.Instance;

        BindInSelectMusicPhase();
        BindInMusicDetailSelectPhase();
        BindInOptionSelectPhase();
    }

    private void BindInSelectMusicPhase()
    {
        // トピックの移動
        selectSceneDataGetter?.CurrentMusicIndex
            .Skip(1)
            .Subscribe(_ => {
                soundManager.PlaySE(SE_Type.MoveTopic);
            })
            .AddTo(this.gameObject);

        // 選択楽曲の変更
        selectSceneDataGetter?.CurrentMusicData
            .Where(value => value != null)
            .Subscribe(value => {
                soundManager.PlayBGM(value.SampleClip);
            })
            .AddTo(this.gameObject);

        // 難易度UP
        selectSceneDataGetter?.Difficulty
            .Pairwise()
            .Where(pair => pair.Previous < pair.Current)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.UpDifficulty))
            .AddTo(this.gameObject);

        // 難易度DOWN
        selectSceneDataGetter?.Difficulty
            .Pairwise()
            .Where(pair => pair.Previous > pair.Current)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.DownDifficulty))
            .AddTo(this.gameObject);

        // 楽曲トピックの選択
        phaseStatusGetter?.Value.PhaseStatus
            .Pairwise()
            .Where(pair => pair.Current == PhaseStatusInSelectScene.DetailSelect && pair.Previous == PhaseStatusInSelectScene.MusicSelect)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.SelectMusic))
            .AddTo(this.gameObject);

    }

    private void BindInMusicDetailSelectPhase()
    {
        // 楽曲の決定
        phaseStatusGetter?.Value.PhaseStatus
            .Pairwise()
            .Where(pair => pair.Current == PhaseStatusInSelectScene.FadeOut && pair.Previous == PhaseStatusInSelectScene.DetailSelect)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.DesicionMusic))
            .AddTo(this.gameObject);

        // 楽曲トピック選択に戻る
        phaseStatusGetter?.Value.PhaseStatus
            .Pairwise()
            .Where(pair => pair.Current == PhaseStatusInSelectScene.MusicSelect && pair.Previous == PhaseStatusInSelectScene.DetailSelect)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.BackTopic1))
            .AddTo(this.gameObject);

        // オプションの選択
        phaseStatusGetter?.Value.PhaseStatus
            .Pairwise()
            .Where(pair => pair.Current == PhaseStatusInSelectScene.MusicOption && pair.Previous == PhaseStatusInSelectScene.DetailSelect)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.SelectOption))
            .AddTo(this.gameObject);
    }

    private void BindInOptionSelectPhase()
    {
        // トピックの移動
        selectSceneDataGetter?.CurrentOptionIndex
            .Skip(1)
            .Subscribe(_ => {
                soundManager.PlaySE(SE_Type.MoveTopic);
            })
            .AddTo(this.gameObject);

        // 楽曲確認に戻る
        phaseStatusGetter?.Value.PhaseStatus
            .Pairwise()
            .Where(pair => pair.Current == PhaseStatusInSelectScene.DetailSelect && pair.Previous == PhaseStatusInSelectScene.MusicOption)
            .Subscribe(_ => soundManager.PlaySE(SE_Type.BackTopic1))
            .AddTo(this.gameObject);

    }
}

