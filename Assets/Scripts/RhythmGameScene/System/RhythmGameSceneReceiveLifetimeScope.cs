using VContainer;
using VContainer.Unity;

public sealed class RhythmGameSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<IJudgementRecorder>(resolver => resolver.Resolve<ScoreHolder>(), Lifetime.Singleton);

        builder.Register<ChartDataHolder>(Lifetime.Singleton);
        builder.Register<IChartDataGetter>(resolver => resolver.Resolve<ChartDataHolder>(), Lifetime.Singleton);
        builder.Register<IChartDataSetter>(resolver => resolver.Resolve<ChartDataHolder>(), Lifetime.Singleton);
    }
}