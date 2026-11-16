using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VContainer;
using UnityEngine.EventSystems;
using static UndoRedo.History;

namespace ChartEditor
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] KeyCode playKey = KeyCode.Space;

        [Header("マウス関係")]
        [Tooltip("拡大縮小の感度")]
        [SerializeField] float scalingSensitivity = 0.1f;
        [Tooltip("再生位置移動の感度基準(z/scroll)")]
        [SerializeField] float moveSensitivityMax = 0.01f;
        [Header("ショートカットキー")]
        [SerializeField] EditModeToKeycode[] editModeShortCutKeys;

        [Tooltip("譜面エクスポート")]
        [SerializeField] ChartDataExporter chartDataExporter;

        IChartEditorDataSetter dataSetter;
        IChartEditorOptionSetter optionSetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorOptionGetter optionGetter;
        VerticesChainNavigator verticesChainNavigator;
        // ↑ / ↓ の長押しで、前後の節へ続けて移る
        readonly KeyRepeat chainUpKeyRepeat = new KeyRepeat(KeyCode.UpArrow);
        readonly KeyRepeat chainDownKeyRepeat = new KeyRepeat(KeyCode.DownArrow);

        EditMode[] scaleIgnoreModes = new EditMode[]
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        EditMode[] playIgnoreModes = new EditMode[]
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        // 操作の途中で Undo / Redo すると不整合になるモード
        EditMode[] undoRedoIgnoreModes = new EditMode[]
        {
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.CurveHandling,
            EditMode.Connecting,
            EditMode.SpaceMoving,
            EditMode.VertexMoving,
            EditMode.VerticesRotating,
            EditMode.VerticesScaling,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
            // 全画面プレビュー(EditMode.Preview)は含めない
            // プレビューの上でもノーツを動かせるので、その場で元に戻せるようにする
        };

        [Inject]
        public void Construct(
            IChartEditorDataSetter dataSetter,
            IChartEditorDataGetter dataGetter,
            IChartEditorOptionSetter optionSetter,
            IChartEditorOptionGetter optionGetter,
            VerticesChainNavigator verticesChainNavigator)
        {
            this.dataSetter = dataSetter;
            this.dataGetter = dataGetter;

            this.optionSetter = optionSetter;
            this.optionGetter = optionGetter;
            this.verticesChainNavigator = verticesChainNavigator;
        }

        private void Update()
        {
            // 譜面の読み込み中は、キー操作(再生・保存・Undo・ショートカットなど)を受け付けない
            if (dataGetter.IsLoadingChart.Value) { return; }

            var scroll = Input.mouseScrollDelta.y;

            // 修飾キーは左右どちらでも反応する
            bool ctrl = ModifierKey.Ctrl;
            bool shift = ModifierKey.Shift;
            // 入力欄に文字を打っている間は、キー操作を譜面の操作として扱わない
            bool isTyping = InputFieldFocus.IsTyping;

            // 楽曲再生
            if (!isTyping && Input.GetKeyDown(playKey)) { OperateMusicPlay(); }
            // メニューの一覧を開いている間は、ホイールで裏の譜面を動かさない
            bool isWheelAccepted = !EditorMenuBar.IsBlockingChartInput;
            // スケール変更
            if (isWheelAccepted && ctrl && Mathf.Abs(scroll) > 0.01f) { OperateChartViewScale(scroll); }
            // 譜面スクロール
            if (isWheelAccepted && !ctrl && Mathf.Abs(scroll) > 0.01f) { OperatePlaybackProgress(scroll); }
            // 保存
            if (ctrl && Input.GetKeyDown(KeyCode.S)) { SaveChart(); }
            // Undo
            if (ctrl && !shift && Input.GetKeyDown(KeyCode.Z) && CanUndoRedo(isTyping)) { Undo(); }
            // Redo (Ctrl+Shift+Z または Ctrl+Y)
            if (ctrl && ((shift && Input.GetKeyDown(KeyCode.Z)) || Input.GetKeyDown(KeyCode.Y)) && CanUndoRedo(isTyping)) { Redo(); }
            // Move editing vertices along the connected SpaceHold chain
            // (SpaceHold の3D編集を開いている間は、3D編集の側で前後の節へ移る)
            // (長押しで、くり返す: 録音 20261117_024144)
            bool isChainKeyAccepted = !isTyping && !EditorMenuBar.IsBlockedByOverlay;
            bool isUpKey = chainUpKeyRepeat.Poll();
            bool isDownKey = chainDownKeyRepeat.Poll();
            if (isChainKeyAccepted && isUpKey) { verticesChainNavigator?.MoveToNext(); }
            if (isChainKeyAccepted && isDownKey) { verticesChainNavigator?.MoveToPrevious(); }
            // ショートカットキー (SpaceHold の3D編集を開いている間は、ツールを切り替えない)
            if (!isTyping && !EditorMenuBar.IsBlockedByOverlay) { CheckAndDoShortCutKey(); }
        }

        /// <summary>
        /// Undo / Redo を受け付けてよい状態か
        /// ドラッグ操作の途中などで実行すると、操作中のノーツ・頂点が消えて不整合になるため
        /// </summary>
        private bool CanUndoRedo(bool isTyping)
        {
            if (isTyping) { return false; }
            // マウスボタンを押している間(ドラッグ中)は不可
            if (Input.GetMouseButton(0)) { return false; }
            // 右クリックのメニューを出している間も不可 (メニューの項目が、元に戻して消えたノーツに働かないように)
            if (EditorContextMenuView.IsOpen) { return false; }

            return IsUndoRedoAllowedMode;
        }

        /// <summary>
        /// 今のエディットモードで、Undo / Redo をしてよいか (ほかの操作の途中でないか)
        /// </summary>
        private bool IsUndoRedoAllowedMode => !dataGetter.CurrentEditMode.Value.IsInEditModeList(undoRedoIgnoreModes);

        #region メニューから呼ぶ操作 (キー操作と同じことをする)

        /// <summary>
        /// 今、元に戻せるか (元に戻せる操作が残っていて、ほかの操作の途中でない)
        /// </summary>
        public bool CanUndoNow => !dataGetter.IsLoadingChart.Value && IsUndoRedoAllowedMode && UndoRedo.History.CanUndo;

        /// <summary>
        /// 今、やり直せるか
        /// </summary>
        public bool CanRedoNow => !dataGetter.IsLoadingChart.Value && IsUndoRedoAllowedMode && UndoRedo.History.CanRedo;

        /// <summary>
        /// 元に戻す (Ctrl+Z と同じ)
        /// </summary>
        public void UndoOnce()
        {
            if (CanUndoNow) { Undo(); }
        }

        /// <summary>
        /// やり直す (Ctrl+Y と同じ)
        /// </summary>
        public void RedoOnce()
        {
            if (CanRedoNow) { Redo(); }
        }

        /// <summary>
        /// 楽曲を再生する / 止める (Space キーと同じ)
        /// </summary>
        public void TogglePlay()
        {
            if (dataGetter.IsLoadingChart.Value) { return; }

            OperateMusicPlay();
        }

        #endregion

        /// <summary>
        /// 拡大率の操作
        /// </summary>
        private void OperateChartViewScale(float delta)
        {
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(scaleIgnoreModes)) { return; }
            // カーソルがUI上にあるときは返す
            //if (EventSystem.current.IsPointerOverGameObject()) { return; }

            optionSetter?.SetChartViewScale(optionGetter.ChartViewScale.Value + delta * scalingSensitivity);
        }

        /// <summary>
        /// 再生位置の操作
        /// </summary>
        private void OperatePlaybackProgress(float delta)
        {
            // 再生中は操作を受け付けない
            if (dataGetter.PlayMode.Value == PlayMode.Play) { return; }
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(playIgnoreModes)) { return; }
            // カーソルがUI上にあるときは返す
            // (プレビューの映像の上は別。ミニプレビューにカーソルを置いているときも、譜面の上と同じように動かせる。
            //  全画面プレビューでも、クイックメニューやメニューの上では動かさない。そこでのホイールは、その欄を送る操作)
            if (EventSystem.current.IsPointerOverGameObject() &&
                !PreviewNotePicker.IsPointerOnPreview(Input.mousePosition))
            {
                return;
            }

            // スクロール感度と譜面長さによって変える
            float chartSeconds = dataGetter.ChartSeconds.Value;
            float groundLength = chartSeconds * optionGetter.ChartViewScale.Value;
            float movementPerScroll = moveSensitivityMax * optionGetter.ScrollSensitivity.Value;
            float ratio = (movementPerScroll * delta) / groundLength;

            dataSetter?.SetPlaybackProgress(dataGetter.PlaybackProgress.Value + ratio);
        }

        /// <summary>
        /// 楽曲再生/停止の操作
        /// </summary>
        private void OperateMusicPlay()
        {
            switch (dataGetter.PlayMode.Value)
            {
                case PlayMode.Play:
                    dataSetter?.SetPlayMode(PlayMode.Stop);
                    break;
                case PlayMode.Stop:
                    dataSetter?.SetPlayMode(PlayMode.Play);
                    break;
            }

        }

        private void SaveChart()
        {
            chartDataExporter.Export();
        }

        private void CheckAndDoShortCutKey()
        {
            if(editModeShortCutKeys == null) { return; }

            foreach(var key in editModeShortCutKeys)
            {
                key.CheckAndChangeEditMode(ModifierKey.Alt, dataGetter.EditNoteType.Value, dataSetter.SetEditMode);
            }
        }

        /// <summary>
        /// ツール (エディットモード) を切り替えるショートカットキーを調べる (Alt と一緒に押すキー)
        /// ツールのボタンに、キーを表示するのに使う
        /// </summary>
        /// <param name="editMode">ツールのエディットモード</param>
        /// <param name="editNoteType">そのツールを使うレイヤー</param>
        /// <returns>キーが決まっているか</returns>
        public bool TryGetEditModeShortcutKey(EditMode editMode, EditNoteType editNoteType, out KeyCode keyCode)
        {
            keyCode = KeyCode.None;
            if (editModeShortCutKeys == null) { return false; }

            foreach (var key in editModeShortCutKeys)
            {
                if (key == null || !key.IsFor(editMode, editNoteType)) { continue; }

                keyCode = key.KeyCode;
                return keyCode != KeyCode.None;
            }
            return false;
        }

        [System.Serializable]
        class EditModeToKeycode
        {
            [SerializeField] string modeName;
            [SerializeField] EditMode editMode;
            [SerializeField] EditNoteType editNoteType;
            [SerializeField] KeyCode keyCode;

            public KeyCode KeyCode => keyCode;

            public bool IsFor(EditMode editMode, EditNoteType editNoteType)
            {
                return this.editMode == editMode && this.editNoteType == editNoteType;
            }

            public bool CheckAndChangeEditMode(bool isModifierKeyPressed, EditNoteType currentEditNoteType, Action<EditMode> changeEditMode)
            {
                if (!isModifierKeyPressed) { return false; }
                if (!Input.GetKeyDown(keyCode)) { return false; }
                if (editNoteType != currentEditNoteType) { return false; }

                changeEditMode.Invoke(editMode);
                return true;
            }
        }
    }
}