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
                .As<IChartEditorOptionSetter>()
                .As<IVolumeGetter>();

            builder.Register<NotesDataHolder>(Lifetime.Singleton)
                .AsSelf()
                .As<INotesDataGetter>()
                .As<INotesDataSetter>();

            builder.Register<VerticesChainNavigator>(Lifetime.Singleton);

            // 未保存の変更の監視・自動保存
            builder.RegisterEntryPoint<ChartSaveGuard>().AsSelf();

            // レギュレーション違反(赤いアウトライン)をログ欄に知らせる
            builder.RegisterEntryPoint<RegulationWarningNotifier>();

            // 譜面の読み込み中、クリックを受け付けないようにする
            builder.RegisterEntryPoint<LoadingInputBlocker>();

            // ミニプレビューのノーツをクリックして、譜面側のノーツを選択する
            builder.RegisterEntryPoint<PreviewNotePicker>();
        }
    }
}
