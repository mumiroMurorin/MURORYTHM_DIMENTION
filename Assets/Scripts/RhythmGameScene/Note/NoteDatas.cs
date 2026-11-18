using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JudgementUtil.Dynamic;
using System.Linq;

/// <summary>
/// Factoryの初期化に必要なデータ
/// ノーツの初期化に必要な共通のデータはここに入れる
/// </summary>
public class NoteFactoryInitializingData
{
    public INoteSpawnDataOptionGetter OptionHolder { get; set; }

    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    public NoteJudgementSettingsCatalog JudgementSettingsCatalog { get; set; }

    public ISliderInputGetter SliderInputGetter { get; set; }

    public ISpaceInputGetter SpaceInputGetter { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public Transform NoteParent { get; set; }

    public T GetJudgementSettings<T>(NoteType noteType) where T : NoteJudgementConfig
    {
        return JudgementSettingsCatalog?.GetJudgementSettings<T>(noteType, Difficulty);
    }
}

/// <summary>
/// Perfect～Goodまでの判定許容範囲をまとめたクラス
/// </summary>
[System.Serializable]
public class JudgementWindow 
{
    [Header("それぞれの判定(秒)")]
    [SerializeField] float perfectWindow_faster;
    [SerializeField] float perfectWindow_latter;
    [SerializeField] float greatWindow_faster;
    [SerializeField] float greatWindow_latter;
    [SerializeField] float goodWindow_faster;
    [SerializeField] float goodWindow_latter;

    public float PerfectWindowFaster { get { return perfectWindow_faster; } }
    public float PerfectWindowLatter { get { return perfectWindow_latter; } }
    public float GreatWindowFaster { get { return greatWindow_faster; } }
    public float GreatWindowLatter { get { return greatWindow_latter; } }
    public float GoodWindowFaster { get { return goodWindow_faster; } }
    public float GoodWindowLatter { get { return goodWindow_latter; } }

    public Judgement GetJudgement(float currentTime, float judgeTime)
    {
        return GetJudgementAndError(currentTime, judgeTime).Judgement;
    }

    /// <summary>
    /// 判定誤差 早ければ- 遅ければ+
    /// </summary>
    /// <param name="currentTime"></param>
    /// <param name="correctTiming"></param>
    /// <returns></returns>
    public JudgementAndErrorTime GetJudgementAndError(float currentTime, float correctTiming)
    {
        // 判定誤差 早ければ- 遅ければ+
        float error = currentTime - correctTiming;

        // Good判定前
        if (correctTiming - goodWindow_faster > currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.None, Error = error }; }
        // Good判定後
        if (correctTiming + goodWindow_latter < currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.Miss, Error = error }; }

        if (-perfectWindow_faster < error && error <= perfectWindow_latter) { return new JudgementAndErrorTime { Judgement = Judgement.Perfect, Error = error }; }
        else if (-greatWindow_faster < error && error <= greatWindow_latter) { return new JudgementAndErrorTime { Judgement = Judgement.Great, Error = error }; }
        else if (-goodWindow_faster < error && error <= goodWindow_latter) { return new JudgementAndErrorTime { Judgement = Judgement.Good, Error = error }; }

        return new JudgementAndErrorTime { Judgement = Judgement.None };
    }

    public bool IsPassJudgementRange(float currentTime, float judgeTime)
    {
        return GetJudgement(currentTime, judgeTime) == Judgement.Miss;
    }

    public void ClipWindow(float clipDuration, bool isFaster)
    {
        if (isFaster)
        {
            float max = goodWindow_faster;
            perfectWindow_faster = Mathf.Clamp(max - clipDuration, 0, perfectWindow_faster);
            greatWindow_faster = Mathf.Clamp(max - clipDuration, perfectWindow_faster, greatWindow_faster);
            goodWindow_faster = Mathf.Clamp(max - clipDuration, greatWindow_faster, goodWindow_faster);
        }
        else
        {
            float max = goodWindow_latter;
            perfectWindow_latter = Mathf.Clamp(max - clipDuration, 0, perfectWindow_latter);
            greatWindow_latter = Mathf.Clamp(max - clipDuration, perfectWindow_latter, greatWindow_latter);
            goodWindow_latter = Mathf.Clamp(max - clipDuration, greatWindow_latter, goodWindow_latter);
        }
    }

    public JudgementWindow Copy()
    {
        return new JudgementWindow
        {
            perfectWindow_faster = this.perfectWindow_faster,
            perfectWindow_latter = this.perfectWindow_latter,

            greatWindow_faster = this.greatWindow_faster,
            greatWindow_latter = this.greatWindow_latter,

            goodWindow_faster = this.goodWindow_faster,
            goodWindow_latter = this.goodWindow_latter,
        };
    }
}

[System.Serializable]
public class NoteTypeToJudgementWindow
{
    [SerializeField] NoteType noteType;
    [SerializeField] JudgementWindowObject judgementWindowObject;
    [SerializeField] DifficultyToJudgementWindow[] difficultyJudgementWindows;

