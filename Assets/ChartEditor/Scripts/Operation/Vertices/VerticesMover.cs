using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 頂点の移動 (掴んだ頂点と、選択中の頂点をまとめて平行移動する)
    /// </summary>
    public class VerticesMover : VerticesDragOperationBase
    {
        protected override EditMode IdleMode => EditMode.VertexMove;

        protected override EditMode DraggingMode => EditMode.VertexMoving;

        protected override bool IncludesClickedVertex => true;

        protected override Vector2 CalcPosition(Vector2 originPos, Vector2 cursorPos)
        {
            // 掴んだ頂点との位置関係を保ったままカーソルに追従させる
            return cursorPos + (originPos - BasePos);
        }
    }
}
