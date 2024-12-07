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
        GroundTouchNoteObject GenerateNote(IGroundNoteGenerationData generationData);
    }

    /// <summary>
    /// �m�[�c�̃X���C�_�[����ɕK�v�ȃf�[�^�̑��݂�ۏ؂���
    /// </summary>
    public interface INoteSliderJudgementData
    {
        public float JudgeTime { get; }

        public int StartIndex { get; }
        public int EndIndex { get; }
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
    /// �m�[�c�̋�Ԕ���ɕK�v�ȃf�[�^�̑��݂�ۏ؂���
    /// </summary>
    public interface INoteSpaceJudgementData
    {
        public float JudgeTime { get; }

        public Vector2 JudgePosition { get; }
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
    public interface INotePositionSetter
    {
        public void SetPosition(INoteSpawnData spawnData);
    }

    // ================================================
    //                   �m�[�c�f�[�^
    // ================================================

    /// <summary>
    /// �O���E���h�m�[�g�̃f�[�^
    /// </summary>
    public class GroundTouchNoteData : IGroundNoteGenerationData, INoteSpawnData, INoteSliderJudgementData
    {
        public float JudgeTime { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public int NoteLaneWidth => EndIndex - StartIndex + 1;

        public Vector3 NoteEulerAngles => new Vector3(0, 0, (StartIndex + (EndIndex - StartIndex) / 2f - 7.5f) * 11.25f);

        public GameObject NoteGameObject { get; set; }

        public Vector3 SpawnPosition { get; set; }
    }

    /// <summary>
    /// �O���E���h�_�C�i�~�b�N(�E)�m�[�c�̃f�[�^
    /// </summary>
    public class GroundDynamicNoteRightData : IGroundNoteGenerationData, INoteSpawnData, INoteSpaceJudgementData
    {
        public float JudgeTime { get; set; }

        public int NoteLaneWidth { get; set; }

        public Vector3 NoteEulerAngles { get; set; }

        public GameObject NoteGameObject { get; set; }

        public Vector3 SpawnPosition { get; set; }

        public Vector2 JudgePosition { get; set; }
    }

    /// <summary>
    /// �O���E���h�_�C�i�~�b�N(��)�m�[�c�̃f�[�^
    /// </summary>
    public class GroundDynamicNoteLeftData : IGroundNoteGenerationData, INoteSpawnData, INoteSpaceJudgementData
    {
        public float JudgeTime { get; set; }

        public int NoteLaneWidth { get; set; }

        public Vector3 NoteEulerAngles { get; set; }

        public GameObject NoteGameObject { get; set; }

        public Vector3 SpawnPosition { get; set; }

        public Vector2 JudgePosition { get; set; }
    }

    /// <summary>
    /// �O���E���h�_�C�i�~�b�N(��)�m�[�c�̃f�[�^
    /// </summary>
    public class GroundDynamicNoteUpData : IGroundNoteGenerationData, INoteSpawnData, INoteSpaceJudgementData
    {
        public float JudgeTime { get; set; }

        public int NoteLaneWidth { get; set; }

        public Vector3 NoteEulerAngles { get; set; }

        public GameObject NoteGameObject { get; set; }

        public Vector3 SpawnPosition { get; set; }

        public Vector2 JudgePosition { get; set; }
    }

    /// <summary>
    /// �O���E���h�_�C�i�~�b�N(��)�m�[�c�̃f�[�^
    /// </summary>
    public class GroundDynamicNoteDownData : IGroundNoteGenerationData, INoteSpawnData, INoteSpaceJudgementData
    {
        public float JudgeTime { get; set; }

        public int NoteLaneWidth { get; set; }

        public Vector3 NoteEulerAngles { get; set; }

        public GameObject NoteGameObject { get; set; }

        public Vector3 SpawnPosition { get; set; }

        public Vector2 JudgePosition { get; set; }
    }
}