using VContainer;
using VContainer.Unity;

public sealed class LobbySceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<LobbySceneDataHolder>(Lifetime.Singleton);
        builder.Register<ILobbySceneDataGetter>(resolver => resolver.Resolve<LobbySceneDataHolder>(), Lifetime.Singleton);
        builder.Register<ILobbySceneDataSetter>(resolver => resolver.Resolve<LobbySceneDataHolder>(), Lifetime.Singleton);
    }
}