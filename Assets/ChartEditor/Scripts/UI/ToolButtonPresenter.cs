using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class ToolButtonPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] List<EditNoteTypeToToolView> toolViews;
        [Space(10), Header("Ground")]
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnGround_view;
        [SerializeField] ButtonView notesMirrorButton_view;
        [SerializeField] NotesMirror notesMirror_model;
        [Space(10), Header("Space")]
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnSpace_view;
        [SerializeField] ButtonView spaceNotesMirrorButton_view;
        [SerializeField] SpaceNotesMirror spaceNotesMirror_model;
        [Space(10), Header("Vertices")]
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnVertices_view;
        [SerializeField] VertexIndicesSliderButtonView slideClockwiseButton_view;
        [SerializeField] VertexIndicesSliderButtonView slideCounterclockwiseButton_view;
        [SerializeField] VerticesMirrorButtonView mirrorXAxisButton_view;
        [SerializeField] VerticesMirrorButtonView mirrorYAxisButton_view;
        [SerializeField] VerticesSlider verticesSlider_model;
        [SerializeField] VerticesReverser verticesReverser_model;
        [Space(10), Header("ショートカットキーの表示")]
        [Tooltip("ツールを切り替えるキーを受け付けている処理 (ボタンに表示するキーを、ここから受け取る)")]
        [SerializeField] InputHandler inputHandler_model;

        IChartEditorDataSetter dataSetter_model;
        IChartEditorDataGetter dataGetter_model;
        IChartEditorOptionSetter optionSetter_model;
        IChartEditorOptionGetter optionGetter_model;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionSetter optionDataSetter, IChartEditorOptionGetter optionDataGetter)
        {
            dataSetter_model = chartEditorDataSetter;
            optionSetter_model = optionDataSetter;
            dataGetter_model = chartEditorDataGetter;
            optionGetter_model = optionDataGetter;
        }

        void Start()
        {
            BindForEditView();
            SetEvent();
            SetShortcutTexts();
        }

        // 反転のショートカットキー (Alt と一緒に押す。反転ボタンに「Alt+M」と表示する)
        const KeyCode MIRROR_KEY = KeyCode.M;

        /// <summary>
        /// それぞれのボタンの右下に、ショートカットキーを表示する
        /// (キーは、実際にキーを受け付けている処理から受け取る。ボタンには書いていない)
        /// </summary>
        private void SetShortcutTexts()
        {
            SetShortcutTexts(toolButtonsOnGround_view, EditNoteType.Ground);
            SetShortcutTexts(toolButtonsOnSpace_view, EditNoteType.Space);
            SetShortcutTexts(toolButtonsOnVertices_view, EditNoteType.Vertices);

            // 反転ボタン (グラウンド・スペース)
            string mirrorText = ShortcutBadge.FormatAltKey(MIRROR_KEY);
            SetShortcutText(notesMirrorButton_view, mirrorText);
            SetShortcutText(spaceNotesMirrorButton_view, mirrorText);
        }

        private void SetShortcutTexts(List<ToolButtonToEditMode> buttons, EditNoteType editNoteType)
        {
            if (buttons == null) { return; }

            foreach (var button in buttons)
            {
                if (button == null || button.ToolButton_view == null) { continue; }

                KeyCode key = KeyCode.None;
                if (inputHandler_model != null) { inputHandler_model.TryGetEditModeShortcutKey(button.EditMode, editNoteType, out key); }

                button.ToolButton_view.SetShortcutText(ShortcutBadge.FormatAltKey(key));
            }
        }

        private static void SetShortcutText(ButtonView button, string text)
        {
            if (button == null || string.IsNullOrEmpty(text)) { return; }

            // 文字の見た目(フォント)は、ボタンの名前の文字に合わせる
            TextMeshProUGUI label = ShortcutBadge.Create(button.transform, button.GetComponentInChildren<TextMeshProUGUI>(true));
            if (label != null) { label.text = text; }
        }

        // 反転を受け付けないモード (ほかの操作の途中)
        static readonly EditMode[] mirrorIgnoreEditModes =
        {
            EditMode.Connecting,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
            EditMode.Explanation,
        };

        void Update()
        {
            // Alt+M: 選択中のノーツを反転する (反転ボタンと同じ。グラウンド・スペースのレイヤーで)
            if (!ModifierKey.Alt || !Input.GetKeyDown(MIRROR_KEY)) { return; }

            // 読み込み中・文字の入力中・ドラッグの途中・ほかの操作の途中は受け付けない
            if (dataGetter_model.IsLoadingChart.Value || InputFieldFocus.IsTyping || Input.GetMouseButton(0)) { return; }
            // SpaceHold の3D編集を開いている間は、裏の譜面のノーツを反転しない
            if (EditorMenuBar.IsBlockedByOverlay) { return; }
            if (dataGetter_model.CurrentEditMode.Value.IsInEditModeList(mirrorIgnoreEditModes)) { return; }

            switch (dataGetter_model.EditNoteType.Value)
            {
                case EditNoteType.Ground:
                    notesMirror_model?.MirrorSelectingNotes();
                    break;
                case EditNoteType.Space:
                    spaceNotesMirror_model?.MirrorSelectingSpaceNotes();
                    break;
            }
        }

        private void BindForEditView()
        {
            foreach (var button in toolButtonsOnGround_view)
            {
                button.BindForDeploymentNoteType(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            foreach (var button in toolButtonsOnSpace_view)
            {
                button.BindForDeploymentNoteType(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            foreach (var button in toolButtonsOnVertices_view)
            {
                button.BindForDeploymentNoteType(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            dataGetter_model.EditNoteType
                .Subscribe(type =>
                {
                    foreach (var view in toolViews)
                    {
                        view.CheckAndSetActiveToolView(type);
                    }
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            foreach (var button in toolButtonsOnGround_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            if (notesMirrorButton_view != null)
            {
                notesMirrorButton_view.OnPushButtonListner += () => { notesMirror_model?.MirrorSelectingNotes(); };
            }

            foreach (var button in toolButtonsOnSpace_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            if (spaceNotesMirrorButton_view != null)
            {
                spaceNotesMirrorButton_view.OnPushButtonListner += () => { spaceNotesMirror_model?.MirrorSelectingSpaceNotes(); };
            }

            foreach (var button in toolButtonsOnVertices_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            if (slideClockwiseButton_view != null)
            {
                slideClockwiseButton_view.OnClickedListner += () => { verticesSlider_model?.SlideIndices(-1); };
            }

            if (slideCounterclockwiseButton_view != null)
            {
                slideCounterclockwiseButton_view.OnClickedListner += () => { verticesSlider_model?.SlideIndices(+1); };
            }

            if (mirrorXAxisButton_view != null)
            {
                mirrorXAxisButton_view.OnClickedListner += () => { verticesReverser_model?.ReverseXAxis(); };
            }

            if (mirrorYAxisButton_view != null)
            {
                mirrorYAxisButton_view.OnClickedListner += () => { verticesReverser_model?.ReverseYAxis(); };
            }
        }

        [Serializable]
        public class EditNoteTypeToToolView
        {
            [SerializeField] GameObject parent;
            [SerializeField] EditNoteType editNoteType;

            public void CheckAndSetActiveToolView(EditNoteType editNoteType)
            {
                parent.SetActive(editNoteType == this.editNoteType);
            }
        }

        [Serializable]
        public class ToolButtonToEditMode
        {
            [SerializeField] ChangeEditModeButtonView toolButton_view;
            [SerializeField] EditMode editMode;

            public ChangeEditModeButtonView ToolButton_view => toolButton_view;
            public EditMode EditMode => editMode;

            public void BindForDeploymentNoteType(IReadOnlyReactiveProperty<EditMode> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(editMode => ToolButton_view.OnChangeEditMode(editMode == this.editMode))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                toolButton_view.OnClickedListner += action;
            }
        }
    }
}