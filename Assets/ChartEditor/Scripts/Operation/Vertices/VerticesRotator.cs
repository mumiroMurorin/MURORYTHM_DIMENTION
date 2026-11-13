using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 頂点の回転 (選択中の頂点を、その中心まわりに回転させる)
    /// </summary>
    public class VerticesRotator : VerticesDragOperationBase
    {
        protected override EditMode IdleMode => EditMode.VerticesRotate;

        protected override EditMode DraggingMode => EditMode.VerticesRotating;

        protected override Vector2 CalcPosition(Vector2 originPos, Vector2 cursorPos)
        {
            // 掴んだ頂点 → カーソル位置 の、中心から見た角度だけ回す
            float rotateAngle = Vector2Extensions.AngleBetweenVectors(BasePos, cursorPos, CenterPos);
            return originPos.RotatePoint(CenterPos, rotateAngle);
        }
    }
}
