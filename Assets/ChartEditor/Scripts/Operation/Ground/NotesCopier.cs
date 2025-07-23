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
        Dictionary<IDeployableNoteData, AddressWithinRange> copiedNotes;

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
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V)) { PasteNotes(); }
        }

        /// <summary>
        /// ノーツのコピー
        /// </summary>
        private void CopyNotes()
        {
            if(notesGetter.SelectingNotes == null || notesGetter.SelectingNotes.Count == 0) { return; }

            copiedNotes = new Dictionary<IDeployableNoteData, AddressWithinRange>();

            foreach(var note in notesGetter.SelectingNotes)
            {
                copiedNotes.Add(note, new AddressWithinRange(note.Address));
            }
            Debug.Log("【Notes】ノーツをコピー");
        }

        /// <summary>
        /// ノーツの張り付け
        /// </summary>
        private void PasteNotes()
        {
            var currentEditMode = dataGetter.CurrentEditMode.Value;
            var groundCollider = dataGetter.GetInteractableCollider<IDeployableCollider>();
            var spaceCollider = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();

            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }
            if (copiedNotes == null || copiedNotes.Count == 0) { return; }
            if (groundCollider == null && spaceCollider == null) { return; }

            // コピーされたノーツをコピーしたり
            var copiedNotesCopy = copiedNotes.ToDictionary(
                    pair => pair.Key.Copy(),     
                    pair => pair.Value 
                );
            var firstNoteAddress = copiedNotesCopy.Keys.OrderedByAddress().ToList().First()?.Address;
            var cursorAddress = groundCollider != null ? groundCollider.Address : spaceCollider.Address;
            var subdivisionDelta = dataGetter.ChartData.Value.GetSubdivisionDelta(cursorAddress, new AddressInChart(firstNoteAddress));

            // ペースト
            Record(() => {
                notesSetter.ClearSelectingNotes();    // 全選択解除
                foreach (var pair in copiedNotesCopy) { PasetNote(pair.Key, pair.Value, subdivisionDelta); }
            }, 
            // 削除
            () => {
                foreach (var pair in copiedNotesCopy) { DeleteNote(pair.Key); }
            });

            Debug.Log("【Notes】ノーツを張り付け");

        }

        private void PasetNote(IDeployableNoteData data, AddressWithinRange originAddress, int subdivisionDelta)
        {
            var address = dataGetter.ChartData.Value.AddressAddition(new AddressInChart(originAddress), subdivisionDelta);
            data.SetAddress(new AddressWithinRange(address, data.Address.Range.Count));

            // 配置
            dataGetter.ChartData.Value.AddNote(data);

            // 選択する
            if (notesGetter.GetNoteObject(data).TryGetComponent(out ISelectableNoteObject selectable)) 
            {
                notesSetter.TryAddSelectingNotes(selectable.NoteObject.NoteData);
            }
        }

        private void DeleteNote(IDeployableNoteData data)
        {
            dataGetter.ChartData.Value.RemoveNote(data);
        }
    }

}