using VContainer;
using VContainer.Unity;
using Refactoring;

public sealed class RhythmGameSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<IJudgementRecorder>(resolver => resolver.Resolve<ScoreHolder>(), Lifetime.Singleton);
        builder.Register<IChartDataGetter>(resolver => resolver.Resolve<MusicDataHolder>(), Lifetime.Singleton);
        builder.Register<IChartDataSetter>(resolver => resolver.Resolve<MusicDataHolder>(), Lifetime.Singleton);
    }
}