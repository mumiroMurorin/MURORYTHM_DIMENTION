using VContainer;
using VContainer.Unity;

public sealed class GameOverSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<GameOverSceneDataHolder>(Lifetime.Singleton)
            .As<IGameOverSceneDataGetter>()
            .As<IGameOverSceneDataSetter>();
    }
}