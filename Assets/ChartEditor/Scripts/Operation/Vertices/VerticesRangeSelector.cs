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

        // 編集ツールでは、頂点を置ける所を押すと頂点が置かれるので、そこからのドラッグでは範囲選択を始めない
        // (Shift を押しながらのドラッグで始める。頂点を置けない所 (円の外) からのドラッグと、
        //  回転・拡大縮小のツールでは、今までどおり Shift 無しで始まる)
        protected override bool StartsOnPlainPress =>
            dataGetter.CurrentEditMode.Value != EditMode.VertexEdit ||
            dataGetter.GetInteractableCollider<IPointDeployableCollider>() == null;

        protected override void SelectObjectUnderCursor()
        {
            var collider = dataGetter.GetInteractableCollider<ISelectableVertexCollider>();
            if (collider == null) { return; }

            multiVertexSelector.SelectMulti(collider.SelectableObject);
        }

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
