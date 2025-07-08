using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

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
            foreach (var note in notesGetter.SelectingNotes)
            {
                MirrorNote(note);
            }

            Debug.Log($"ÅyîΩì]Åz");
        }

        private void MirrorNote(IDeployableNoteData noteData)
        {
            if(noteData == null) { return; }

            // IMovableObjectÇÃéÊìæ
            var noteObject = notesGetter.GetNoteObject(noteData);
            if (!noteObject.TryGetComponent(out IMovableObject movableObject)) { return; }

            // ÉAÉhÉåÉXÇÃåvéZ
            var newAddress = new AddressWithinRange(noteData.Address);
            newAddress.MirrorRange();

            noteMover.MoveNote(movableObject, newAddress);
        }
    }

}