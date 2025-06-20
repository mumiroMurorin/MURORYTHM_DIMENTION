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

        IChartEditorDataSetter editorDataSetter_model;
        IChartEditorDataGetter editorDataGetter_model;
        IChartEditorOptionSetter optionDataSetter_model;
        IChartEditorOptionGetter optionDataGetter_model;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionSetter optionDataSetter, IChartEditorOptionGetter optionDataGetter)
        {
            editorDataSetter_model = chartEditorDataSetter;
            optionDataSetter_model = optionDataSetter;
            editorDataGetter_model = chartEditorDataGetter;
            optionDataGetter_model = optionDataGetter;
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
                button.BindForDeploymentNoteType(editorDataGetter_model.CurrentEditMode, this.gameObject);
                button.BindForAutomode(editorDataGetter_model.AutoEditMode, this.gameObject);
            }

            // スペースツールボタン
            foreach (var button in toolButtonsOnSpace_view)
            {
                button.BindForDeploymentNoteType(editorDataGetter_model.CurrentEditMode, this.gameObject);
                button.BindForAutomode(editorDataGetter_model.AutoEditMode, this.gameObject);
            }

            // メッシュツールボタン
            foreach (var button in toolButtonsOnVertices_view)
            {
                button.BindForDeploymentNoteType(editorDataGetter_model.CurrentEditMode, this.gameObject);
                button.BindForAutomode(editorDataGetter_model.AutoEditMode, this.gameObject);
            }

            // ツールビューの更新
            editorDataGetter_model.EditNoteType
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
                button.SetEvent(() => { editorDataSetter_model.SetEditMode(button.EditMode); });
            }

            // スペースツールボタン
            foreach (var button in toolButtonsOnSpace_view)
            {
                button.SetEvent(() => { editorDataSetter_model.SetEditMode(button.EditMode); });
            }

            // メッシュツールボタン
            foreach (var button in toolButtonsOnVertices_view)
            {
                button.SetEvent(() => { editorDataSetter_model.SetEditMode(button.EditMode); });
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
            [SerializeField] bool IsHiddenInAutoMode;

            public ChangeEditModeButtonView ToolButton_view { get { return toolButton_view; } }

            public EditMode EditMode { get { return editMode; } }

            public void BindForDeploymentNoteType(IReadOnlyReactiveProperty<EditMode> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(editMode => ToolButton_view.OnChangeEditMode(editMode == this.editMode))
                    .AddTo(gameObject);
            }

            public void BindForAutomode(IReadOnlyReactiveProperty<bool> isAutoModeRP, GameObject gameObject)
            {
                isAutoModeRP
                    .Subscribe(isAutomode => ToolButton_view.OnChangeAutoMode(isAutomode && IsHiddenInAutoMode))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                toolButton_view.OnClickedListner += action;
            }
        }

    }

}