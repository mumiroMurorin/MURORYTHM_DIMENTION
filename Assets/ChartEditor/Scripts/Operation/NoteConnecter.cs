using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using System;
using System.Linq;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// ホールド (スペースホールド) の接続
    ///
    /// ・接続ツール: ノーツを押して接続を始め、つなぐ先を順に押していく。Ctrl を押しながら押すと、接続を外す
    /// ・接続ツールで、複数選択しているノーツの1つを押す: 選択中のノーツをまとめて接続する
    /// ・Ctrl+J (どのツールでも): 選択中のホールド・スペースホールドを、まとめて接続する
    /// ・Shift を押しながら接続する (つなぐ先を押す、Ctrl+Shift+J): 完成した SpaceHold (始点と終点がある) に加わる
    ///   ばらばらのスペースホールドのノーツの形を、その時刻での SpaceHold の断面 (前後の節を補間した形) に合わせる
    ///   (SpaceHoldConnectCorrection)。元に戻すと、形も戻る
    /// </summary>
    public class NoteConnecter : MonoBehaviour
    {
        const int UNCHAINED_INDEX = -1;

        // 選択中のノーツをまとめて接続するショートカットキー (Ctrl と一緒に押す。J = Join)
        const KeyCode CONNECT_SELECTION_KEY = KeyCode.J;

        // 操作の途中なので、まとめて接続するキーを受け付けないモード
        static readonly EditMode[] connectSelectionIgnoreModes =
        {
            EditMode.Connecting,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
            EditMode.CurveHandling,
            EditMode.Explanation,
        };

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        Dictionary<IChainNoteData, List<IDisposable>> noteDataToDisposables = new Dictionary<IChainNoteData, List<IDisposable>>();
        IChainNoteData startNote;
        bool isConnecting;
        int connectingChainIndex;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
        }

        private void Update()
        {
            // Ctrl+J: 選択中のホールド・スペースホールドを、まとめて接続する (どのツールを選んでいても使える)
            // 読み込み中・文字の入力中・ドラッグの途中・ほかの操作の途中は受け付けない
            if (ModifierKey.Ctrl && Input.GetKeyDown(CONNECT_SELECTION_KEY) && !Input.GetMouseButton(0) &&
                SelectedNotesKeyOperation.IsAcceptable(dataGetter, connectSelectionIgnoreModes))
            {
                ConnectSelectingNotes();
            }

            EditMode editMode = dataGetter.CurrentEditMode.Value;

            if(editMode != EditMode.Connect && editMode != EditMode.Connecting && editMode != EditMode.DisConnect) 
            {
                isConnecting = false;
                return; 
            }

            // Ctrl押されている間は接続解除モード
            if (ModifierKey.Ctrl) { dataSetter.SetEditMode(EditMode.DisConnect); }
            // それ以外は接続モード
            else if (isConnecting) { dataSetter.SetEditMode(EditMode.Connecting); }
            else { dataSetter.SetEditMode(EditMode.Connect); }

            editMode = dataGetter.CurrentEditMode.Value;

            // 接続モード開始
            if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connect) { StartConnectOnClick(); }
            // 接続解除
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.DisConnect) { DisConnectNoteOnClick(); }
            // 接続
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connecting) { ConnectNoteOnClick(); }
            // 接続モード終了
            else if (Input.GetMouseButtonDown(1)) { EndConnect(); }
        }

        private static bool ContainsLocked(IEnumerable<IChainNoteData> notes)
        {
            if (!NoteLock.HasAny || notes == null) { return false; }
            foreach (var note in notes)
            {
                if (NoteLock.IsLocked(note)) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 接続モードの開始
        /// </summary>
        private void StartConnectOnClick()
        {
            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if(collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData) { return; }

            var clickedNote = (IChainNoteData)connectableObject.Note.NoteData;

            // 複数選択しているノーツの1つを押したときは、選択中のノーツをまとめて接続して終わる
            if (TryConnectSelectingNotes(clickedNote)) { return; }

            startNote = clickedNote;
            connectingChainIndex = startNote.ChainIndex.Value;

            dataSetter.SetEditMode(EditMode.Connecting);
            isConnecting = true;
        }

        /// <summary>
        /// 複数選択しているノーツの1つを押したとき、選択中の(押したノーツと同じ種類の)ノーツをまとめて接続する
        /// (1つずつ順に押していかなくても、範囲選択してから1回押すだけでつながるように)
        /// </summary>
        /// <param name="clickedNote">押したノーツ</param>
        /// <returns>まとめて接続したとき true。そうでないとき(ふつうの接続の操作に進むとき) false</returns>
        private bool TryConnectSelectingNotes(IChainNoteData clickedNote)
        {
            // どのノーツを、どの接続番号に入れるかを決める
            // (押したノーツが選択中でないとき、選択中の接続できるノーツが1つだけのとき、すでに全部つながっているときは、ここで終わる)
            var members = new List<IChainNoteData>();
            var originIndices = new List<int>();
            if (!ChainConnectPlanner.TryPlan(
                    clickedNote,
                    notesGetter.SelectingNotes,
                    note => notesGetter.GetNoteObject(note) != null && !NoteLock.IsLocked(note),
                    index => notesGetter.GetChainNoteList(index)?.ChainNoteList,
                    notesGetter.GetUsableChainNoteIndex,
                    members,
                    originIndices,
                    out int chainIndex))
            {
                return false;
            }

            // つながりごと加わるノーツに、ロックしたノーツがあれば、つながない (ロックしたノーツの接続や種類を変えないように)
            if (ContainsLocked(members))
            {
                Debug.Log("【ロック】ロックしたノーツを含むつながりは、つなげません (外すには、右クリックのメニューの「ロックを外す」)");
                return true;
            }

            // Shift を押していたら、完成した SpaceHold に加わるノーツの形を、その時刻の断面に合わせる
            var corrections = PlanSpaceHoldCorrections(members);

            // 接続
            Record(() => {
                foreach (var member in members)
                {
                    ConnectNote(member, chainIndex);
                }
                SpaceHoldConnectCorrection.ApplyAfter(corrections);
            },
            // 元の接続に戻す (未接続だったノーツは、未接続に戻る)
            () => {
                SpaceHoldConnectCorrection.ApplyBefore(corrections);
                for (int i = 0; i < members.Count; i++)
                {
                    DisconnectNote(members[i]);
                    ConnectNote(members[i], originIndices[i]);
                }
            });

            LogCorrections(corrections);
            return true;
        }

        /// <summary>
        /// Shift を押しながら接続したとき、完成した SpaceHold (始点と終点がある) に加わるノーツの形を、
        /// その時刻での SpaceHold の断面 (前後の節を補間した形) に合わせる段取りを決める (SpaceHoldConnectCorrection)
        /// Shift を押していないとき、合わせるノーツが無いときは、空の一覧
        /// </summary>
        private List<SpaceHoldConnectCorrection.Correction> PlanSpaceHoldCorrections(IEnumerable<IChainNoteData> joiningNotes)
        {
            var corrections = new List<SpaceHoldConnectCorrection.Correction>();
            if (!SpaceHoldConnectCorrection.IsRequested) { return corrections; }

            SpaceHoldConnectCorrection.TryPlan(notesGetter, dataGetter.ChartData.Value, joiningNotes, corrections);
            return corrections;
        }

        private static void LogCorrections(List<SpaceHoldConnectCorrection.Correction> corrections)
        {
            if (corrections.Count == 0) { return; }

            Debug.Log($"【接続】SpaceHold に加えたノーツ ({corrections.Count} 個) の形を、その時刻の断面に合わせました");
        }

        /// <summary>
        /// 選択中のホールド・スペースホールドを、種類ごとにまとめて接続する (Ctrl+J)
        ///
        /// 接続ツールで、複数選択しているノーツの1つを押したときと同じ結果になる。
        /// ホールド同士、スペースホールド同士を、それぞれ1本につなぐ。すでにつながっているノーツは、その接続ごと入る。
        /// 接続番号は、選択中のノーツの中で最初に見つかった接続済みのノーツのもの。どれも未接続なら、新しい番号。
        /// 元に戻す操作は、両方の種類をまとめて1回ぶん。
        /// </summary>
        /// <returns>1つでも接続したとき true</returns>
        public bool ConnectSelectingNotes()
        {
            // どのノーツを、どの接続番号に入れるかを決める (種類ごとに、別の接続になる)
            var members = new List<IChainNoteData>();
            var originIndices = new List<int>();
            var targetIndices = new List<int>();
            ChainConnectPlanner.TryPlanSelection(
                notesGetter.SelectingNotes,
                note => notesGetter.GetNoteObject(note) != null && !NoteLock.IsLocked(note),
                index => notesGetter.GetChainNoteList(index)?.ChainNoteList,
                index => notesGetter.GetChainNoteList(index) != null,
                members,
                originIndices,
                targetIndices);

            if (members.Count == 0)
            {
                Debug.Log("【接続】接続できるノーツがありません。同じ種類のホールド (スペースホールド) を2つ以上選んでから、Ctrl+J を押してください");
                return false;
            }

            // つながりごと加わるノーツに、ロックしたノーツがあれば、つながない
            if (ContainsLocked(members))
            {
                Debug.Log("【ロック】ロックしたノーツを含むつながりは、つなげません (外すには、右クリックのメニューの「ロックを外す」)");
                return false;
            }

            // Shift を押していたら (Ctrl+Shift+J)、完成した SpaceHold に加わるノーツの形を、その時刻の断面に合わせる
            var corrections = PlanSpaceHoldCorrections(members);

            // 接続
            Record(() => {
                for (int i = 0; i < members.Count; i++)
                {
                    ConnectNote(members[i], targetIndices[i]);
                }
                SpaceHoldConnectCorrection.ApplyAfter(corrections);
            },
            // 元の接続に戻す (未接続だったノーツは、未接続に戻る)
            () => {
                SpaceHoldConnectCorrection.ApplyBefore(corrections);
                for (int i = 0; i < members.Count; i++)
                {
                    DisconnectNote(members[i]);
                    ConnectNote(members[i], originIndices[i]);
                }
            });

            Debug.Log($"【接続】選択中のノーツを接続しました ({members.Count} 個)");
            LogCorrections(corrections);
            return true;
        }

        /// <summary>
        /// ノーツの接続
        /// </summary>
        private void ConnectNoteOnClick()
        {
            // 接続元のノートが削除されていたら(Undoなど)、接続をやめる
            if (startNote == null || notesGetter.GetNoteObject(startNote) == null)
            {
                startNote = null;
                EndConnect();
                return;
            }

            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if (collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData addNote) { return; }

            // 接続番号の取得
            // 接続元も追加ノートも番号を持っていなかったとき(両ノート未接続時)、新規取得
            if (connectingChainIndex == UNCHAINED_INDEX && addNote.ChainIndex.Value == UNCHAINED_INDEX)
            {
                connectingChainIndex = notesGetter.GetUsableChainNoteIndex();
            }
            // 追加ノートが番号を持っていたとき、番号を更新
            // 元ノートが番号を持っていた場合は何もしない(connectingChainIndexに既に代入済)
            else if (connectingChainIndex == UNCHAINED_INDEX && addNote.ChainIndex.Value != UNCHAINED_INDEX)
            {
                connectingChainIndex = addNote.ChainIndex.Value;
            }

            var chainList = notesGetter.GetChainNoteList(addNote.ChainIndex.Value)?.ChainNoteList.ToList();
            // つなぐノーツと、そのつながりごと加わるノーツに、ロックしたノーツがあれば、つながない
            if (NoteLock.IsLocked(startNote) || NoteLock.IsLocked(addNote) || (chainList != null && ContainsLocked(chainList)))
            {
                Debug.Log("【ロック】ロックしたノーツを含むつながりは、つなげません (外すには、右クリックのメニューの「ロックを外す」)");
                return;
            }
            int chainListIndexOrigin = chainList == null || chainList.Count == 0 ? -1 : chainList[0].ChainIndex.Value;
            var startNoteCopy = startNote;
            int startNoteIndexOrigin = startNote.ChainIndex.Value;
            int connectingChainIndexCopy = connectingChainIndex;

            // Shift を押しながら接続したら、完成した SpaceHold に加わるノーツの形を、その時刻の断面に合わせる
            var corrections = PlanSpaceHoldCorrections(new IChainNoteData[] { startNote, addNote });

            // 接続
            Record(() => {
                ConnectClickedNotes();
                SpaceHoldConnectCorrection.ApplyAfter(corrections);
            },
            // 接続解除
            () => {
                SpaceHoldConnectCorrection.ApplyBefore(corrections);
                DisconnectClickedNotes();
            });

            LogCorrections(corrections);

            void ConnectClickedNotes()
            {
                // 繋がってるノーツは全部繋げる
                if (chainList != null)
                {
                    ConnectNote(startNoteCopy, connectingChainIndexCopy);
                    foreach (var chain in chainList)
                    {
                        ConnectNote(chain, connectingChainIndexCopy);
                    }
                }
                // つながってないとき
                else
                {
                    ConnectNote(startNoteCopy, connectingChainIndexCopy);
                    ConnectNote(addNote, connectingChainIndexCopy);
                }
            }

            void DisconnectClickedNotes()
            {
                // 繋がってたノーツは全部繋げて元に戻す
                if (chainList != null)
                {
                    DisconnectNote(startNoteCopy);
                    ConnectNote(startNoteCopy, startNoteIndexOrigin);

                    foreach (var chain in chainList)
                    {
                        DisconnectNote(chain);
                        ConnectNote(chain, chainListIndexOrigin);
                    }
                }
                // 別に繋がってなかったら接続解除するだけ
                else
                {
                    DisconnectNote(startNoteCopy);
                    ConnectNote(startNoteCopy, startNoteIndexOrigin);

                    DisconnectNote(addNote);
                }
            }
        }

        /// <summary>
        /// ノーツの接続解除
        /// </summary>
        private void DisConnectNoteOnClick()
        {
            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if (collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData removeNote) { return; }

            // 接続されていないノートは何もしない (Undo履歴にも積まない)
            int chainIndexOrigin = removeNote.ChainIndex.Value;
            if (chainIndexOrigin == UNCHAINED_INDEX) { return; }

            // 接続解除
            Record(() => {
                DisconnectNote(removeNote);
            },
            // 元の接続に戻す
            () => {
                ConnectNote(removeNote, chainIndexOrigin);
            });
        }

        /// <summary>
        /// ノートの接続
        /// </summary>
        /// <param name="chainNote"></param>
        /// <param name="chainIndex"></param>
        public void ConnectNote(IChainNoteData chainNote, int chainIndex)
        {
            if(chainIndex == -1) { return; }

            // データの入れ替え
            notesGetter.RemoveChainNote(chainNote);
            chainNote.SetChainIndex(chainIndex);
            notesGetter.AddChainNote(chainNote);

            // ノート順のアップデート
            UpdateChainNoteObj(chainNote, chainIndex);

            // すでに購読されていたとき、破棄
            if (noteDataToDisposables.TryGetValue(chainNote, out var disposables))
            {
                foreach(var dis in disposables) { dis.Dispose(); }
                noteDataToDisposables.Remove(chainNote);
            }

            // 購読
            var noteList = notesGetter.GetChainNoteList(chainIndex);
            var disposable1 = noteList.ChainNoteList.ObserveCountChanged()
                .Subscribe(_ => {
                    UpdateChainNoteObj(chainNote, chainIndex);
                })
                .AddTo(this.gameObject);

            var disposable2 = chainNote.Address.BarIndexRP
                .Subscribe(_ => { noteList.UpdateChainNoteData(chainNote); })
                .AddTo(this.gameObject);

            var disposable3 = chainNote.Address.SubDivisionIndexRP
                .Subscribe(_ => { noteList.UpdateChainNoteData(chainNote); })
                .AddTo(this.gameObject);

            // 購読データの追加、更新
            var disList = new List<IDisposable>() { disposable1, disposable2, disposable3 };
            if (!noteDataToDisposables.TryAdd(chainNote, disList))
            {
                noteDataToDisposables[chainNote] = disList;
            }
        }

        /// <summary>
        /// ノートの接続解除
        /// </summary>
        /// <param name="chainNote"></param>
        private void DisconnectNote(IChainNoteData chainNote)
        {
            notesGetter.RemoveChainNote(chainNote);
            chainNote.SetChainIndex(-1);
            UpdateChainNoteObj(chainNote, -1);

            // 購読の破棄
            ReleaseNote(chainNote);
        }

        /// <summary>
        /// ノートに対する購読を破棄する (ノートが譜面から削除されたとき用。接続番号は変更しない)
        /// </summary>
        public void ReleaseNote(IChainNoteData chainNote)
        {
            if (!noteDataToDisposables.TryGetValue(chainNote, out var disposables)) { return; }

            foreach (var dis in disposables) { dis.Dispose(); }
            noteDataToDisposables.Remove(chainNote);
        }

        /// <summary>
        /// すべてのノートに対する購読を破棄する (譜面が入れ替わったとき用)
        /// </summary>
        public void ReleaseAllNotes()
        {
            foreach (var disposables in noteDataToDisposables.Values)
            {
                foreach (var dis in disposables) { dis.Dispose(); }
            }

            noteDataToDisposables.Clear();
            startNote = null;
            isConnecting = false;
        }


        /// <summary>
        /// ノートの前後を更新
        /// </summary>
        /// <param name="chainNote"></param>
        /// <param name="chainIndex"></param>
        private void UpdateChainNoteObj(IChainNoteData chainNote, int chainIndex)
        {
            // NextNoteとBackNoteを更新する
            var noteList = notesGetter.GetChainNoteList(chainIndex);
            int index = noteList != null ? noteList.IndexOf(chainNote) : -1;

            if(index < 0) 
            {
                chainNote.NoteObject.SetBackNote(null);
                chainNote.NoteObject.SetNextNote(null);
                chainNote.NoteObject.SetSubdivisionOrder(0);
                return;
            }

            var backNoteObj = index > 0 ? noteList.ChainNoteList[index - 1].NoteObject : null;
            var nextNoteObj = index < noteList.ChainNoteList.Count - 1 ? noteList.ChainNoteList[index + 1].NoteObject : null;

            chainNote.NoteObject.SetBackNote(backNoteObj);
            chainNote.NoteObject.SetNextNote(nextNoteObj);

            // 同じ分線に、同じ接続のノーツが並んでいるとき、その中で何番目かを伝える (レーン画面で、ずらして見せるため)
            int subdivisionOrder = 0;
            for (int i = index - 1; i >= 0; i--)
            {
                if (!SortedChainNoteDataList.IsSameSubdivision(noteList.ChainNoteList[i], chainNote)) { break; }

                subdivisionOrder++;
            }

            chainNote.NoteObject.SetSubdivisionOrder(subdivisionOrder);
        }

        /// <summary>
        /// 接続モードの終了
        /// </summary>
        private void EndConnect()
        {
            if (dataGetter.CurrentEditMode.Value != EditMode.Connecting) { return; }

            dataSetter.SetEditMode(EditMode.Connect);
            isConnecting = false;
        }
    }
}