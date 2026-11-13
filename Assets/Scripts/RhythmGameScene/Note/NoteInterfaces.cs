using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
/// 生成結果が「それまでに生成したノーツ」に左右されるファクトリに付ける目印
/// (重なったホールドの描画順、ステンシル番号、輪郭線の重複除去など)
///
/// こうしたファクトリは、一部のノーツだけ作り直すと本編と結果が変わってしまう。
/// 譜面エディタのプレビューは、この目印が付いた種類だけ、変更があるたびに
/// Initialize からやり直して譜面順に全部作り直す。
/// Spawn をまたいで状態を持つファクトリを新しく作ったら、これを付けること。
/// </summary>
public interface IOrderDependentNoteFactory
{
}

/// <summary>
/// ノーツインタラクトエフェクトの初期化など
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IInteractNoteEffectController
{
    void SetEffect(INoteData noteData, Judgement judgement, Action<IInteractNoteEffectController> returnToPool);

    void SetTransform(Vector3 pos, Quaternion rotation);

    void Play();
}

/// <summary>
/// ノーツ判定エフェクトの初期化など
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IJudgementEffectController
{
    void SetEffect(Judgement judgement, Action<IJudgementEffectController> returnToPool, float error = 0f);

    void SetTransform(Vector3 pos, Quaternion rotation);

    void Play();
}

/// <summary>
/// ノーツのアクティブ化ができる
/// </summary>
public interface INoteActivable
{
    void SetActive(bool isVisible);
}

/// <summary>
/// 譜面上の累積距離を使って表示を切り替えられるノーツ
/// </summary>
public interface INoteVisibilityTarget : INoteActivable
{
    float StartChartDistance { get; }

    float EndChartDistance { get; }

    bool IsVisibilityLocked { get; }

    bool ShouldLockVisibility(float currentDistance);

    void LockVisibility();

    void UnlockVisibility();
}

public interface INoteRangeVisibilityTarget
{
    void SetVisibleRange(float minDistance, float maxDistance);
}
