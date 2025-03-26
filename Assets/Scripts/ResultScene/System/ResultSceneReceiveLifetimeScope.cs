using VContainer;
using VContainer.Unity;
using Refactoring;

public sealed class ResultSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        //builder.Register<MusicDataListHolder>(Lifetime.Singleton);
        //builder.Register<ISelectSceneDataGetter>(resolver => resolver.Resolve<MusicDataListHolder>(), Lifetime.Singleton);
        //builder.Register<ISelectSceneDataSetter>(resolver => resolver.Resolve<MusicDataListHolder>(), Lifetime.Singleton);
    }
}