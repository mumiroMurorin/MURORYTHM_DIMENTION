using VContainer;
using VContainer.Unity;

public sealed class TitleSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<ISelectSceneDataGetter>(resolver => resolver.Resolve<SelectSceneDataHolder>(), Lifetime.Singleton);
        builder.Register<ISelectSceneDataSetter>(resolver => resolver.Resolve<SelectSceneDataHolder>(), Lifetime.Singleton);
    }
}