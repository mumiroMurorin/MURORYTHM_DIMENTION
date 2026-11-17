using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class NoteTypeChanger : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.ChangeType) { return; }

            // 左クリック
            if (Input.GetMouseButtonDown(0)) { ChangeNoteTypeOnClick(); }
        }

        /// <summary>
        /// ノーツタイプの変更
        /// </summary>
        private void ChangeNoteTypeOnClick()
        {

            var collider = chartEditorDataGetter.GetInteractableCollider<IChangableCollider>();
            if (collider == null) { return; }

            var changableObject = collider.Note;
            if (changableObject.NoteData == null) { return; }

            // 先に変更し、実際に種類が変わったときだけUndo履歴に登録する
            // (変更先が無い種類のノートで、何も起きない操作を履歴に積まないため)
            var previousType = changableObject.NoteData.NoteTypeRP.Value;

            // ダイナミックノーツが載っているノーツは HoldMeshRelay にできない。ルールに合わないダイナミックノーツは、種類を変えられない
            if (!DynamicNoteAttachments.CanChangeType(changableObject.NoteData as IDeployableNoteData, NoteTypeCycle.NextNoteType(previousType), out string refusal))
            {
                DynamicNoteAttachments.LogRefused(1, refusal);
                return;
            }

            ChangeNoteType(changableObject);
            if (changableObject.NoteData.NoteTypeRP.Value == previousType) { return; }

            Record(() => {
                ChangeNoteType(changableObject);
            },
            () => {
                ChangeNoteType(changableObject);
            }, execute: false);
        }

        private void ChangeNoteType(IChangableObject changableObject)
        {
            changableObject.NoteData.ChangeNoteType();
            //changableObject.OnChangeNoteType();
        }
    }

}