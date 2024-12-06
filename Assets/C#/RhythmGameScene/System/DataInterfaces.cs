using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// �O���E���h�m�[�c�̐����ɕK�v�ȃf�[�^�̑��݂�ۏ؂���
    /// </summary>
    public interface IGroundNoteGenerationData
    {
        int NoteLaneWidth { get; }
        Vector3 NoteEulerAngles { get; }
    }

    /// <summary>
    /// �O���E���h�m�[�g�̐������s��
    /// </summary>
    public interface IGroundNoteGenerator
    {
        NoteObject GenerateNote(IGroundNoteGenerationData generationData);
    }

    /// <summary>
    /// �X�y�[�X�m�[�c�̐����ɕK�v�ȃf�[�^�̑��݂�ۏ؂���
    /// </summary>
    public interface ISpaceNoteGenerationData
    {
        float NoteLength { get; }
        Vector3 NoteEulerAngles { get; }
    }

    /// <summary>
    /// �X�y�[�X�m�[�g�̐������s��
    /// </summary>
    public interface ISpaceNoteGenerator
    {
        NoteObject GenerateNote(ISpaceNoteGenerationData generationData);
    }

    /// <summary>
    /// �m�[�c�̃X�|�[���ɕK�v�ȃf�[�^�����݂��邱�Ƃ�ۏ�
    /// </summary>
    public interface INoteSpawnData
    {
        GameObject NoteGameObject { get; }

        Vector3 SpawnPosition { get; }
    }

    /// <summary>
    /// �������ꂽ�m�[�c���X�|�[����������N���X (Factory�p�^�[��)
    /// </summary>
    public interface INoteSpawner
    {
        public NoteObject Spawn(INoteSpawnData spawnData);
    }
}