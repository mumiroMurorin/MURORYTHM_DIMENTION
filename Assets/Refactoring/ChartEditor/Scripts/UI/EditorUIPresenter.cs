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
        [Serializable]
        public class ToolButtonToEditMode
        {
            [SerializeField] ChangeEditModeButton toolButton_view;
            [SerializeField] EditMode editMode;

            public ChangeEditModeButton ToolButton_view { get { return toolButton_view; } }

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

        [SerializeField] List<ToolButtonToEditMode> toolButtons_view;

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
            foreach(ToolButtonToEditMode tool in toolButtons_view)
            {
                tool.Bind(dataGetter_model.CurrentEditMode, this.gameObject);
            }
        }

        private void SetEvent()
        {
            foreach (ToolButtonToEditMode tool in toolButtons_view)
            {
                tool.SetEvent(() => { dataSetter_model.SetEditMode(tool.EditMode); });
            }
        }
    }

}
