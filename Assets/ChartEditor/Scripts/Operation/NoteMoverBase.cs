using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.Notes.NotesMoveRecord;

namespace ChartEditor
{
    /// <summary>
    /// ノーツをドラッグして移動させる操作の共通処理 (グラウンド・スペース共通)
    /// 派生クラスは「どのコライダーを掴み、どこに置けるか」と「置き先のアドレスの求め方」を実装する
    /// </summary>
    public abstract class NoteMoverBase : MonoBehaviour
    {
        protected INotesDataGetter notesGetter;
        protected IChartEditorDataGetter dataGetter;
        protected IChartEditorDataSetter dataSetter;

        readonly List<MoveTarget> targets = new List<MoveTarget>();
        readonly List<NoteDataToAddress> previousAddress = new List<NoteDataToAddress>();
        object lastLocation;
        bool isDragging;

        /// <summary>
        /// 操作待ちのエディットモード
        /// </summary>
        protected abstract EditMode IdleMode { get; }

        /// <summary>
        /// ドラッグ中のエディットモード
        /// </summary>
        protected abstract EditMode DraggingMode { get; }

        /// <summary>
        /// カーソル下の「掴めるノート」を取得する
        /// </summary>
        protected abstract bool TryGetClickedNote(out NoteObject note);

        /// <summary>
        /// ノートから移動用のコンポーネントを取り出す
        /// </summary>
        protected abstract bool TryCreateTarget(NoteObject note, out MoveTarget target);

        /// <summary>
        /// カーソル下の「置ける場所」を取得する
        /// </summary>
        /// <param name="location">場所の同一判定用オブジェクト</param>
        protected abstract bool TryGetDropLocation(out object location, out AddressInChart address);

        /// <summary>
        /// 置き先から、ノートの新しいアドレスを求める
        /// </summary>
        protected abstract AddressWithinRange CalcAddress(MoveTarget target, AddressInChart dropAddress);

        /// <summary>
        /// ドラッグ開始時の追加処理
        /// </summary>
        protected virtual void OnStartDrag(NoteObject clickedNote) { }

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        void LateUpdate()
        {
            var editMode = dataGetter.CurrentEditMode.Value;

            // 移動モードのみ
            if (editMode != IdleMode && editMode != DraggingMode)
            {
                // ドラッグの途中で別のモードに切り替えられたら、その場で移動を確定する
                if (isDragging) { EndDrag(); }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // Shift を押しながらのドラッグは範囲選択なので、ノーツは掴まない
                if (!RangeSelectGesture.IsForced(dataGetter)) { StartDrag(); }
            }
            else if (Input.GetMouseButton(0)) { Drag(); }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                EndDrag();
                dataSetter?.SetEditMode(IdleMode);
            }
        }

        /// <summary>
        /// クリックされた時
        /// </summary>
        private void StartDrag()
        {
            if (isDragging) { EndDrag(); }
            Initialize();

            // 動かせるオブジェクトでなければ返す
            if (!TryGetClickedNote(out var clickedNote)) { return; }

            // 基準となるクリックされたノーツ情報を保存
            var baseAddress = new AddressInChart(clickedNote.NoteData.Address);
            OnStartDrag(clickedNote);

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            skippedLockedCount = 0;
            foreach (var data in notesGetter.SelectingNotes)
            {
                // ロックしたノーツは動かさない
                if (NoteLock.IsLocked(data)) { skippedLockedCount++; continue; }
                // 譜面の読み込み直後など、対応するオブジェクトが無いノートは飛ばす
                var obj = notesGetter.GetNoteObject(data);
                if (obj == null) { continue; }
                if (!TryCreateTarget(obj, out var target)) { continue; }

                // ノーツオブジェクト側の動作
                target.OnMoveStart?.Invoke();

                // 基準ノートとの差分を保存
                target.SubDivisionDelta = dataGetter.ChartData.Value.GetSubdivisionDelta(new AddressInChart(data.Address), baseAddress);
                target.SliderDelta = (int)(data.Address.Range[0] - baseAddress.SliderIndex);
                target.RangeCount = data.Address.Range.Count;

                targets.Add(target);
                previousAddress.Add(new NoteDataToAddress(data, data.Address));
            }

            isDragging = true;
            dataSetter?.SetEditMode(DraggingMode);
        }

        private void Drag()
        {
            if (!isDragging || targets.Count == 0) { return; }

            // カーソル下の親取得
            if (!TryGetDropLocation(out var location, out var dropAddress)) { return; }
            if (ReferenceEquals(lastLocation, location)) { return; }

            // 最新配置場所の更新
            lastLocation = location;

            // アドレスの移動
            foreach (var target in targets)
            {
                target.Note.NoteData.SetAddress(CalcAddress(target, dropAddress));
                target.OnMove?.Invoke();
            }
        }

        /// <summary>
        /// クリック終了時(移動終了時)
        /// </summary>
        private void EndDrag()
        {
            var previous = new List<NoteDataToAddress>();
            var current = new List<NoteDataToAddress>();

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                // ドラッグ中に破棄されたノートには触らない
                if (target.Note == null) { continue; }

                target.OnMoveEnd?.Invoke();

                var noteData = target.Note.NoteData;
                previous.Add(previousAddress[i]);
                current.Add(new NoteDataToAddress(noteData, noteData.Address));
            }

            // 実際に動いたときだけUndo履歴に登録 (そのとき、選択中のロックしたノーツを動かさなかったことも知らせる)
            if (RecordNotesMovingIfChanged(previous, current)) { NoteLock.LogRefused(skippedLockedCount, "動かし"); }

            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        // 掴んだときに、選択中だが、ロックしていたので動かさなかったノーツの数
        int skippedLockedCount;

        private void Initialize()
        {
            isDragging = false;
            skippedLockedCount = 0;
            lastLocation = null;
            targets.Clear();
            previousAddress.Clear();
        }

        /// <summary>
        /// 移動対象のノート
        /// </summary>
        protected class MoveTarget
        {
            public MoveTarget(NoteObject note, Action onMoveStart, Action onMove, Action onMoveEnd)
            {
                Note = note;
                OnMoveStart = onMoveStart;
                OnMove = onMove;
                OnMoveEnd = onMoveEnd;
            }

            public NoteObject Note { get; }
            public Action OnMoveStart { get; }
            public Action OnMove { get; }
            public Action OnMoveEnd { get; }

            /// <summary>
            /// 掴んだノートとの分線数の差
            /// </summary>
            public int SubDivisionDelta { get; set; }

            /// <summary>
            /// 掴んだノートとのレーン番号の差
            /// </summary>
            public int SliderDelta { get; set; }

            /// <summary>
            /// ノートの横幅(レーン数)
            /// </summary>
            public int RangeCount { get; set; }
        }
    }
}
