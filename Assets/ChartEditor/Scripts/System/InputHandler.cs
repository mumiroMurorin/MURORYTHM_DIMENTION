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
            EditMode.Connecting,
            EditMode.SpaceMoving,
            EditMode.VertexMoving,
            EditMode.VerticesRotating,
            EditMode.VerticesScaling,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
            EditMode.Preview,
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
            bool isTyping = IsTypingInInputField();

            // 楽曲再生
            if (!isTyping && Input.GetKeyDown(playKey)) { OperateMusicPlay(); }
            // スケール変更
            if (ctrl && Mathf.Abs(scroll) > 0.01f) { OperateChartViewScale(scroll); }
            // 譜面スクロール
            if (!ctrl && Mathf.Abs(scroll) > 0.01f) { OperatePlaybackProgress(scroll); }
            // 保存
            if (ctrl && Input.GetKeyDown(KeyCode.S)) { SaveChart(); }
            // Undo
            if (ctrl && !shift && Input.GetKeyDown(KeyCode.Z) && CanUndoRedo(isTyping)) { Undo(); }
            // Redo (Ctrl+Shift+Z または Ctrl+Y)
            if (ctrl && ((shift && Input.GetKeyDown(KeyCode.Z)) || Input.GetKeyDown(KeyCode.Y)) && CanUndoRedo(isTyping)) { Redo(); }
            // Move editing vertices along the connected SpaceHold chain
            if (!isTyping && Input.GetKeyDown(KeyCode.UpArrow)) { verticesChainNavigator?.MoveToNext(); }
            if (!isTyping && Input.GetKeyDown(KeyCode.DownArrow)) { verticesChainNavigator?.MoveToPrevious(); }
            // ショートカットキー
            if (!isTyping) { CheckAndDoShortCutKey(); }
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
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(undoRedoIgnoreModes)) { return false; }

            return true;
        }

        /// <summary>
        /// 入力欄(InputField)にフォーカスがあるか
        /// </summary>
        private bool IsTypingInInputField()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) { return false; }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected == null) { return false; }

            if (selected.TryGetComponent(out TMPro.TMP_InputField tmpField)) { return tmpField.isFocused; }
            if (selected.TryGetComponent(out UnityEngine.UI.InputField field)) { return field.isFocused; }

            return false;
        }

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
            if (EventSystem.current.IsPointerOverGameObject() && dataGetter.CurrentEditMode.Value != EditMode.Preview) { return; }

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

        [System.Serializable]
        class EditModeToKeycode
        {
            [SerializeField] string modeName;
            [SerializeField] EditMode editMode;
            [SerializeField] EditNoteType editNoteType;
            [SerializeField] KeyCode keyCode;

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