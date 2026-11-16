using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面 (グラウンド・スペースのレイヤー) で、ノーツを右クリックしたときのメニュー (EditorContextMenuView に出す)
    ///
    /// 右クリックしたノーツが選択中でなければ、そのノーツだけを選んでから、メニューを出す (ロックしたノーツは選ばない)。
    /// 出す項目 (そのノーツと選択で、できるものだけ):
    /// ・選択中のノーツを接続 (Ctrl+J と同じ)
    /// ・SpaceHold: この節を頂点レイヤーで編集、ホールド全体を3Dで編集。SpaceBreak: 形を頂点レイヤーで編集
    /// ・グラウンドのホールド: このホールドだけを編集 (ダブルクリックと同じ)
    /// ・種類を変える (タイプ変更の道具と同じ変わり方。選択中の、同じ種類のノーツもまとめて変える)
    ///   例: タッチノート → 神タッチノート、→ダイナミック → ←ダイナミック
    /// ・コピー、削除
    /// ・ロックする / ロックを外す (NoteLock。すべてのロックを外すのは、メニューバーの「譜面」)
    /// </summary>
    public class NoteContextMenu : ITickable
    {
        // メニューを出してよいモード (編集の道具・接続・タイプ変更の道具で、ほかの操作の途中でないとき)
        static readonly EditMode[] acceptedModes =
        {
            EditMode.None,
            EditMode.Deploy,
            EditMode.Move,
            EditMode.Scale,
            EditMode.CurveHandle,
            EditMode.SpaceDeploy,
            EditMode.SpaceMove,
            EditMode.Connect,
            EditMode.DisConnect,
            EditMode.ChangeType,
        };

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;

        CursorInteracter cursorInteracter;
        NoteConnecter connecter;
        NotesCopier copier;
        NoteDestroyer destroyer;
        bool isSceneSearched;

        public NoteContextMenu(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        void ITickable.Tick()
        {
            if (!Input.GetMouseButtonDown(1)) { return; }
            if (!CanOpen()) { return; }

            SearchSceneOnce();
            if (cursorInteracter == null) { return; }

            IDeployableNoteData note = cursorInteracter.FindNoteUnderCursor();
            if (note == null) { return; }

            Open(note);
        }

        private bool CanOpen()
        {
            if (dataGetter.ChartData.Value == null || dataGetter.IsLoadingChart.Value) { return false; }
            if (EditorMenuBar.IsBlockingChartInput || InputFieldFocus.IsTyping) { return false; }

            var layer = dataGetter.EditNoteType.Value;
            if (layer != EditNoteType.Ground && layer != EditNoteType.Space) { return false; }
            if (!dataGetter.CurrentEditMode.Value.IsInEditModeList(acceptedModes)) { return false; }

            // 左ボタンを押している間 (ドラッグの途中) と、画面部品の上は出さない
            if (Input.GetMouseButton(0)) { return false; }
            // ホールドの曲線の印の上は、印のメニューを出す (HoldCurveEditor)
            if (HoldCurveEditor.IsCapturingPointer) { return false; }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return false; }
            return true;
        }

        private void SearchSceneOnce()
        {
            if (isSceneSearched) { return; }
            isSceneSearched = true;

            cursorInteracter = UnityEngine.Object.FindObjectOfType<CursorInteracter>(true);
            connecter = UnityEngine.Object.FindObjectOfType<NoteConnecter>(true);
            copier = UnityEngine.Object.FindObjectOfType<NotesCopier>(true);
            destroyer = UnityEngine.Object.FindObjectOfType<NoteDestroyer>(true);
        }

        private void Open(IDeployableNoteData note)
        {
            bool isLocked = NoteLock.IsLocked(note);

            // 選択中でなければ、そのノーツだけを選ぶ (クリックしたときと同じ。ロックしたノーツも選べる)
            if (!Contains(notesGetter.SelectingNotes, note))
            {
                notesSetter.ClearSelectingNotes();
                notesSetter.TryAddSelectingNotes(note);
            }

            var items = new List<EditorContextMenuView.Item>();
            var selection = new List<IDeployableNoteData>(notesGetter.SelectingNotes);
            // 変える操作は、ロックしていないノーツだけが対象 (コピーは、ロックしたノーツもできる)
            var unlocked = selection.FindAll(selected => !NoteLock.IsLocked(selected));
            var lockedSelection = selection.FindAll(NoteLock.IsLocked);

            if (!isLocked)
            {
                AddConnectItem(items, unlocked);
                AddEditItems(items, note);
                AddTypeItem(items, note, unlocked);
            }

            if (copier != null && copier.CanCopySelectedNotes) { items.Add(new EditorContextMenuView.Item("コピー (Ctrl+C)", copier.CopySelectedNotes)); }

            if (!isLocked)
            {
                if (destroyer != null && destroyer.CanDestroySelectedNotes)
                {
                    items.Add(new EditorContextMenuView.Item(unlocked.Count > 1 ? $"削除 ({unlocked.Count} 個)" : "削除", destroyer.DestroySelectedNotes));
                }

                var lockTargets = unlocked.Count > 0 ? unlocked : new List<IDeployableNoteData> { note };
                items.Add(new EditorContextMenuView.Item(lockTargets.Count > 1 ? $"ロックする ({lockTargets.Count} 個)" : "ロックする",
                    () => NoteLock.Lock(lockTargets)));
            }

            if (lockedSelection.Count > 0)
            {
                items.Add(new EditorContextMenuView.Item(lockedSelection.Count > 1 ? $"ロックを外す ({lockedSelection.Count} 個)" : "ロックを外す",
                    () => NoteLock.Unlock(lockedSelection)));
            }

            // (すべてのロックを外すのは、メニューバーの「譜面」から (録音 20261116_235216))

            EditorContextMenuView.Show(Input.mousePosition, items);
        }

        private void AddConnectItem(List<EditorContextMenuView.Item> items, List<IDeployableNoteData> selection)
        {
            if (connecter == null) { return; }

            int chainNotes = 0;
            foreach (var selected in selection)
            {
                if (selected is IChainNoteData) { chainNotes++; }
            }
            if (chainNotes < 2) { return; }

            items.Add(new EditorContextMenuView.Item("選択中のノーツを接続 (Ctrl+J)", () =>
            {
                if (!connecter.ConnectSelectingNotes()) { Debug.Log("【接続】接続できるノーツがありません (すでにつながっているか、種類が違います)"); }
            }));
        }

        private void AddEditItems(List<EditorContextMenuView.Item> items, IDeployableNoteData note)
        {
            switch (note)
            {
                case NoteData_SpaceHold spaceHold:
                    items.Add(new EditorContextMenuView.Item("この節を頂点レイヤーで編集", () => OpenInVerticesLayer(spaceHold)));
                    if (SpaceHold3DEditor.Instance != null)
                    {
                        items.Add(new EditorContextMenuView.Item("ホールド全体を3Dで編集", () => SpaceHold3DEditor.Instance.Open(spaceHold)));
                    }
                    break;
                case NoteData_SpaceBreak spaceBreak:
                    items.Add(new EditorContextMenuView.Item("形を頂点レイヤーで編集", () => OpenInVerticesLayer(spaceBreak)));
                    break;
                case NoteData_Hold hold:
                    if (!HoldIsolation.IsActive) { items.Add(new EditorContextMenuView.Item("このホールドだけを編集", () => HoldIsolation.TryEnter(hold))); }
                    break;
            }
        }

        private void OpenInVerticesLayer(IVerticesControlableNoteData note)
        {
            dataSetter.SetEditNoteType(EditNoteType.Vertices);
            notesSetter.SetEditingVertices(note);
        }

        /// <summary>
        /// 種類を変える項目 (タイプ変更の道具と同じ変わり方)。選択中の、同じ種類のノーツも、まとめて変える (元に戻すのは1回ぶん)
        /// </summary>
        private void AddTypeItem(List<EditorContextMenuView.Item> items, IDeployableNoteData note, List<IDeployableNoteData> selection)
        {
            if (note is not ITypeChangableNoteData changable) { return; }

            DeploymentNoteType current = changable.NoteTypeRP.Value;
            DeploymentNoteType next = NoteTypeCycle.NextNoteType(current);
            if (next == current) { return; }

            string name = GetTypeName(next);
            if (string.IsNullOrEmpty(name)) { return; }

            var targets = new List<ITypeChangableNoteData> { changable };
            foreach (var selected in selection)
            {
                if (ReferenceEquals(selected, note)) { continue; }
                if (selected is ITypeChangableNoteData other && other.NoteTypeRP.Value == current) { targets.Add(other); }
            }

            string label = targets.Count > 1 ? $"{name}にする ({targets.Count} 個)" : $"{name}にする";
            items.Add(new EditorContextMenuView.Item(label, () =>
            {
                // もう一度変えると、元の種類に戻る
                Record(() => { foreach (var target in targets) { target.ChangeNoteType(); } },
                       () => { foreach (var target in targets) { target.ChangeNoteType(); } });
            }));
        }

        /// <summary>
        /// メニューに出す、ノーツの種類の名前 (変えられない種類は空)
        /// </summary>
        public static string GetTypeName(DeploymentNoteType type)
        {
            switch (type)
            {
                case DeploymentNoteType.Touch: return "タッチノート";
                case DeploymentNoteType.DivineTouch: return "神タッチノート";
                case DeploymentNoteType.DynamicGroundUpward: return "↑ダイナミック";
                case DeploymentNoteType.DynamicGroundDownward: return "↓ダイナミック";
                case DeploymentNoteType.DynamicGroundRightward: return "→ダイナミック";
                case DeploymentNoteType.DynamicGroundLeftward: return "←ダイナミック";
                case DeploymentNoteType.HoldStart: return "ふつうのホールドの始点";
                case DeploymentNoteType.DivineHoldStart: return "神ホールドの始点";
                case DeploymentNoteType.HoldRelay: return "中継点";
                case DeploymentNoteType.HoldMeshRelay: return "メッシュ中継点";
                case DeploymentNoteType.HoldEnd: return "判定ありの終点";
                case DeploymentNoteType.HoldEndUnjudge: return "判定なしの終点";
                case DeploymentNoteType.SpaceHoldRelay: return "中継点";
                case DeploymentNoteType.SpaceHoldMeshRelay: return "メッシュ中継点";
                default: return string.Empty;
            }
        }

        private static bool Contains(IEnumerable<IDeployableNoteData> notes, IDeployableNoteData note)
        {
            foreach (var each in notes)
            {
                if (ReferenceEquals(each, note)) { return true; }
            }
            return false;
        }
    }
}
