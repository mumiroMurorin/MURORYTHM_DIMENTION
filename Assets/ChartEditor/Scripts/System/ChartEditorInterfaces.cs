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
    /// 選択可能なオブジェクト(コライダー)
    /// </summary>
    public interface ISelectableVertexCollider : IInteractableCollider
    {
        ISelectableVertexObject SelectableObject { get; }
    }

    /// <summary>
    /// 選択可能なオブジェクト
    /// </summary>
    public interface ISelectableVertexObject
    {
        VertexObject VertexObject { get; }

        void OnSelect();

        void OnDeselect();
    }

    /// <summary>
    /// 選択可能なノート(コライダー)
    /// </summary>
    public interface ISelectableNoteCollider : IInteractableCollider
    {
        ISelectableNoteObject SelectableObject { get; }
    }

    /// <summary>
    /// 選択可能なノート
    /// </summary>
    public interface ISelectableNoteObject
    {
        NoteObject NoteObject { get; }

        void OnSelect();

        void OnDeselect();
    }

    /// <summary>
    /// 配置可能オブジェクト(コライダー)
    /// </summary>
    public interface IDeployableCollider: IInteractableCollider
    {
        Transform deployParent { get; }

        AddressInChart Address { get; }
    }

    /// <summary>
    /// 点配置可能オブジェクト(コライダー)
    /// </summary>
    public interface IPointDeployableCollider : IInteractableCollider
    {

    }

    /// <summary>
    /// 自由配置可能オブジェクト(コライダー)
    /// </summary>
    public interface IFreedomDeployableCollider: IInteractableCollider
    {
        Transform deployParent { get; }

        AddressInChart Address { get; }
    }

    /// <summary>
    /// 配置可能オブジェクト
    /// </summary>
    public interface IDeployableObject
    {
        public NoteObject Note { get; }

        public Action OnDestroyListner { get; set; }

        void OnInstantiate(IDeployableNoteData noteData, Func<AddressWithinRange, Transform> getParentTransformFunc);

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

        void OnInstantiate(IDeployableNoteData noteData, Func<AddressWithinRange, Transform> getParentTransformFunc);

        void OnMove(Transform parent);

        void OnDeploy();

        void OnDisable();
    }

    /// <summary>
    /// 移動可能オブジェクト(コライダー)
    /// </summary>
    public interface IMovableCollider: IInteractableCollider
    {
        IMovableObject Note { get; }
    }

    /// <summary>
    /// 点移動可能オブジェクト(コライダー)
    /// </summary>
    public interface IPointMovableCollider : IInteractableCollider
    {
        IPointMovableObject Vertex { get; }
    }

    /// <summary>
    /// 自由移動可能オブジェクト(コライダー)
    /// </summary>
    public interface IFreedomMovableCollider : IInteractableCollider
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
    /// 自由移動可能オブジェクト
    /// </summary>
    public interface IPointMovableObject
    {
        VertexObject Vertex { get; }

        void OnMoveStart();

        void OnMove();

        void OnMoveEnd();
    }

    /// <summary>
    /// スケーリング可能オブジェクト(コライダー)
    /// </summary>
    public interface IScalableCollider : IInteractableCollider
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
    /// 削除可能オブジェクト
    /// </summary>
    public interface IDestroyableObject
    {
        NoteObject Note { get; }

        void OnDestroy();
    }

    /// <summary>
    /// 削除可能オブジェクト
    /// </summary>
    public interface IDestroyableVertex
    {
        VertexObject Vertex { get; }

        void OnDestroy();
    }

    /// <summary>
    /// 接続可能オブジェクト(コライダー)
    /// </summary>
    public interface IConnectableCollider : IInteractableCollider
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
    public interface IChangableCollider : IInteractableCollider
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
    public interface ISpaceEditableCollider : IInteractableCollider
    {
        ISpaceEditableObject Note { get; }
    }

    /// <summary>
    /// 編集可能オブジェクト
    /// </summary>
    public interface ISpaceEditableObject
    {
        IVerticesControlableNoteData NoteData { get; }
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