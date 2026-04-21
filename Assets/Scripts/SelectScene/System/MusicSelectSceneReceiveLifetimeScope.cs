using VContainer;
using VContainer.Unity;

public sealed class MusicSelectSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<SelectSceneDataHolder>(Lifetime.Singleton)
            .As<ISelectSceneDataGetter>()
            .As<ISelectSceneDataSetter>();

        builder.Register<IJudgementRecorder>(resolver => resolver.Resolve<ScoreHolder>(), Lifetime.Singleton);
    }
}