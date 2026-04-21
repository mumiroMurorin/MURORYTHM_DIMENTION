using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        // 入力関係
        builder.Register<InputHolder>(Lifetime.Singleton)
            .AsSelf()
            .As<ISliderInputSetter>()
            .As<ISpaceInputSetter>()
            .As<ISliderInputGetter>()
            .As<ISpaceInputGetter>();

        // オプション
        builder.Register<OptionHolder>(Lifetime.Singleton)
            .AsSelf()
            .As<INoteSpawnDataOptionGetter>()
            .As<INoteSpawnDataOptionSetter>()
            .As<IOptionGetter>()
            .As<IOptionSetter>()
            .As<IVolumeGetter>();

        // スコア
        builder.Register<ScoreHolder>(Lifetime.Singleton)
            .AsSelf()
            .As<IJudgementRecorder>()
            .As<IScoreGetter>()
            .As<IScoreSetter>();

        // 楽曲データ
        builder.Register<MusicDataHolder>(Lifetime.Singleton)
            .AsSelf()
            .As<IMusicDataGetter>()
            .As<IMusicDataSetter>();

        // 楽曲データリスト
        builder.Register<MusicDataListHolder>(Lifetime.Singleton)
            .AsSelf()
            .As<IMusicDataListGetter>()
            .As<IMusicDataListSetter>();
    }
}