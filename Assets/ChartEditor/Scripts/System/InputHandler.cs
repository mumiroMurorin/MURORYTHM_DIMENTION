using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] KeyCode playKey = KeyCode.Space;

        [Header("マウス関係")]
        [Tooltip("拡大縮小の感度")]
        [SerializeField] float scalingSensitivity = 0.1f;
        [Tooltip("再生位置移動の感度基準")]
        [SerializeField] float moveSensitivityMax = 0.01f;
        [Header("ショートカットキー")]
        [SerializeField] EditModeToKeycode[] editModeShortCutKeys;

        [Tooltip("譜面エクスポート")]
        [SerializeField] ChartDataExporter chartDataExporter;

        IChartEditorDataSetter dataSetter;
        IChartEditorOptionSetter optionSetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorOptionGetter optionGetter;

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

        [Inject]
        public void Construct(IChartEditorDataSetter dataSetter, IChartEditorDataGetter dataGetter, IChartEditorOptionSetter optionSetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataSetter = dataSetter;
            this.dataGetter = dataGetter;

            this.optionSetter = optionSetter;
            this.optionGetter = optionGetter;
        }

        private void Update()
        {
            var scroll = Input.mouseScrollDelta.y;

            // 楽曲再生
            if (Input.GetKeyDown(playKey)) { OperateMusicPlay(); }
            // スケール変更
            if (Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scroll) > 0.01f) { OperateChartViewScale(scroll); }
            // 譜面スクロール
            if (!Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scroll) > 0.01f) { OperatePlaybackProgress(scroll); }
            // 保存
            if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)) SaveChart();
            // Undo
            if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z)) { Undo(); }
            // Redo
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z)) { Redo(); }
            // ショートカットキー
            CheckAndDoShortCutKey();
        }

        /// <summary>
        /// 拡大率の操作
        /// </summary>
        private void OperateChartViewScale(float delta)
        {
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(scaleIgnoreModes)) { return; }

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

            // スクロール感度と拡大率によって変える
            float ratio = moveSensitivityMax * optionGetter.ScrollSensitivity.Value * Mathf.Clamp(10f - optionGetter.ChartViewScale.Value / 0.15f, 1f, 10f);
            dataSetter?.SetPlaybackProgress(dataGetter.PlaybackProgress.Value + delta * ratio);
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
                key.CheckAndChangeEditMode(KeyCode.LeftAlt, dataGetter.EditNoteType.Value, dataSetter.SetEditMode);
            }
        }

        [System.Serializable]
        class EditModeToKeycode
        {
            [SerializeField] EditMode editMode;
            [SerializeField] EditNoteType editNoteType;
            [SerializeField] KeyCode keyCode;

            public bool CheckAndChangeEditMode(KeyCode modifierKey, EditNoteType currentEditNoteType, Action<EditMode> changeEditMode)
            {
                if (!Input.GetKey(modifierKey)) { return false; } 
                if (!Input.GetKeyDown(keyCode)) { return false; }
                if (editNoteType != currentEditNoteType) { return false; }

                changeEditMode.Invoke(editMode);
                return true;
            }
        }
    }
}