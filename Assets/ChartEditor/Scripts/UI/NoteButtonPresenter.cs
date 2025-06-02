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
    public class NoteButtonPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] NotesViewportView notesViewport_view;
        [SerializeField] List<NoteButtonToEditMode> noteButtons_view;

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
            BindForNoteView();

            SetEvent();
        }

        private void BindForNoteView()
        {
            // 配置ノーツボタン
            foreach (var button in noteButtons_view)
            {
                button.BindForDeploymentNoteType(editorDataGetter_model.DeploymentNoteType, this.gameObject);
                button.BindForEditNoteType(editorDataGetter_model.EditNoteType, this.gameObject);
            }

            // ノーツビューの可視不可視
            editorDataGetter_model?.CurrentEditMode
                .Subscribe(notesViewport_view.OnChangeEditMode)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // 配置ノーツボタン
            foreach (var button in noteButtons_view)
            {
                button.SetEvent(() => { editorDataSetter_model.SetNoteType(button.NoteType); });
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
            [SerializeField] EditNoteType editNoteType;

            public ChangeDeploymentNoteButtonView NoteButton_view { get { return noteButton_view; } }

            public DeploymentNoteType NoteType { get { return noteType; } }

            public void BindForDeploymentNoteType(IReadOnlyReactiveProperty<DeploymentNoteType> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(noteType => NoteButton_view.OnChangeDeploymentNote(noteType == this.noteType))
                    .AddTo(gameObject);
            }

            public void BindForEditNoteType(IReadOnlyReactiveProperty<EditNoteType> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(editNoteType => NoteButton_view.OnChangeEditNoteType(editNoteType == this.editNoteType))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                noteButton_view.OnClickedListner += action;
            }
        }   
    }
}