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
        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;
        IChartEditorDataGetter dataGetter;
        IPreviewPointer previewPointer;
        Dictionary<IDeployableNoteData, AddressWithinRange> copiedNotes;

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
        public void Construct(
            IChartEditorDataGetter dataGetter,
            IChartEditorDataSetter dataSetter,
            INotesDataGetter notesGetter,
            INotesDataSetter notesSetter,
            IPreviewPointer previewPointer)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.dataGetter = dataGetter;
            this.previewPointer = previewPointer;
        }

        void Update()
        {
            if (!ModifierKey.Ctrl) { return; }

            bool isCopy = Input.GetKeyDown(KeyCode.C);
            bool isPaste = Input.GetKeyDown(KeyCode.V);
            if (!isCopy && !isPaste) { return; }

            // 読み込み中・文字の入力中・ほかの操作の途中は受け付けない
            // (全画面プレビューでも、プレビューの上で選んだノーツをコピー・貼り付けできる)
            if (!SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes)) { return; }

            if (isCopy) { CopyNotes(); }
            if (isPaste) { PasteNotes(); }
        }

        private void CopyNotes()
        {
            if (notesGetter.SelectingNotes == null || notesGetter.SelectingNotes.Count == 0) { return; }

            copiedNotes = new Dictionary<IDeployableNoteData, AddressWithinRange>();

            foreach (var note in notesGetter.SelectingNotes)
            {
                copiedNotes.Add(note, new AddressWithinRange(note.Address));
            }

            Debug.Log("【Notes】 Copied notes.");
        }

        private void PasteNotes()
        {
            if (copiedNotes == null || copiedNotes.Count == 0) { return; }

            // 貼り付け先 (カーソルが指している分線)。指している場所が無ければ貼り付けない
            var cursorAddress = GetPasteCursorAddress();
            if (cursorAddress == null) { return; }

            var copiedNotesCopy = CreateCopiedNoteInfos();
            if (copiedNotesCopy.Count == 0) { return; }

            if (!TryCreatePasteContext(copiedNotesCopy, cursorAddress, out var pasteContext)) { return; }

            Record(() => {
                notesSetter.ClearSelectingNotes();
                PasteCopiedNotes(copiedNotesCopy, pasteContext);
            },
            () => {
                DeleteCopiedNotes(copiedNotesCopy);
            });

            Debug.Log("【Notes】 Pasted notes.");
        }

        private List<CopiedNoteInfo> CreateCopiedNoteInfos()
        {
            return copiedNotes
                .Select(pair => new CopiedNoteInfo(
                    CopyNoteData(pair.Key),
                    pair.Value,
                    GetOriginalChainIndex(pair.Key)))
                .ToList();
        }

        private IDeployableNoteData CopyNoteData(IDeployableNoteData sourceNote)
        {
            var copiedNote = sourceNote.Copy();

            // Keep any user-changed note type even if a data class forgets to copy it.
            if (sourceNote is ITypeChangableNoteData sourceTypeChangable &&
                copiedNote is ITypeChangableNoteData copiedTypeChangable)
            {
                copiedTypeChangable.SetNoteType(sourceTypeChangable.NoteTypeRP.Value);
            }

            return copiedNote;
        }

        private int GetOriginalChainIndex(IDeployableNoteData noteData)
        {
            return noteData is IChainNoteData chainData ? chainData.ChainIndex.Value : -1;
        }

        private bool TryCreatePasteContext(List<CopiedNoteInfo> copiedNotesCopy, IReadOnlyAddressInChart cursorAddress, out PasteContext pasteContext)
        {
            pasteContext = default;

            var firstNoteAddress = copiedNotesCopy
                .Select(x => x.NoteData)
                .OrderedByAddress()
                .FirstOrDefault()
                ?.Address;
            if (firstNoteAddress == null) { return false; }

            pasteContext = new PasteContext(
                dataGetter.ChartData.Value.GetSubdivisionDelta(cursorAddress, new AddressInChart(firstNoteAddress)),
                CreateChainIndexMap(copiedNotesCopy));

            return true;
        }

        /// <summary>
        /// 貼り付け先の分線 (カーソルが指している場所)。指している場所が無ければ null
        /// </summary>
        private IReadOnlyAddressInChart GetPasteCursorAddress()
        {
            // カーソルがプレビューの上にあるときは、プレビューの中で指している分線
            // (プレビューの画面部品の裏に隠れている譜面のレーンには貼り付けない)
            if (previewPointer != null && previewPointer.IsPointerOnPreview)
            {
                return previewPointer.TryGetPointedAddress(out var previewAddress) ? previewAddress : null;
            }

            // 全画面プレビューの間は、譜面のレーンは画面に出ていない
            if (dataGetter.EditNoteType.Value == EditNoteType.Preview) { return null; }

            var groundCollider = dataGetter.GetInteractableCollider<IDeployableCollider>();
            if (groundCollider != null) { return groundCollider.Address; }

            var spaceCollider = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();
            return spaceCollider?.Address;
        }

        private Dictionary<int, int> CreateChainIndexMap(List<CopiedNoteInfo> copiedNotesCopy)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            HashSet<int> reservedIndexes = new HashSet<int>();

            foreach (var originalChainIndex in copiedNotesCopy
                         .Select(x => x.OriginalChainIndex)
                         .Where(index => index >= 0)
                         .Distinct()
                         .OrderBy(index => index))
            {
                int newChainIndex = GetAvailableChainIndex(reservedIndexes);
                reservedIndexes.Add(newChainIndex);
                map[originalChainIndex] = newChainIndex;
            }

            return map;
        }

        private int GetAvailableChainIndex(HashSet<int> reservedIndexes)
        {
            int index = notesGetter.GetUsableChainNoteIndex();

            while (reservedIndexes.Contains(index) || notesGetter.GetChainNoteList(index) != null)
            {
                index++;
            }

            return index;
        }

        private void PasteCopiedNotes(List<CopiedNoteInfo> copiedNotesCopy, PasteContext pasteContext)
        {
            foreach (var copied in copiedNotesCopy)
            {
                PasteSingleNote(copied, pasteContext);
            }
        }

        private void PasteSingleNote(CopiedNoteInfo copiedNote, PasteContext pasteContext)
        {
            var data = copiedNote.NoteData;
            var address = dataGetter.ChartData.Value.AddressAddition(new AddressInChart(copiedNote.OriginAddress), pasteContext.SubdivisionDelta);
            data.SetAddress(new AddressWithinRange(address, data.Address.Range.Count));

            if (data is IChainNoteData chainData &&
                copiedNote.OriginalChainIndex >= 0 &&
                pasteContext.ChainIndexMap.TryGetValue(copiedNote.OriginalChainIndex, out int newChainIndex))
            {
                chainData.SetChainIndex(newChainIndex);
            }

            dataGetter.ChartData.Value.AddNote(data);

            if (notesGetter.GetNoteObject(data).TryGetComponent(out ISelectableNoteObject selectable))
            {
                notesSetter.TryAddSelectingNotes(selectable.NoteObject.NoteData);
            }
        }

        private void DeleteCopiedNotes(List<CopiedNoteInfo> copiedNotesCopy)
        {
            foreach (var copied in copiedNotesCopy)
            {
                DeleteNote(copied.NoteData);
            }
        }

        private void DeleteNote(IDeployableNoteData data)
        {
            dataGetter.ChartData.Value.RemoveNote(data);
        }

        private class CopiedNoteInfo
        {
            public CopiedNoteInfo(IDeployableNoteData noteData, AddressWithinRange originAddress, int originalChainIndex)
            {
                NoteData = noteData;
                OriginAddress = originAddress;
                OriginalChainIndex = originalChainIndex;
            }

            public IDeployableNoteData NoteData { get; }

            public AddressWithinRange OriginAddress { get; }

            public int OriginalChainIndex { get; }
        }

        private readonly struct PasteContext
        {
            public PasteContext(int subdivisionDelta, Dictionary<int, int> chainIndexMap)
            {
                SubdivisionDelta = subdivisionDelta;
                ChainIndexMap = chainIndexMap;
            }

            public int SubdivisionDelta { get; }

            public Dictionary<int, int> ChainIndexMap { get; }
        }
    }
}
