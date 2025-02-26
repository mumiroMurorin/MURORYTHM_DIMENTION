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

    public enum EditMode
    {
        none,
        modify,
        scaling,
        move
    }
}