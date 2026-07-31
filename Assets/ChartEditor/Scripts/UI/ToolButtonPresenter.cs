using System;
using System.Collections.Generic;
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