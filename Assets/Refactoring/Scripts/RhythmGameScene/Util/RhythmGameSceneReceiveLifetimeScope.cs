using VContainer;
using VContainer.Unity;
using Refactoring;

public sealed class RhythmGameSceneReceiveLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<IJudgementRecorder>(resolver => resolver.Resolve<ScoreHolder>(), Lifetime.Singleton);
        builder.Register<Transitioner_LoadData>(Lifetime.Transient);
        builder.Register<Transitioner_LoadChart>(Lifetime.Transient);

    }
}