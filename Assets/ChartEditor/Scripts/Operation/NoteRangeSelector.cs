using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// ノーツの範囲選択
    /// </summary>
    public class NoteRangeSelector : RangeSelectorBase
    {
        [SerializeField] MultiNoteSelector multiNoteSelector;
        [SerializeField] float minY;
        [SerializeField] float maxY;

        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.Connecting,
            EditMode.EditBarConfig,
            EditMode.EditingBarConfig,
            EditMode.EditSubDivisionConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
            EditMode.Preview,
        };

        protected override bool IsActive =>
            dataGetter.EditNoteType.Value == EditNoteType.Ground || dataGetter.EditNoteType.Value == EditNoteType.Space;

        protected override EditMode[] IgnoreEditModes => ignoreEditModes;

        protected override bool IsCursorOnSelectable => dataGetter.GetInteractableCollider<ISelectableNoteCollider>() != null;

        protected override void SelectObjectUnderCursor()
        {
            var collider = dataGetter.GetInteractableCollider<ISelectableNoteCollider>();
            if (collider == null) { return; }

            multiNoteSelector.SelectMulti(collider.SelectableObject);
        }

        protected override void SelectObjects(Rect selectionRect)
        {
            foreach (var dtn in notesGetter.DataToNoteObject)
            {
                // ホールドを1本だけ編集している間は、ほかのノーツを選ばない
                if (HoldIsolation.IsBlocked(dtn.Data)) { continue; }

                var obj = dtn.Object;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(obj.transform.position);

                // カメラの前方にあるか
                if (screenPos.z < 0f) { continue; }
                if (obj.transform.position.y < minY || maxY < obj.transform.position.y) { continue; }

                if (!selectionRect.Contains((Vector2)screenPos)) { continue; }
                if (!dtn.Object.TryGetComponent(out ISelectableNoteObject selectable)) { continue; }

                multiNoteSelector.SelectMulti(selectable);
            }
        }
    }
}
