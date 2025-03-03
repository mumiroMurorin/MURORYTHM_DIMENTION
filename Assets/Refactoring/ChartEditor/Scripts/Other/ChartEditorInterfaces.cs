using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// ���[���ɔz�u�ł���
    /// </summary>
    public interface ILaneDeployable
    {
        /// <summary>
        /// �z�u
        /// </summary>
        /// <param name="pos"></param>
        GameObject Deploy(Vector3 pos);

        /// <summary>
        /// �g��k��
        /// </summary>
        /// <param name="scale"></param>
        void Scaling(float scale);

        /// <summary>
        /// ������
        /// </summary>
        void Initialize();
    }

    public interface IInteractableCollider
    {
        /// <summary>
        /// �G�f�B�b�g���[�h�̕ύX
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