    public JudgementWindow CheckAndGetJudgementWindow(NoteType noteType)
    {
        return CheckAndGetJudgementWindow(noteType, Difficulty.Normal);
    }

    public JudgementWindow CheckAndGetJudgementWindow(NoteType noteType, Difficulty difficulty)
    {
        if(this.noteType == noteType)
        {
            JudgementWindow difficultyWindow = FindJudgementWindow(difficulty);
            if (difficultyWindow != null) { return difficultyWindow; }
            if (judgementWindowObject == null) { return null; }
            if (judgementWindowObject.JudgementWindow == null) { return null; }

            return judgementWindowObject.JudgementWindow;
        }

        return null;
    }

    private JudgementWindow FindJudgementWindow(Difficulty difficulty)
    {
        if (difficultyJudgementWindows == null) { return null; }

        for (int i = 0; i < difficultyJudgementWindows.Length; i++)
        {
            DifficultyToJudgementWindow data = difficultyJudgementWindows[i];
            if (data == null) { continue; }
            if (!data.CheckCondition(difficulty)) { continue; }

            return data.JudgementWindow;
        }

        return null;
    }
}

[System.Serializable]
public class DifficultyToJudgementWindow
{
    [SerializeField] Difficulty difficulty;
    [SerializeField] JudgementWindowObject judgementWindowObject;

    public JudgementWindow JudgementWindow
    {
        get
        {
            if (judgementWindowObject == null) { return null; }
            return judgementWindowObject.JudgementWindow;
        }
    }

    public bool CheckCondition(Difficulty difficulty)
    {
        return this.difficulty == difficulty;
    }
}

public class DynamicJudgementHandler
{
    // ノーツが乗っているレーンごとの判定の向き (入力空間での単位ベクトル)
    readonly List<Vector2> judgeDirections = new List<Vector2>();
    readonly float magnitude;

    /// <param name="range">ノーツが乗っているレーン番号</param>
    /// <param name="rotationVector">ノーツの向き (上 (0,1) = 半円の中心へ、右 (1,0) = レーン番号が増える向き)</param>
    /// <param name="magnitude">成功に必要な、判定の向きへの手の速さ</param>
    public DynamicJudgementHandler(int[] range, Vector3 rotationVector, float magnitude)
    {
        this.magnitude = magnitude;
        if (range == null) { return; }

        foreach (int laneIndex in range)
        {
            Vector2 direction = DynamicJudgement.GetLaneJudgeDirection(laneIndex, rotationVector);
            if (direction == Vector2.zero || judgeDirections.Contains(direction)) { continue; }

            judgeDirections.Add(direction);
        }
    }

    /// <summary>
    /// どれかのレーンの判定の向きに、magnitude 以上の速さで手が動いていれば成功
    /// </summary>
    public bool Judge(Vector3 velocity)
    {
        foreach (Vector2 direction in judgeDirections)
        {
            if (DynamicJudgement.JudgeDirection(direction, velocity, magnitude)) { return true; }
        }

        return false;
    }
}

public struct JudgementAndErrorTime
{
    public Judgement Judgement { get; set; }

    public float Error { get; set; }
}

/// <summary>
/// ノーツの判定情報を纏めたデータ
/// </summary>
public class NoteJudgementData
{
    public NoteJudgementData(INoteData noteData, Judgement judgement, float timingError)
    {
        this.NoteData = noteData;
        this.Judgement = judgement;
        this.TimingError = timingError;
    }

    public INoteData NoteData { get; }

    public Judgement Judgement { get; }

    public float TimingError { get; }

    public bool IsEnabledFastLate { get; set; }
}

public class TimeToPos
{
    public TimeToPos(float time, Vector3 pos)
    {
        Time = time;
        Pos = pos;
    }

    public float Time { get; set; }

    public Vector3 Pos { get; set; }
}

/// <summary>
/// ホールドノーツ用の纏めクラス
/// </summary>
public class TimeToRange
{
    public TimeToRange(float timing,float[] range)
    {
        Timing = timing;
        Range = range;
    }

    public float Timing { get; set; }
    public float[] Range { get; set; }
}

/// <summary>
/// スペースホールドノーツ用の纏めクラス
/// </summary>
public class TimeToVertices
{
    public TimeToVertices(float timing, Vector2[] vertices)
    {
        this.Timing = timing;
        this.Vertices = vertices;
    }

    public float Timing { get; set; }
    public Vector2[] Vertices { get; set; }
}

/// <summary>
/// 奥行→頂点リスト
/// </summary>
public class DepthToVertices
{
    public DepthToVertices(float depth, Vector2[] vertices)
    {
        this.Depth = depth;
        this.Vertices = vertices;
    }

    public float Depth { get; set; }
    public Vector2[] Vertices { get; set; }
}


