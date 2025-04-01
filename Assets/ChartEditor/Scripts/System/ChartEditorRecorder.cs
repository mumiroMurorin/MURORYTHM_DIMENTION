using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace ChartEditor
{
    public class ChartEditorRecorder : IChartEditorDataGetter, IChartEditorDataSetter
    {
        // エディットモード
        ReactiveProperty<EditMode> currentEditMode = new ReactiveProperty<EditMode>(EditMode.None);
        IReadOnlyReactiveProperty<EditMode> IChartEditorDataGetter.CurrentEditMode => currentEditMode;

        void IChartEditorDataSetter.SetEditMode(EditMode editMode) 
        {
            currentEditMode.Value = editMode;
            Debug.Log($"Change Edit Mode: {currentEditMode.Value}");
        }

        // 配置ノーツ
        ReactiveProperty<DeploymentNoteType> deploymentNoteType = new ReactiveProperty<DeploymentNoteType>(ChartEditor.DeploymentNoteType.TouchNote);
        IReadOnlyReactiveProperty<DeploymentNoteType> IChartEditorDataGetter.DeploymentNoteType => deploymentNoteType;
        void IChartEditorDataSetter.SetNoteType(DeploymentNoteType noteType)
        {
            deploymentNoteType.Value = noteType;
            Debug.Log($"Change Deployment Note: {deploymentNoteType.Value}");
        }

        // 配置場所の更新
        ReactiveProperty<IDeployableCollider> deployableCollider = new ReactiveProperty<IDeployableCollider>();
        IReadOnlyReactiveProperty<IDeployableCollider> IChartEditorDataGetter.DeployableCollider => deployableCollider;

        void IChartEditorDataSetter.SetDeployableCollider(IDeployableCollider collider)
        {
            if(deployableCollider.Value == collider) { return; }
            deployableCollider.Value = collider;
        }

        // 動かせるオブジェクト(ノーツ)
        ReactiveProperty<IMovableObject> movableObject= new ReactiveProperty<IMovableObject>();
        IReadOnlyReactiveProperty<IMovableObject> IChartEditorDataGetter.MovableObject => movableObject;

        void IChartEditorDataSetter.SetMovableObject(IMovableObject mObject)
        {
            if (movableObject.Value == mObject) { return; }

            movableObject.Value = mObject;
        }

        // スケーリングできるオブジェクト(ノーツ)
        ReactiveProperty<IScalableObject> scalableObject = new ReactiveProperty<IScalableObject>();
        IReadOnlyReactiveProperty<IScalableObject> IChartEditorDataGetter.ScalableObject => scalableObject;

        void IChartEditorDataSetter.SetScalableObject(IScalableObject sObject)
        {
            if(scalableObject.Value == sObject) { return; }

            scalableObject.Value = sObject;
        }

        // 消せるノーツ
        ReactiveProperty<IDestroyableObject> destroyableObject = new ReactiveProperty<IDestroyableObject>();
        IReadOnlyReactiveProperty<IDestroyableObject> IChartEditorDataGetter.DestroyableObject => destroyableObject;
        void IChartEditorDataSetter.SetDestroyableObject(IDestroyableObject dObject)
        {
            if (destroyableObject.Value == dObject) { return; }

            destroyableObject.Value = dObject;
        }
    }

    public interface IChartEditorDataGetter
    {
        IReadOnlyReactiveProperty<EditMode> CurrentEditMode { get; }

        IReadOnlyReactiveProperty<DeploymentNoteType> DeploymentNoteType { get; }

        IReadOnlyReactiveProperty<IDeployableCollider> DeployableCollider { get; }

        IReadOnlyReactiveProperty<IMovableObject> MovableObject { get; }

        IReadOnlyReactiveProperty<IScalableObject> ScalableObject { get; }

        IReadOnlyReactiveProperty<IDestroyableObject> DestroyableObject { get; }
    }

    public interface IChartEditorDataSetter
    {
        void SetEditMode(EditMode editMode);

        void SetNoteType(DeploymentNoteType noteType);

        void SetDeployableCollider(IDeployableCollider collider);

        void SetMovableObject(IMovableObject mObject);

        void SetScalableObject(IScalableObject sObject);

        void SetDestroyableObject(IDestroyableObject dObject);
    }
}
