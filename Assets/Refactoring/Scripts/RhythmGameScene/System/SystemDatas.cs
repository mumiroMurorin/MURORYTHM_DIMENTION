using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// インゲーム部の内部ステータスの列挙型
    /// </summary>
    public enum PhaseStatusInRhythmGame
    {
        LoadData,
        LoadChart,
        FadeIn,
        LoadBody,
        StartAnimation,
        Play,
        EndAnimation,
        FadeOut,
        TransitionResultScene,
    }

    /// <summary>
    /// リザルトシーンの内部ステータス
    /// </summary>
    public enum PhaseStatusInResultScene
    {
        LoadData,
        FadeIn,
        Result,
        FadeOut,
        TransitionSelectScene,
    }

    /// <summary>
    /// セレクトシーンの内部ステータス
    /// </summary>
    public enum PhaseStatusInSelectScene
    {
        LoadData,
        FadeIn,
        MusicSelect,
        DetailSelect,
        FadeOut,
        TransitionRhythmGameScene,
    }
}
