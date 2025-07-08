using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using System;

namespace ChartEditor
{
    public class ToolButtonPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] List<EditNoteTypeToToolView> toolViews;
        [Space(10),Header("Ground")]
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnGround_view;
        [SerializeField] ButtonView notesMirrorButton_view;
        [SerializeField] NotesMirror notesMirror_model;
        [Space(10), Header("Space")]
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnSpace_view;
        [Space(10), Header("Vertices")]
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnVertices_view;
        [SerializeField] VertexIndicesSliderButtonView slideClockwiseButton_view;
        [SerializeField] VertexIndicesSliderButtonView slideCounterclockwiseButton_view;
        [SerializeField] VerticesMirrorButtonView mirrorXAxisButton_view;
        [SerializeField] VerticesMirrorButtonView mirrorYAxisButton_view;
        [SerializeField] VerticesSlider verticesSlider_model;
        [SerializeField] VerticesReverser verticesReverser_model;

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
        }

        private void BindForEditView()
        {
            // グラウンドツールボタン
            foreach (var button in toolButtonsOnGround_view)
            {
                button.BindForDeploymentNoteType(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            // スペースツールボタン
            foreach (var button in toolButtonsOnSpace_view)
            {
                button.BindForDeploymentNoteType(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            // メッシュツールボタン
            foreach (var button in toolButtonsOnVertices_view)
            {
                button.BindForDeploymentNoteType(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            // ツールビューの更新
            dataGetter_model.EditNoteType
                .Subscribe(type => {
                    foreach (var view in toolViews)
                    {
                        view.CheckAndSetActiveToolView(type);
                    }
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // グラウンドツールボタン
            foreach (var button in toolButtonsOnGround_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            // ノーツを反転
            notesMirrorButton_view.OnPushButtonListner += () => { notesMirror_model?.MirrorSelectingNotes(); };

            // スペースツールボタン
            foreach (var button in toolButtonsOnSpace_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            // メッシュツールボタン
            foreach (var button in toolButtonsOnVertices_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            // 時計回りに要素番号をスライド
            slideClockwiseButton_view.OnClickedListner += () => { verticesSlider_model.SlideIndices(-1); };
            // 反時計回りに要素番号をスライド
            slideCounterclockwiseButton_view.OnClickedListner += () => { verticesSlider_model.SlideIndices(+1); };

            // X軸反転ボタン
            mirrorXAxisButton_view.OnClickedListner += () => { verticesReverser_model?.ReverseXAxis(); };
            // Y軸反転ボタン
            mirrorYAxisButton_view.OnClickedListner += () => { verticesReverser_model?.ReverseYAxis(); };
        }

        /// <summary>
        /// ツールボタンの親
        /// </summary>
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

        /// <summary>
        /// ツールボタンに対するアクション他
        /// </summary>
        [Serializable]
        public class ToolButtonToEditMode
        {
            [SerializeField] ChangeEditModeButtonView toolButton_view;
            [SerializeField] EditMode editMode;

            public ChangeEditModeButtonView ToolButton_view { get { return toolButton_view; } }

            public EditMode EditMode { get { return editMode; } }

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