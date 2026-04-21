using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChartEditor
{
    public class ChartEditorLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ChartEditorDataHolder>(Lifetime.Singleton)
               .AsSelf()
               .As<IChartEditorDataGetter>()
               .As<IChartEditorDataSetter>();

            builder.Register<ChartEditorOptionHolder>(Lifetime.Singleton)
                .AsSelf()
                .As<INoteSpawnDataOptionGetter>()
                .As<INoteSpawnDataOptionSetter>()
                .As<IChartEditorOptionGetter>()
                .As<IChartEditorOptionSetter>();

            builder.Register<NotesDataHolder>(Lifetime.Singleton)
                .AsSelf()
                .As<INotesDataGetter>()
                .As<INotesDataSetter>();
        }
    }
}