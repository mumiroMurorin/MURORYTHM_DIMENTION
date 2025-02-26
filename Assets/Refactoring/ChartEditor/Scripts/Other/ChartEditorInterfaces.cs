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

    public enum EditMode
    {
        none,
        modify,
        scaling,
        move
    }
}