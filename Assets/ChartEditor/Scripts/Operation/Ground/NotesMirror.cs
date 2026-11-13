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
            var currentAddress = new List<NoteDataToAddress>();
            foreach (var note in notesGetter.SelectingNotes)
            {
                var before = new NoteDataToAddress(note, note.Address);

                // 反転できなかったノート(オブジェクトが無い等)は記録しない
                if (!MirrorNote(note)) { continue; }

                previousAddress.Add(before);
                currentAddress.Add(new NoteDataToAddress(note, note.Address));
            }

            // 反転したノートが無いときは何もしない (Undo履歴にも積まない)
            if (previousAddress.Count == 0) { return; }

            // RedoUndoに登録
            RecordNotesMovingMirror(previousAddress, currentAddress);

            Debug.Log($"【反転】");
        }

        /// <returns>反転したとき true</returns>
        private bool MirrorNote(IDeployableNoteData noteData)
        {
            if(noteData == null) { return false; }

            // IMovableObjectの取得
            var noteObject = notesGetter.GetNoteObject(noteData);
            if (noteObject == null) { return false; }
            if (!noteObject.TryGetComponent(out IMovableObject movableObject)) { return false; }

            // アドレスの計算
            var newAddress = new AddressWithinRange(noteData.Address);
            newAddress.MirrorRange();

            noteMover.MoveNote(movableObject, newAddress);
            return true;
        }
    }

}