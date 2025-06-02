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
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnGround_view;
        [SerializeField] List<ToolButtonToEditMode> toolButtonsOnSpace_view;

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