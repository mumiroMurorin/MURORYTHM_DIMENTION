using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 頂点の移動 (掴んだ頂点と、選択中の頂点をまとめて平行移動する)
    /// 頂点の編集ツール・配置ツールの中の操作。配置ツールで何も無い所を押して置いた頂点も、ボタンを離さずに動かせば、そのまま掴んで動かせる
    /// </summary>
    public class VerticesMover : VerticesDragOperationBase
    {
        protected override EditMode IdleMode => EditMode.VertexEdit;

        // 編集ツールと配置ツールの、どちらでも動かせる
        protected override bool IsIdleMode(EditMode mode) => mode == EditMode.VertexEdit || mode == EditMode.VertexPlace;

        protected override EditMode DraggingMode => EditMode.VertexMoving;

        protected override bool IncludesClickedVertex => true;

        protected override Vector2 CalcPosition(Vector2 originPos, Vector2 cursorPos)
        {
            // 掴んだ頂点との位置関係を保ったままカーソルに追従させる
            return cursorPos + (originPos - BasePos);
        }
    }
}
