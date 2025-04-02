using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using System;

namespace ChartEditor
{
    public class EditorUIPresenter : MonoBehaviour
    {
        [SerializeField] List<ToolButtonToEditMode> toolButtons_view;
        [SerializeField] List<NoteButtonToEditMode> noteButtons_view;
        [SerializeField] NotesViewportView notesViewport_view;
        [SerializeField] MusicBrowseButtonView musicBrowseButton_view;

        IChartEditorDataSetter dataSetter_model;
        IChartEditorDataGetter dataGetter_model;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter)
        {
            dataSetter_model = chartEditorDataSetter;
            dataGetter_model = chartEditorDataGetter;
        }

        void Start()
        {
            Bind(); 
            SetEvent();
        }

        private void Bind()
        {
            // ツールボタン
            foreach(var button in toolButtons_view)
            {
                button.Bind(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            // ノートボタン
            foreach (var button in noteButtons_view)
            {
                button.Bind(dataGetter_model.DeploymentNoteType, this.gameObject);
            }

            // ノーツビューの可視不可視
            dataGetter_model?.CurrentEditMode
                .Subscribe(notesViewport_view.OnChangeEditMode)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            foreach (var button in toolButtons_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            foreach(var button in noteButtons_view)
            {
                button.SetEvent(() => { dataSetter_model.SetNoteType(button.NoteType); });
            }
        }

        #region その他まとめクラス

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

            public void Bind(IReadOnlyReactiveProperty<EditMode> reactiveProperty, GameObject gameObject)
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

        /// <summary>
        /// ノートボタンに対するアクション他
        /// </summary>
        [Serializable]
        public class NoteButtonToEditMode
        {
            [SerializeField] ChangeDeploymentNoteButtonView noteButton_view;
            [SerializeField] DeploymentNoteType noteType;

            public ChangeDeploymentNoteButtonView NoteButton_view { get { return noteButton_view; } }

            public DeploymentNoteType NoteType { get { return noteType; } }

            public void Bind(IReadOnlyReactiveProperty<DeploymentNoteType> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(noteType => NoteButton_view.OnChangeDeploymentNote(noteType == this.noteType))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                noteButton_view.OnClickedListner += action;
            }
        }

        #endregion
    }

}
