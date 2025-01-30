using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    /// <summary>
    /// �e��m�[�c�f�[�^�̊�ƂȂ�C���^�[�t�F�[�X
    /// �r���_�[�p�^�[���̎g�p
    /// </summary>
    public interface INoteData
    {
        public NoteType NoteType { get; }

        /// <summary>
        /// �y�ȊJ�n����n�b��Ƀm�[�c�̔���
        /// </summary>
        public float Timing { get; set; }

        public ITimeGetter Timer { get; set; }

    }

    /// <summary>
    /// �e��m�[�c�̐������s��
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INoteFactory<T> where T : INoteData
    {
        void Initialize(NoteFactoryInitializingData initializingData);

        NoteObject<T> Spawn(T data);
    }

    /// <summary>
    /// �C���X�y�N�^�[�Őݒ�ł���悤�Ɋ��N���X�Ń��b�v
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NoteFactory<T> : MonoBehaviour, INoteFactory<T> where T : INoteData
    {
        public abstract void Initialize(NoteFactoryInitializingData initializingData);

        public abstract NoteObject<T> Spawn(T data);
    }
}
