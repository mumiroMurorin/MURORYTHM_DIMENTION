using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChartEditor
{
    public class ChartEditorLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ChartEditorRecorder>(Lifetime.Singleton)
               .As<IChartEditorDataGetter>()
               .As<IChartEditorDataSetter>();
        }
    }
}