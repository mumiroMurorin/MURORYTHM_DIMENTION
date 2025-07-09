using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using UniRx;
using static UndoRedo.History;

namespace ChartEditor
{
    public class NotesCopier : MonoBehaviour
    {
        [SerializeField] NoteDeployer noteDeployer;
        [SerializeField] SpaceDeployer spaceDeployer;

        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        List<IDeployableNoteData> copiedNotes;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.Connecting,
             EditMode.EditingBarConfig,
             EditMode.EditingSubDivisionConfig,
             EditMode.Moving,
             EditMode.Scaling,
             EditMode.SpaceMoving
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        void Update()
        {
            EditMode currentEditMode = dataGetter.CurrentEditMode.Value;

            if (dataGetter.EditNoteType.Value != EditNoteType.Ground && dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }
            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }

            // ノーツのコピー
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C)) { CopyNotes(); }
            // ノーツの貼り付け
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V)) { PasteVertices(); }
        }

        /// <summary>
        /// ノーツのコピー
        /// </summary>
        private void CopyNotes()
        {
            copiedNotes = new List<IDeployableNoteData>(notesGetter.SelectingNotes);
            Debug.Log("【Notes】ノーツをコピー");
        }

        /// <summary>
        /// ノーツの張り付け
        /// </summary>
        private void PasteVertices()
        {
            var currentEditMode = dataGetter.CurrentEditMode.Value;
            var groundCollider = dataGetter.GetInteractableCollider<IDeployableCollider>();
            var spaceCollider = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();

            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }
            if (copiedNotes == null) { return; }
            if (groundCollider == null && spaceCollider == null) { return; }

            var copiedNotesCopy = copiedNotes.Select(x => x.Copy()).OrderedByAddress().ToList();
            var cursorAddress = groundCollider != null ? groundCollider.Address : spaceCollider.Address;
            var subdivisionDelta = dataGetter.ChartData.Value.GetSubdivisionDelta(cursorAddress, new AddressInChart(copiedNotesCopy[0].Address));

            // 全選択解除
            notesSetter.ClearSelectingNotes();

            foreach (var data in copiedNotesCopy)
            {
                var address = dataGetter.ChartData.Value.AddressAddition(new AddressInChart(data.Address), subdivisionDelta);

                data.SetAddress(new AddressWithinRange(address, data.Address.Range.Count));

                // 配置
                if (data.Address.Range[0] == 100) { spaceDeployer.DeployForNoteData(data); }
                else { noteDeployer.DeployForNoteData(data); }

                // 選択する
                if(!notesGetter.GetNoteObject(data).TryGetComponent(out ISelectableNoteObject selectable)) { continue; }
                notesSetter.TryAddSelectingNotes(selectable.NoteObject.NoteData);
            }
           
            Debug.Log("【Notes】ノーツを張り付け");
        }
    }

}