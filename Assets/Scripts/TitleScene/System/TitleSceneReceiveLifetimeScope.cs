using VContainer;
using VContainer.Unity;

public sealed class TitleSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        // タイトルへ戻った時点で、前のプレイヤーが選択した楽曲と難易度を初期化する
        builder.RegisterEntryPoint<TitleSceneMusicSelectionResetter>();

        builder.Register<ISelectSceneDataGetter>(resolver => resolver.Resolve<SelectSceneDataHolder>(), Lifetime.Singleton);
        builder.Register<ISelectSceneDataSetter>(resolver => resolver.Resolve<SelectSceneDataHolder>(), Lifetime.Singleton);
    }
}
