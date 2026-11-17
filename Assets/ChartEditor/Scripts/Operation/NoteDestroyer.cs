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
            notesGetter.SelectingNotes.Any(note => !NoteLock.IsLocked(note)) &&
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

            // ロックしたノーツは消さない (選択にも残す)
            var datasCopy = notesGetter.SelectingNotes.Where(note => !NoteLock.IsLocked(note)).ToList();
            NoteLock.LogRefused(notesGetter.SelectingNotes.Count - datasCopy.Count, "消し");

            // 何も選択されていないときは何もしない (Undo履歴にも積まない)
            if (datasCopy.Count == 0) { return; }

            // 下のノーツを消すときは、載っているダイナミックノーツも一緒に消す
            // (ダイナミックノーツだけを選んで消したときは、ダイナミックノーツだけが外れる)
            datasCopy = DynamicNoteAttachments.WithPartners(datasCopy, includeDynamics: true, includeHosts: false)
                .Where(note => !NoteLock.IsLocked(note))
                .ToList();

            foreach (var data in datasCopy) { notesSetter.TryRemoveSelectingNotes(data); }

            // ダイナミックノーツを先に消す (ホールドの終点と一緒に消すとき、つながりが変わって決め直す終点の種類が、
            // 消えるダイナミックノーツを見ないように)
            datasCopy = datasCopy.OrderBy(note => DynamicNoteRules.IsDynamic(note.NoteType) ? 0 : 1).ToList();

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

