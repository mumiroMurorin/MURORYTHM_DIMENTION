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

        builder.Register<InputHolder>(Lifetime.Singleton); // �C���X�^���X��1�x�����o�^
        builder.Register<ISliderInputSetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISpaceInputSetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISliderInputGetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
        builder.Register<ISpaceInputGetter>(resolver => resolver.Resolve<InputHolder>(), Lifetime.Singleton);
    }
}