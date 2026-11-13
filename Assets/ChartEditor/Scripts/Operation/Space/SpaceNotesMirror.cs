using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class SpaceNotesMirror : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        public void MirrorSelectingSpaceNotes()
        {
            if (dataGetter == null || notesGetter == null) { return; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }
            if (notesGetter.SelectingNotes == null || notesGetter.SelectingNotes.Count == 0) { return; }

            // 反転対象のノートを取り出す
            var targets = new List<IVerticesControlableNoteData>();
            foreach (var note in notesGetter.SelectingNotes)
            {
                if (note is IVerticesControlableNoteData verticesNote) { targets.Add(verticesNote); }
            }

            if (targets.Count == 0) { return; }

            // 左右反転は「座標の鏡映 + 頂点順の逆転」で、もう一度行うと完全に元へ戻る
            // 座標だけを記録すると頂点順が戻らないため、操作そのものを Undo / Redo に登録する
            Record(() => {
                foreach (var target in targets) { MirrorVerticesLeftRight(target); }
            },
            () => {
                foreach (var target in targets) { MirrorVerticesLeftRight(target); }
            });
        }

        void MirrorVerticesLeftRight(IVerticesControlableNoteData verticesNote)
        {
            verticesNote.SpaceVertices.ReverseVertices(new Vector2(0f, 1f), new Vector2(0f, -1f));
        }
    }
}
