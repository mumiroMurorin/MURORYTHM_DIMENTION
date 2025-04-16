using VContainer;
using VContainer.Unity;
using Refactoring;

public sealed class MusicSelectSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<SelectSceneDataHolder>(Lifetime.Singleton);
        builder.Register<ISelectSceneDataGetter>(resolver => resolver.Resolve<SelectSceneDataHolder>(), Lifetime.Singleton);
        builder.Register<ISelectSceneDataSetter>(resolver => resolver.Resolve<SelectSceneDataHolder>(), Lifetime.Singleton);
    }
}