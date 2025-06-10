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

    /// <summary>
    /// レイヤーによって影響を受ける
    /// </summary>
    public interface ILayerAffectable
    {
        void OnChangeLayer(EditNoteType editNoteType);
    }

    /// <summary>
    /// インタラクト可能なオブジェクト(コライダー)
    /// </summary>
    public interface IInteractableCollider
    {
        /// <summary>
        /// エディットモードの変更
        /// </summary>
        /// <returns></returns>
        EditMode EditMode { get; }
    }

    /// <summary>
    /// 配置可能オブジェクト(コライダー)
    /// </summary>
    public interface IDeployableCollider
    {
        Transform deployParent { get; }

        AddressInChart Address { get; }
    }

    /// <summary>
    /// 自由配置可能オブジェクト(コライダー)
    /// </summary>
    public interface IFreedomDeployableCollider
    {
        Transform deployParent { get; }

        AddressInChart Address { get; }
    }

    /// <summary>
    /// 配置可能オブジェクト
    /// </summary>
    public interface IDeployableObject
    {
        public Action OnDestroyListner { get; set; }

        void OnInstantiate(IDeployableNoteData noteData, Func<AddressInChart, Transform> getParentTransformFunc);

        void OnMove(Transform parent);

        void OnDeploy();

        void OnDisable();
    }

    /// <summary>
    /// 自由配置可能オブジェクト
    /// </summary>
    public interface IFreedomDeployableObject
    {
        public Action OnDestroyListner { get; set; }

        void OnInstantiate(IDeployableNoteData noteData, Func<AddressInChart, Transform> getParentTransformFunc);

        void OnMove(Transform parent, Vector3 worldPos);

        void OnDeploy();

        void OnDisable();
    }

    /// <summary>
    /// 移動可能オブジェクト(コライダー)
    /// </summary>
    public interface IMovableCollider
    {
        IMovableObject Note { get; }
    }

    /// <summary>
    /// 自由移動可能オブジェクト(コライダー)
    /// </summary>
    public interface IFreedomMovableCollider
    {
        IFreedomMovableObject Note { get; }
    }

    /// <summary>
    /// 移動可能オブジェクト
    /// </summary>
    public interface IMovableObject
    {
        NoteObject Note { get; }

        void OnMoveStart();

        void OnMove();

        void OnMoveEnd();
    }

    /// <summary>
    /// 自由移動可能オブジェクト
    /// </summary>
    public interface IFreedomMovableObject
    {
        NoteObject Note { get; }

        void OnMoveStart();

        void OnMove();

        void OnMoveEnd();
    }

    /// <summary>
    /// スケーリング可能オブジェクト(コライダー)
    /// </summary>
    public interface IScalableCollider
    {
        IScalableObject Note { get; }

        bool IsRightEdge { get; }
    }

    /// <summary>
    /// スケーリング可能オブジェクト
    /// </summary>
    public interface IScalableObject
    {
        NoteObject Note { get; }

        void OnStartScale();

        void OnScale();

        void OnFinishScale();
    }

    /// <summary>
    /// 削除可能オブジェクト(コライダー)
    /// </summary>
    public interface IDestroyableCollider
    {
        IDestroyableObject Note { get; }
    }

    /// <summary>
    /// 削除可能オブジェクト
    /// </summary>
    public interface IDestroyableObject
    {
        NoteObject Note { get; }

        void OnDestroy();
    }

    /// <summary>
    /// 接続可能オブジェクト(コライダー)
    /// </summary>
    public interface IConnectableCollider
    {
        IConnectableObject Note { get; }
    }

    /// <summary>
    /// 接続可能オブジェクト
    /// </summary>
    public interface IConnectableObject
    {
        Transform MeshRightEdge { get; }

        Transform MeshLeftEdge { get; }

        NoteObject Note { get; }

    }

    /// <summary>
    /// タイプ変更可能オブジェクト(コライダー)
    /// </summary>
    public interface IChangableCollider
    {
        IChangableObject Note { get; }
    }

    /// <summary>
    /// タイプ変更可能オブジェクト
    /// </summary>
    public interface IChangableObject
    {
        ITypeChangableNoteData NoteData { get; }

        public void OnChangeNoteType();
    }

    /// <summary>
    /// 編集可能オブジェクト(コライダー)
    /// </summary>
    public interface ISpaceEditableCollider
    {
        ISpaceEditableObject Note { get; }
    }

    /// <summary>
    /// 編集可能オブジェクト
    /// </summary>
    public interface ISpaceEditableObject
    {

    }

    /// <summary>
    /// 被り判定コライダー
    /// </summary>
    public interface IJudgeStackingCollider
    {
        DeploymentNoteType NoteType { get; }

        void NotifyDisable(IJudgeStackingCollider stack);
    }
}