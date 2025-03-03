using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// レーンに配置できる
    /// </summary>
    public interface ILaneDeployable
    {
        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="pos"></param>
        GameObject Deploy(Vector3 pos);

        /// <summary>
        /// 拡大縮小
        /// </summary>
        /// <param name="scale"></param>
        void Scaling(float scale);

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

    }

    public interface IDeployableObject
    {
        void OnInstantiate();

        void OnDeploy();
    }

    public interface IMovableObject
    {
        GameObject gameObject { get; }

        void OnMoveStart();

        void OnMove();

        void OnMoveEnd();
    }

    public interface IScalableObject
    {
        GameObject gameObject { get; }

        void OnScale();
    }

    public enum EditMode
    {
        none,
        deploy,
        move,
        scale,
    }
}