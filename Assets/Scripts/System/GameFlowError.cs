using System;
using UnityEngine;

/// <summary>
/// ゲームを先へ進められなくなるエラー (譜面・音源の読み込み失敗、ノーツの生成失敗など) を通知する。
/// 各シーンの PhaseTransitioner が購読し、元のシーン (選曲画面など) へ戻す。
/// </summary>
public static class GameFlowError
{
    /// <summary>
    /// エラーが通知されたとき (引数はエラーの内容)
    /// </summary>
    public static event Action<string> OnRaised;

    /// <summary>
    /// エラーを通知する。購読しているシーンがあれば、そのシーンが元のシーンへ戻る。
    /// </summary>
    public static void Raise(string message)
    {
        Debug.LogError($"【System】{message}");
        OnRaised?.Invoke(message);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnRaised = null;
    }
}
