using VContainer;
using VContainer.Unity;

public sealed class LobbySceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<LobbySceneDataHolder>(Lifetime.Singleton)
            .As<ILobbySceneDataGetter>()
            .As<ILobbySceneDataSetter>();
    }
}