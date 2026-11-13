using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 頂点の範囲選択
    /// </summary>
    public class VerticesRangeSelector : RangeSelectorBase
    {
        [SerializeField] MultiVertexSelector multiVertexSelector;
        [SerializeField] float minZ;
        [SerializeField] float maxZ;

        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.VertexDeploy,
            EditMode.VerticesScaling,
            EditMode.VertexMoving,
            EditMode.VerticesRotating,
            EditMode.Preview,
        };

        protected override bool IsActive => dataGetter.EditNoteType.Value == EditNoteType.Vertices;

        protected override EditMode[] IgnoreEditModes => ignoreEditModes;

        // 頂点の上、または中心点(選択はできないが掴める)の上では、範囲選択を始めない
        protected override bool IsCursorOnSelectable =>
            dataGetter.GetInteractableCollider<ISelectableVertexCollider>() != null ||
            dataGetter.GetInteractableCollider<IPointMovableCollider>() != null;

        protected override void SelectObjects(Rect selectionRect)
        {
            foreach (var dtn in notesGetter.DataToVertexObject)
            {
                var obj = dtn.Object;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(obj.transform.position);

                // 範囲内かどうか
                if (obj.transform.position.z < minZ || maxZ < obj.transform.position.z) { continue; }
                if (!selectionRect.Contains((Vector2)screenPos)) { continue; }
                if (!dtn.Object.TryGetComponent(out ISelectableVertexObject selectable)) { continue; }

                multiVertexSelector.SelectMulti(selectable);
            }
        }
    }
}
