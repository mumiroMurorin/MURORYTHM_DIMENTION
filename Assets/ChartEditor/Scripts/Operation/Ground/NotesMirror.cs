using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.Notes.NotesMoveRecord;

namespace ChartEditor
{
    public class NotesMirror : MonoBehaviour
    {
        [SerializeField] NoteMover noteMover;

        INotesDataGetter notesGetter;
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
            this.dataGetter = dataGetter;
        }

        public void MirrorSelectingNotes()
        {
            // ŒÃ‚¢ƒAƒhƒŒƒX‚ğ‹L˜^‚µ‚Ä”½“]
            var previousAddress = new List<NoteDataToAddress>();
            foreach (var note in notesGetter.SelectingNotes)
            {
                previousAddress.Add(new NoteDataToAddress(note, note.Address));
                MirrorNote(note);
            }

            // V‚µ‚¢ƒAƒhƒŒƒX‚ğ‹L˜^
            var currentAddress = new List<NoteDataToAddress>();
            foreach (var note in notesGetter.SelectingNotes)
            {
                currentAddress.Add(new NoteDataToAddress(note, note.Address));
            }

            // RedoUndo‚É“o˜^
            RecordNotesMovingMirror(previousAddress, currentAddress);

            Debug.Log($"y”½“]z");
        }

        private void MirrorNote(IDeployableNoteData noteData)
        {
            if(noteData == null) { return; }

            // IMovableObject‚Ìæ“¾
            var noteObject = notesGetter.GetNoteObject(noteData);
            if (!noteObject.TryGetComponent(out IMovableObject movableObject)) { return; }

            // ƒAƒhƒŒƒX‚ÌŒvZ
            var newAddress = new AddressWithinRange(noteData.Address);
            newAddress.MirrorRange();

            noteMover.MoveNote(movableObject, newAddress);
        }
    }

}