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

        // コピーしたノーツの写し (コピーした時点の内容)
        List<CopiedNoteInfo> copiedNotes;

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
            bool isCut = Input.GetKeyDown(KeyCode.X);
            if (!isCopy && !isPaste && !isCut) { return; }

            // 読み込み中・文字の入力中・ほかの操作の途中は受け付けない
            // (全画面プレビューでも、プレビューの上で選んだノーツをコピー・貼り付けできる)
            if (!SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes)) { return; }

            if (isCopy) { CopyNotes(); }
            if (isCut) { CutNotes(); }
            if (isPaste)
            {
                // ホールドを1本だけ編集している間は、ノーツを増やせない
                if (HoldIsolation.IsActive)
                {
                    Debug.Log("【ホールドの単独編集】このホールドだけを編集している間は、貼り付けできません");
                    return;
                }

                PasteNotes();
            }
        }

        /// <summary>
        /// 今、選択中のノーツをコピーできるか (メニューの項目を押せるかどうかに使う)
        /// </summary>
        public bool CanCopySelectedNotes =>
            notesGetter.SelectingNotes != null &&
            notesGetter.SelectingNotes.Count > 0 &&
            SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes, true);

        /// <summary>コピーしてあるノーツの数</summary>
        public int CopiedCount => copiedNotes != null ? copiedNotes.Count : 0;

        NoteDestroyer destroyer;
        NoteDestroyer Destroyer => destroyer != null ? destroyer : (destroyer = FindObjectOfType<NoteDestroyer>(true));

        /// <summary>
        /// 今、選択中のノーツを切り取れるか (コピーできて、消せるとき)
        /// </summary>
        public bool CanCutSelectedNotes =>
            CanCopySelectedNotes && !IsVertexSelectionInPreview && Destroyer != null && Destroyer.CanDestroySelectedNotes;

        /// <summary>全画面プレビューで頂点を選んでいるか (このときの Delete は頂点を消すので、切り取りはしない)</summary>
        bool IsVertexSelectionInPreview =>
            dataGetter.EditNoteType.Value == EditNoteType.Preview && PreviewVertexTools.HasSelectedVertices;

        /// <summary>
        /// 選択中のノーツを切り取る (メニューから。Ctrl+X と同じ)
        /// </summary>
        public void CutSelectedNotes()
        {
            if (!CanCutSelectedNotes) { return; }
            CutNotes();
        }

        /// <summary>
        /// 切り取り: コピーしてから消す (ロックしたノーツは、コピーはするが消さない。元に戻すのは、消した分の1回)
        /// </summary>
        private void CutNotes()
        {
            if (notesGetter.SelectingNotes == null || notesGetter.SelectingNotes.Count == 0) { return; }
            if (IsVertexSelectionInPreview)
            {
                // (コピーだけして、選んでいる頂点が消えてしまわないように)
                Debug.Log("【頂点】頂点を選んでいる間は、ノーツを切り取れません");
                return;
            }

            if (!CopyNotes()) { return; }

            // ダイナミックノーツと下のノーツは、どちらを選んでいても、両方とも切り取る (コピーと同じ)。
            // ルールに合わないダイナミックノーツは、コピーしないので、消さずに残す
            foreach (var partner in DynamicNoteAttachments.WithPartners(notesGetter.SelectingNotes)) { notesSetter.TryAddSelectingNotes(partner); }
            foreach (var note in notesGetter.SelectingNotes.Where(DynamicNoteAttachments.IsViolation).ToList()) { notesSetter.TryRemoveSelectingNotes(note); }

            Destroyer?.DestroySelectedNotes();
            Debug.Log("【Notes】 Cut notes.");
        }

        /// <summary>
        /// 今、メニューから貼り付けられるか (コピーしてあって、グラウンド・スペースのレイヤーのとき)
        /// </summary>
        public bool CanPasteFromMenu =>
            CopiedCount > 0 &&
            !HoldIsolation.IsActive &&
            (dataGetter.EditNoteType.Value == EditNoteType.Ground || dataGetter.EditNoteType.Value == EditNoteType.Space) &&
            SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes, true);

        /// <summary>
        /// コピーしたノーツを、今の再生位置の分線 (判定の線のところ) に貼り付ける (メニューから)
        /// </summary>
        public void PasteAtPlaybackLine()
        {
            if (!CanPasteFromMenu) { return; }

            var chartData = dataGetter.ChartData.Value;
            var timeline = SpaceHoldTimeline.Get(chartData);
            if (timeline == null || timeline.LineCount == 0) { return; }

            // 判定の線の下にある時刻に、いちばん近い分線
            // (判定の線は、オフセットのぶんずれている: CameraController が、判定の線の親 (OffsetAxis) を
            //  オフセット [ms] / 1000 × 拡大率 だけ、譜面の奥 (時刻が進む向き) へ動かす。なので、判定の線の下は
            //  再生位置の時刻 + オフセット [秒] のところ)
            float time = dataGetter.ChartSeconds.Value * dataGetter.PlaybackProgress.Value + dataGetter.Offset.Value / 1000f;
            int best = 0;
            float bestGap = float.MaxValue;
            for (int line = 0; line < timeline.LineCount; line++)
            {
                float gap = Mathf.Abs(timeline.GetTime(line) - time);
                if (gap < bestGap) { bestGap = gap; best = line; }
            }

            PasteAt(new AddressInChart(timeline.GetBar(best), timeline.GetSub(best), 0));
        }

        /// <summary>
        /// コピーしたノーツを、その分線に貼り付ける (右クリックのメニューの「ここに貼り付け」、メニューの「貼り付け」)
        /// </summary>
        public void PasteAt(IReadOnlyAddressInChart address)
        {
            if (address == null || CopiedCount == 0) { return; }
            if (HoldIsolation.IsActive)
            {
                Debug.Log("【ホールドの単独編集】このホールドだけを編集している間は、貼り付けできません");
                return;
            }
            PasteNotes(address);
        }

        /// <summary>
        /// 選択中のノーツをコピーする (メニューから。Ctrl+C と同じ)
        /// </summary>
        public void CopySelectedNotes()
        {
            if (!CanCopySelectedNotes) { return; }

            CopyNotes();
        }

        /// <summary>
        /// 選択中のノーツをコピーする
        ///
        /// コピーした時点の内容(種類・幅・形)と、ノーツどうしの間隔を写し取っておく。
        /// 元のノーツを覚えておくだけだと、コピーした後に元のノーツを動かしたり変えたり消したりしたときに、
        /// 貼り付く位置や内容が変わってしまう。
        /// </summary>
        /// <returns>コピーしたとき true</returns>
        private bool CopyNotes()
        {
            if (notesGetter.SelectingNotes == null || notesGetter.SelectingNotes.Count == 0) { return false; }

            var chartData = dataGetter.ChartData.Value;
            if (chartData == null) { return false; }

            // ダイナミックノーツと下のノーツは、どちらを選んでいても、両方ともコピーする (1つのノーツとして扱う)。
            // ルールに合わないダイナミックノーツ (どのノーツにも載っていないもの) は、コピーしない
            var notes = DynamicNoteAttachments.WithPartners(notesGetter.SelectingNotes);
            int looseCount = notes.RemoveAll(DynamicNoteAttachments.IsViolation);
            DynamicNoteAttachments.LogRefused(looseCount, "ルールに合わないダイナミックノーツは、コピーできません");
            if (notes.Count == 0)
            {
                // (前にコピーしたものを、うっかり貼り付けないように、空にしておく)
                copiedNotes = null;
                return false;
            }

            // いちばん手前のノーツ (貼り付けるとき、このノーツがカーソルの指す分線に来る)
            var firstNote = notes.OrderedByAddress().FirstOrDefault();
            if (firstNote == null) { return false; }
            var firstAddress = new AddressInChart(firstNote.Address);

            copiedNotes = new List<CopiedNoteInfo>();

            foreach (var note in notes)
            {
                // ダイナミックノーツは、下のノーツが一覧の何番目か (貼り付けた後も、そのノーツの写しに載せる)
                var host = DynamicNoteAttachments.GetHostOf(note);
                copiedNotes.Add(new CopiedNoteInfo(
                    CopyNoteData(note),
                    // いちばん手前のノーツから、何分線後ろにあるか
                    chartData.GetSubdivisionDelta(new AddressInChart(note.Address), firstAddress),
                    GetOriginalChainIndex(note),
                    host != null ? notes.IndexOf(host) : -1));
            }

            Debug.Log("【Notes】 Copied notes.");
            return true;
        }

        private void PasteNotes()
        {
            // 貼り付け先 (カーソルが指している分線)。指している場所が無ければ貼り付けない
            PasteNotes(GetPasteCursorAddress());
        }

        private void PasteNotes(IReadOnlyAddressInChart cursorAddress)
        {
            if (copiedNotes == null || copiedNotes.Count == 0) { return; }
            if (dataGetter.ChartData.Value == null) { return; }
            if (cursorAddress == null) { return; }

            // 貼り付けるノーツ。写しから、貼り付けのたびに新しく作る (同じ写しを何度でも貼り付けられるように)
            var pasteNotes = copiedNotes
                .Select(copied => new CopiedNoteInfo(CopyNoteData(copied.NoteData), copied.SubdivisionOffset, copied.OriginalChainIndex, copied.HostIndex))
                .ToList();

            // 貼り付けるダイナミックノーツは、一緒に貼り付ける下のノーツの写しに載せる
            foreach (var pasteNote in pasteNotes)
            {
                if (pasteNote.HostIndex >= 0 && pasteNote.HostIndex < pasteNotes.Count)
                {
                    DynamicNoteAttachments.Remember(pasteNotes[pasteNote.HostIndex].NoteData, pasteNote.NoteData);
                }
            }

            var pasteContext = new PasteContext(new AddressInChart(cursorAddress), CreateChainIndexMap(pasteNotes));

            Record(() => {
                notesSetter.ClearSelectingNotes();
                PasteCopiedNotes(pasteNotes, pasteContext);
            },
            () => {
                DeleteCopiedNotes(pasteNotes);
            });

            Debug.Log("【Notes】 Pasted notes.");
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

        /// <summary>
        /// 貼り付け先の分線 (カーソルが指している場所)。指している場所が無ければ null
        /// </summary>
        private IReadOnlyAddressInChart GetPasteCursorAddress()
        {
            // カーソルがプレビューの上にあるときは、プレビューの中で指している分線
            // (プレビューの画面部品の裏に隠れている譜面のレーンには貼り付けない。ミニプレビューの上では、貼り付けない)
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

        private Dictionary<int, int> CreateChainIndexMap(List<CopiedNoteInfo> pasteNotes)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            HashSet<int> reservedIndexes = new HashSet<int>();

            foreach (var originalChainIndex in pasteNotes
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

        private void PasteCopiedNotes(List<CopiedNoteInfo> pasteNotes, PasteContext pasteContext)
        {
            foreach (var pasteNote in pasteNotes)
            {
                PasteSingleNote(pasteNote, pasteContext);
            }
        }

        private void PasteSingleNote(CopiedNoteInfo pasteNote, PasteContext pasteContext)
        {
            var chartData = dataGetter.ChartData.Value;
            var data = pasteNote.NoteData;

            // 貼り付け先: カーソルが指している分線から、コピーしたときの間隔だけ後ろ。レーンはコピーしたときのまま
            var lanes = data.Address.Range;
            var cursorAddress = pasteContext.CursorAddress;
            var address = chartData.AddressAddition(
                new AddressInChart(cursorAddress.BarIndex, cursorAddress.SubDivisionIndex, lanes[0]),
                pasteNote.SubdivisionOffset);
            data.SetAddress(new AddressWithinRange(address, lanes.Count));

            if (data is IChainNoteData chainData &&
                pasteNote.OriginalChainIndex >= 0 &&
                pasteContext.ChainIndexMap.TryGetValue(pasteNote.OriginalChainIndex, out int newChainIndex))
            {
                chainData.SetChainIndex(newChainIndex);
            }

            chartData.AddNote(data);

            // 譜面に入らなかったノーツ (貼り付け先が譜面の中に無かったとき) は、選択しない
            var noteObject = notesGetter.GetNoteObject(data);
            if (noteObject != null && noteObject.TryGetComponent(out ISelectableNoteObject selectable))
            {
                notesSetter.TryAddSelectingNotes(selectable.NoteObject.NoteData);
            }
        }

        private void DeleteCopiedNotes(List<CopiedNoteInfo> pasteNotes)
        {
            foreach (var pasteNote in pasteNotes)
            {
                // 譜面に入らなかったノーツは、消すものが無い
                if (notesGetter.GetNoteObject(pasteNote.NoteData) == null) { continue; }

                DeleteNote(pasteNote.NoteData);
            }
        }

        private void DeleteNote(IDeployableNoteData data)
        {
            dataGetter.ChartData.Value.RemoveNote(data);
        }

        /// <summary>
        /// コピーしたノーツ1つぶん
        /// </summary>
        private class CopiedNoteInfo
        {
            public CopiedNoteInfo(IDeployableNoteData noteData, int subdivisionOffset, int originalChainIndex, int hostIndex)
            {
                NoteData = noteData;
                SubdivisionOffset = subdivisionOffset;
                OriginalChainIndex = originalChainIndex;
                HostIndex = hostIndex;
            }

            /// <summary>
            /// ノーツの内容 (コピーした時点の種類・幅・形)
            /// </summary>
            public IDeployableNoteData NoteData { get; }

            /// <summary>
            /// コピーしたノーツのうち、いちばん手前のノーツから何分線後ろにあったか
            /// </summary>
            public int SubdivisionOffset { get; }

            /// <summary>
            /// コピーした時点の接続番号 (接続されていなければ -1)。同じ番号だったノーツは、貼り付けた後も互いにつながる
            /// </summary>
            public int OriginalChainIndex { get; }

            /// <summary>
            /// ダイナミックノーツのとき: 載っているノーツが、コピーしたノーツの一覧の何番目か (それ以外は -1)
            /// </summary>
            public int HostIndex { get; }
        }

        private readonly struct PasteContext
        {
            public PasteContext(AddressInChart cursorAddress, Dictionary<int, int> chainIndexMap)
            {
                CursorAddress = cursorAddress;
                ChainIndexMap = chainIndexMap;
            }

            /// <summary>
            /// 貼り付け先の分線 (いちばん手前のノーツが来る場所)
            /// </summary>
            public AddressInChart CursorAddress { get; }

            public Dictionary<int, int> ChainIndexMap { get; }
        }
    }
}
