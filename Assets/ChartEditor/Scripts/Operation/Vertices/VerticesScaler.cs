using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 頂点の拡大縮小 (選択中の頂点を、その中心を基準に拡大縮小する)
    /// </summary>
    public class VerticesScaler : VerticesDragOperationBase
    {
        protected override EditMode IdleMode => EditMode.VerticesScale;

        protected override EditMode DraggingMode => EditMode.VerticesScaling;

        protected override Vector2 CalcPosition(Vector2 originPos, Vector2 cursorPos)
        {
            // 拡大倍率 = 中心からカーソルまでの距離 / 中心から掴んだ頂点までの距離
            float baseDistance = (BasePos - CenterPos).magnitude;
            float magnitude = baseDistance != 0f ? (cursorPos - CenterPos).magnitude / baseDistance : 0f;

            return originPos.ScalePoint(CenterPos, magnitude);
        }
    }
}
