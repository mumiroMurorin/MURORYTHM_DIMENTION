using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Refactoring;

public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        base.Configure(builder);

        builder.Register<InputHolder>(Lifetime.Singleton); // インスタンスを1度だけ登録
        builder.Register<ISliderInputSetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISpaceInputSetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISliderInputGetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISpaceInputGetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
    }
}