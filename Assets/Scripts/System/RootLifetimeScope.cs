using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Refactoring;

public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        // 入力関係
        builder.Register<InputHolder>(Lifetime.Singleton);
        builder.Register<ISliderInputSetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISpaceInputSetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISliderInputGetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISpaceInputGetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);

        // オプション
        builder.Register<OptionHolder>(Lifetime.Singleton);
        builder.Register<INoteSpawnDataOptionHolder>(resolver => resolver.Resolve<OptionHolder>(), Lifetime.Singleton);

        // スコア
        builder.Register<ScoreHolder>(Lifetime.Singleton);
        builder.Register<IScoreGetter>(resolver => resolver.Resolve<ScoreHolder>(), Lifetime.Singleton);

        // 楽曲データ
        builder.Register<MusicDataHolder>(Lifetime.Singleton);
        builder.Register<IMusicDataGetter>(resolver => resolver.Resolve<MusicDataHolder>(), Lifetime.Singleton);
        builder.Register<IMusicDataSetter>(resolver => resolver.Resolve<MusicDataHolder>(), Lifetime.Singleton);
    }
}