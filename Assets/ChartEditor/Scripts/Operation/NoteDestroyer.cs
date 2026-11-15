using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using static UndoRedo.History;

namespace ChartEditor
{
    public class NoteDestroyer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;
        IChartEditorDataGetter dataGetter;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.Connecting,
             EditMode.EditingBarConfig,
             EditMode.EditingSubDivisionConfig,
             EditMode.Moving,
             EditMode.Scaling,
             EditMode.SpaceMoving,
            EditMode.Preview,
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.dataGetter = dataGetter;
        }

        private void Update()
        {
            // Deleteキーで消す
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                // 読み込み中・文字の入力中・ほかの操作の途中は受け付けない
                // (全画面プレビューでも、プレビューの上で選んだノーツを消せる)
                if (!SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes)) { return; }

                DestroyNotes();
            }
        }

        /// <summary>
        /// 今、選択中のノーツを削除できるか (メニューの項目を押せるかどうかに使う)
        /// </summary>
        public bool CanDestroySelectedNotes =>
            notesGetter.SelectingNotes.Count > 0 &&
            SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes, true);

        /// <summary>
        /// 選択中のノーツを削除する (メニューから。Delete キーと同じ)
        /// </summary>
        public void DestroySelectedNotes()
        {
            if (!CanDestroySelectedNotes) { return; }

            DestroyNotes();
        }

        private void DestroyNotes()
        {
            // 全画面プレビューで、頂点を選んでいるとき: ノーツではなく、選んでいる頂点を消す (頂点レイヤーと同じ)
            // (頂点を選んだまま Delete を押して、ノーツごと消えてしまわないように。消せないときも、ノーツは消さない)
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview && PreviewVertexTools.HasSelectedVertices)
            {
                if (!PreviewVertexTools.RemoveSelectedVertices())
                {
                    Debug.Log($"【頂点】これ以上、頂点を消せません。形を作るには、頂点が {PreviewVertexTools.MIN_VERTEX_COUNT} つ以上必要です");
                }
                return;
            }

            var datasCopy = notesGetter.SelectingNotes.ToList();

            // 何も選択されていないときは何もしない (Undo履歴にも積まない)
            if (datasCopy.Count == 0) { return; }

            notesSetter.ClearSelectingNotes();

            // 削除
            Record(() => { 
                foreach(var data in datasCopy) { DestroyNote(data); }
            }, 
            // 配置
            () => {
                foreach (var data in datasCopy) { DeployNote(data); }
            });
        }

        /// <summary>
        /// データの削除
        /// </summary>
        /// <param name="noteData"></param>
        private void DestroyNote(IDeployableNoteData noteData)
        {
            dataGetter.ChartData.Value.RemoveNote(noteData);
        }

        /// <summary>
        /// データの追加
        /// </summary>
        /// <param name="noteData"></param>
        private void DeployNote(IDeployableNoteData noteData)
        {
            dataGetter.ChartData.Value.AddNote(noteData);
        }

    }

}

