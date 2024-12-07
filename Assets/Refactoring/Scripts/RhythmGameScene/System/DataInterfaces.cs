using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// グラウンドノーツの生成に必要なデータの存在を保証する
    /// </summary>
    public interface IGroundNoteGenerationData
    {
        int NoteLaneWidth { get; }
        Vector3 NoteEulerAngles { get; }
    }

    /// <summary>
    /// グラウンドノートの生成を行う
    /// </summary>
    public interface IGroundNoteGenerator
    {
        GroundTouchNoteObject GenerateNote(IGroundNoteGenerationData generationData);
    }

    /// <summary>
    /// ノーツのスライダー判定に必要なデータの存在を保証する
    /// </summary>
    public interface INoteSliderJudgementData
    {
        public float JudgeTime { get; }

        public int StartIndex { get; }
        public int EndIndex { get; }
    }

    /// <summary>
    /// スペースノーツの生成に必要なデータの存在を保証する
    /// </summary>
    public interface ISpaceNoteGenerationData
    {
        float NoteLength { get; }
        Vector3 NoteEulerAngles { get; }
    }

    /// <summary>
    /// スペースノートの生成を行う
    /// </summary>
    public interface ISpaceNoteGenerator
    {
        NoteObject GenerateNote(ISpaceNoteGenerationData generationData);
    }

    /// <summary>
    /// ノーツの空間判定に必要なデータの存在を保証する
    /// </summary>
    public interface INoteSpaceJudgementData
    {
        public float JudgeTime { get; }

        public Vector2 JudgePosition { get; }
    }

    /// <summary>
    /// ノーツのスポーンに必要なデータが存在することを保証
    /// </summary>
    public interface INoteSpawnData
    {
        GameObject NoteGameObject { get; }

        Vector3 SpawnPosition { get; }
    }

    /// <summary>
    /// 生成されたノーツをスポーンさせる基底クラス (Factoryパターン)
    /// </summary>
    public interface INotePositionSetter
    {
        public void SetPosition(INoteSpawnData spawnData);
    }

    // ================================================
    //                   ノーツデータ
    // ================================================

    /// <summary>
    /// グラウンドノートのデータ
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
    /// グラウンドダイナミック(右)ノーツのデータ
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
    /// グラウンドダイナミック(左)ノーツのデータ
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
    /// グラウンドダイナミック(上)ノーツのデータ
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
    /// グラウンドダイナミック(下)ノーツのデータ
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