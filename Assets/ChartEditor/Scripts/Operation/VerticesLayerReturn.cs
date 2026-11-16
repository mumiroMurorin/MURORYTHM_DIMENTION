using System;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 頂点レイヤーから、来る前のレイヤー (グラウンド・スペース) へ戻る (録音 20261117_024505)
    ///
    /// レイヤーのボタンで戻ると、グラウンド → スペース → 頂点 の順に回るので、分かりづらかった。
    /// 頂点レイヤーに入る前に開いていたレイヤー (グラウンドかスペース) を覚えておき、ミニプレビューの上の
    /// 「スペースに戻る」(または「グラウンドに戻る」) のボタン (LayerReturnButtonView) で、そこへ戻る。
    /// </summary>
    public class VerticesLayerReturn : ITickable, IDisposable
    {
        static VerticesLayerReturn instance;

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;

        // 最後に開いていた、ノーツのレイヤー (まだ無ければ、スペース: 頂点レイヤーは、ふつうスペースのノーツの形を編集しに来る)
        EditNoteType lastNoteLayer = EditNoteType.Space;

        public VerticesLayerReturn(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            instance = this;
        }

        void IDisposable.Dispose()
        {
            if (instance == this) { instance = null; }
        }

        void ITickable.Tick()
        {
            var layer = dataGetter.EditNoteType.Value;
            if (layer == EditNoteType.Ground || layer == EditNoteType.Space) { lastNoteLayer = layer; }
        }

        /// <summary>今、戻れるか (頂点レイヤーを開いているとき。3D編集の間は戻らない)</summary>
        public static bool CanReturn =>
            instance != null &&
            instance.dataGetter.ChartData.Value != null &&
            !instance.dataGetter.IsLoadingChart.Value &&
            instance.dataGetter.EditNoteType.Value == EditNoteType.Vertices &&
            !SpaceHold3DEditor.IsOpen;

        /// <summary>戻る先のレイヤー</summary>
        public static EditNoteType ReturnLayer => instance != null ? instance.lastNoteLayer : EditNoteType.Space;

        /// <summary>
        /// 来る前のレイヤーへ戻る
        /// </summary>
        public static void Return()
        {
            if (!CanReturn) { return; }
            instance.dataSetter.SetEditNoteType(instance.lastNoteLayer);
        }
    }
}
