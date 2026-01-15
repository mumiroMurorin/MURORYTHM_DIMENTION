using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Deform;

/// <summary>
/// 各種ノーツデータの基となるインターフェース
/// </summary>
public interface INoteData
{
    public NoteType NoteType { get; }

    /// <summary>
    /// 楽曲開始からn秒後にノーツの判定
    /// </summary>
    public float Timing { get; set; }

    public ITimeGetter Timer { get; set; }

}

/// <summary>
/// 判定が存在するノーツ
/// </summary>
public interface IJudgableNoteData
{
    JudgementWindow JudgementWindow { get; set; }

    NoteType NoteType { get; }
}

/// <summary>
/// 削り判定が存在するノーツ
/// </summary>
public interface IClippedJudgableNote
{
    public JudgementWindow JudgementWindow { get; set; }

    NoteType NoteType { get; }

    float Timing { get; }

    int[] Range { get; }
}

/// <summary>
/// 各種ノーツの生成を行う
/// </summary>
/// <typeparam name="T"></typeparam>
public interface INoteFactory<T> where T : INoteData
{
    void Initialize(NoteFactoryInitializingData initializingData);

    NoteObject<T> Spawn(T data, INotePositionCalculator positionCalculator);
}

/// <summary>
/// インスペクターで設定できるように基底クラスでラップ
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class NoteFactory<T> : MonoBehaviour, INoteFactory<T> where T : INoteData
{
    public abstract void Initialize(NoteFactoryInitializingData initializingData);

    public abstract NoteObject<T> Spawn(T data, INotePositionCalculator positionCalculator);
}

/// <summary>
/// ノーツインタラクトエフェクトの初期化など
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IInteractNoteEffectController<T> where T : INoteData
{
    void SetEffect(T noteData);

    void Play();
}

/// <summary>
/// ノーツのアクティブ化ができる
/// </summary>
public interface INoteActivable
{
    void SetActive(bool isVisible);
}