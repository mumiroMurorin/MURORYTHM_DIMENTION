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
            // 古いアドレスを記録して反転
            var previousAddress = new List<NoteDataToAddress>();
            foreach (var note in notesGetter.SelectingNotes)
            {
                previousAddress.Add(new NoteDataToAddress(note, note.Address));
                MirrorNote(note);
            }

            // 新しいアドレスを記録
            var currentAddress = new List<NoteDataToAddress>();
            foreach (var note in notesGetter.SelectingNotes)
            {
                currentAddress.Add(new NoteDataToAddress(note, note.Address));
            }

            // RedoUndoに登録
            RecordNotesMovingMirror(previousAddress, currentAddress);

            Debug.Log($"【反転】");
        }

        private void MirrorNote(IDeployableNoteData noteData)
        {
            if(noteData == null) { return; }

            // IMovableObjectの取得
            var noteObject = notesGetter.GetNoteObject(noteData);
            if (!noteObject.TryGetComponent(out IMovableObject movableObject)) { return; }

            // アドレスの計算
            var newAddress = new AddressWithinRange(noteData.Address);
            newAddress.MirrorRange();

            noteMover.MoveNote(movableObject, newAddress);
        }
    }

}