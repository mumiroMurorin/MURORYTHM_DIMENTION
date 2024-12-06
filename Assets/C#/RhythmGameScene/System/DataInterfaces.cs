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
        NoteObject GenerateNote(IGroundNoteGenerationData generationData);
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
    public interface INoteSpawner
    {
        public NoteObject Spawn(INoteSpawnData spawnData);
    }
}