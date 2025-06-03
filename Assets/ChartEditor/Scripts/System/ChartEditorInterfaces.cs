using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChartEditor
{
    /// <summary>
    /// レーンに配置できる
    /// </summary>
    public interface ILaneDeployable<T>
    {
        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="pos"></param>
        GameObject Deploy(T lineData, Vector3 pos, Transform parent = null);

        /// <summary>
        /// 初期化
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// レーンから削除できる
    /// </summary>
    public interface ILaneDestroyable<T>
    {
        public void Destroy(T lineData);
    }

    public interface ILayerAffectable
    {
        void OnChangeLayer(EditNoteType editNoteType);
    }

    public interface IInteractableCollider
    {
        /// <summary>
        /// エディットモードの変更
        /// </summary>
        /// <returns></returns>
        EditMode EditMode { get; }
    }

    public interface IDeployableCollider
    {
        Transform deployParent { get; }

        AddressInChart Address { get; }
    }

    public interface IFreedomDeployableCollider
    {
        Transform deployParent { get; }

        AddressInChart Address { get; }
    }

    public interface IDeployableObject
    {
        public Action OnDestroyListner { get; set; }

        void OnInstantiate(IDeployableNoteData noteData, Func<AddressInChart, Transform> getParentTransformFunc);

        void OnMove(Transform parent);

        void OnDeploy();

        void OnDisable();
    }

    public interface IFreedomDeployableObject
    {
        public Action OnDestroyListner { get; set; }

        void OnInstantiate(IDeployableNoteData noteData, Func<AddressInChart, Transform> getParentTransformFunc);

        void OnMove(Transform parent, Vector3 worldPos);

        void OnDeploy();

        void OnDisable();
    }

    public interface IMovableCollider
    {
        IMovableObject Note { get; }
    }

    public interface IMovableObject
    {
        NoteObject Note { get; }

        void OnMoveStart();

        void OnMove();

        void OnMoveEnd();
    }

    public interface IScalableCollider
    {
        IScalableObject Note { get; }

        bool IsRightEdge { get; }
    }

    public interface IScalableObject
    {
        NoteObject Note { get; }

        void OnStartScale();

        void OnScale();

        void OnFinishScale();
    }

    public interface IDestroyableCollider
    {
        IDestroyableObject Note { get; }
    }

    public interface IDestroyableObject
    {
        NoteObject Note { get; }

        void OnDestroy();
    }

    public interface IConnectableCollider
    {
        IConnectableObject Note { get; }
    }

    public interface IConnectableObject
    {
        Transform MeshRightEdge { get; }

        Transform MeshLeftEdge { get; }

        NoteObject Note { get; }

    }

    public interface IChangableCollider
    {
        IChangableObject Note { get; }
    }

    public interface IChangableObject
    {
        ITypeChangableNoteData NoteData { get; }

        public void OnChangeNoteType();
    }

    public interface IJudgeStackingCollider
    {
        DeploymentNoteType NoteType { get; }

        void NotifyDisable(IJudgeStackingCollider stack);
    }
}