using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        IChartEditorDataGetter chartEditorDataGetter;
        IMovableObject movedNote;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { StartMoveNote(); }
            else if (Input.GetMouseButton(0)) { MoveNote(); } 
            else if (Input.GetMouseButtonUp(0)) { EndMoveNote(); }
        }

        private void StartMoveNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Move) { return; }

            IMovableObject movableObject = chartEditorDataGetter.MovableObject.Value;
            if (movableObject == null) { return; }

            movableObject.OnMoveStart();
            movedNote = movableObject;
        }
        
        private void MoveNote()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Move) { return; }
            if (movedNote == null) { return; }

            // カーソル下の親取得
            IDeployableCollider deployable = chartEditorDataGetter.DeployableCollider.Value;
            if (deployable == null) { return; }
            if (movedNote.Note.transform.position == deployable.deployParent.position)  { return; }

            // アドレスの移動
            chartEditorDataGetter.ChartData.Value.ChangeNoteAddress(movedNote.Note.NoteData, deployable.Address);

            // オブジェクト側の行動
            movedNote.OnMove();
        }

        private void EndMoveNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Move) { return; }

            movedNote?.OnMoveEnd();
            movedNote = null;
        }
    }
}
