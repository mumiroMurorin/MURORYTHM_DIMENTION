using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class ChartEditorRecorder : IChartEditorDataGetter, IChartEditorDataSetter
    {
        ReactiveProperty<EditMode> currentEditMode = new ReactiveProperty<EditMode>(EditMode.none);
        IReadOnlyReactiveProperty<EditMode> IChartEditorDataGetter.CurrentEditMode => currentEditMode;

        void IChartEditorDataSetter.SetEditMode(EditMode editMode) 
        {
            currentEditMode.Value = editMode;
            Debug.Log($"Change Edit Mode: {currentEditMode.Value}");
        }
    }

    public interface IChartEditorDataGetter
    {
        IReadOnlyReactiveProperty<EditMode> CurrentEditMode { get; }
    }

    public interface IChartEditorDataSetter
    {
        void SetEditMode(EditMode editMode);
    }
}
