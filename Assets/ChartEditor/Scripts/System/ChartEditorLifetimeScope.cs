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
            builder.RegisterEntryPoint<RegulationWarningNotifier>().AsSelf();

            // 譜面の読み込み中、クリックを受け付けないようにする
            builder.RegisterEntryPoint<LoadingInputBlocker>();

            // プレビュー(ミニプレビューと全画面)のノーツを、クリック・ドラッグで選択・移動する
            // (実装しているインターフェースすべてで登録されるので、IPreviewPointer としても受け取れる。NotesCopier が貼り付け先を求めるのに使う)
            builder.RegisterEntryPoint<PreviewNotePicker>();

            // 同じ分線に並んだ、同じホールドのノーツの順番を入れ替える (↑ / ↓)
            builder.RegisterEntryPoint<ChainOrderChanger>();

            // ホールドを、制御点で曲げる (間にメッシュ中継点を置き直す)。制御点の印の見た目は、シーンに置いてある HoldCurveHandlesView
            builder.RegisterEntryPoint<HoldCurveEditor>();

            // ホールドを1本だけ編集する (ホールドをダブルクリックすると、ほかのノーツを白黒にして、触れなくする)
            builder.RegisterEntryPoint<HoldIsolation>();

            // SpaceHold を1本だけ、好きな角度から見て編集する (3D編集。画面部品は実行時に作る)
            builder.RegisterEntryPoint<SpaceHold3DEditor>();

            // ノーツのロック (右クリックのメニューから。ロックしたノーツは、選べない・動かせない)
            builder.RegisterEntryPoint<NoteLock>();
            // ダイナミックノーツを、地上のノーツに載せる (載っているノーツと一緒に動かす。ルールに合わないものに赤い縁取り)
            builder.RegisterEntryPoint<DynamicNoteAttachments>();

            // 編集画面で、ノーツを右クリックしたときのメニュー (画面部品は、シーンに置いてある EditorContextMenuView)
            builder.RegisterEntryPoint<NoteContextMenu>();
            // 頂点レイヤーから、来る前のレイヤーへ戻る (ミニプレビューの上のボタン)
            builder.RegisterEntryPoint<VerticesLayerReturn>();
            // 頂点レイヤーの中心点を右クリックすると、中心の位置を数値で決める窓を出す (窓は、ホールドの曲線の窓を複製して作る)
            builder.RegisterEntryPoint<VertexCenterPanel>();
            // 全画面プレビューの再生中は、まわりの画面部品を隠す
            builder.RegisterEntryPoint<PreviewPlaybackUi>();

            // 頂点レイヤーの下の、SpaceHold の節を時間の順に並べた帯 (フィルムストリップ。画面部品は実行時に作る)
            builder.RegisterEntryPoint<SpaceHoldFilmStrip>();

            // 編集画面の「譜面情報」の欄 (シーンに置いてある ChartInfoView) に、コンボ数・ノーツの種類ごとの数などを渡す
            builder.RegisterEntryPoint<ChartInfoPresenter>();

            // 編集画面の「全体マップ」(シーンに置いてある ChartOverviewView) に、譜面全体の縮小図と今の再生位置を渡す
            builder.RegisterEntryPoint<ChartOverviewPresenter>();
        }
    }
}
