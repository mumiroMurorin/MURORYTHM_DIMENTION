using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        /// 拡大縮小
        /// </summary>
        /// <param name="scale"></param>
        void Scaling(float current, float previous);

        /// <summary>
        /// 初期化
        /// </summary>
        void Initialize();
    }

    public interface IInteractableCollider
    {
        /// <summary>
        /// エディットモードの変更
        /// </summary>
        /// <returns></returns>
        EditMode GetEditMode();
    }

    public interface IDeployableCollider
    {
        Transform transform { get; }
    }

    public interface IDeployableObject
    {
        void OnInstantiate();

        void OnMove(Transform parent);

        void OnDeploy();

        void OnDisable();
    }

    public interface IMovableCollider
    {
        IMovableObject Note { get; }
    }

    public interface IMovableObject
    {
        GameObject gameObject { get; }

        void OnMoveStart();

        void OnMove(Transform parent);

        void OnMoveEnd();
    }

    public interface IScalableCollider
    {
        IScalableObject Note { get; }
    }

    public interface IScalableObject
    {
        GameObject gameObject { get; }

        void OnScale();
    }

    public interface IDestroyableCollider
    {
        IDestroyableObject Note { get; }
    }

    public interface IDestroyableObject
    {
        void OnDestroy();
    }
}