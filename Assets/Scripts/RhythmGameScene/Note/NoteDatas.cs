using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteJudgement;
using System.Linq;
using Deform;

/// <summary>
/// Factoryの初期化に必要なデータ
/// ノーツの初期化に必要な共通のデータはここに入れる
/// </summary>
public class NoteFactoryInitializingData
{
    public INoteSpawnDataOptionHolder OptionHolder { get; set; }

    public ISliderInputGetter SliderInputGetter { get; set; }

    public ISpaceInputGetter SpaceInputGetter { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public GameObject GroundObject { get; set; }

    public Deformer GroundDeformer { get; set; }
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

    public JudgementAndErrorTime GetJudgementAndError(float currentTime, float correctTiming)
    {
        float error = correctTiming - currentTime;

        // Good判定前
        if (correctTiming - goodWindow_faster > currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.None, Error = error }; }
        // Good判定後
        if (correctTiming + goodWindow_latter < currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.Miss, Error = error }; }

        if (-perfectWindow_faster < error && error <= perfectWindow_latter) { return new JudgementAndErrorTime { Judgement = Judgement.Perfect, Error = error }; }
        else if (-greatWindow_faster < error && error <= greatWindow_latter) { return new JudgementAndErrorTime { Judgement = Judgement.Great, Error = error }; }
        else if (-goodWindow_faster < error && error <= goodWindow_latter) { return new JudgementAndErrorTime { Judgement = Judgement.Good, Error = error }; }

        return new JudgementAndErrorTime { Judgement = Judgement.None };
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

    public JudgementWindow CheckAndGetJudgementWindow(NoteType noteType)
    {
        if(this.noteType == noteType)
        {
            return judgementWindowObject.JudgementWindow;
        }

        return null;
    }
}

/// <summary>
/// 判定に応じたSEの再生を纏めたクラス
/// </summary>
[System.Serializable]
public class JudgementSoundEffects
{
    [System.Serializable]
    public class JudgementToSE
    {
        [SerializeField] Judgement Judgement;
        [SerializeField] AudioClip audioClip;

        /// <summary>
        /// 条件と照らし合わせ、TrueであればSEの再生を行う
        /// </summary>
        /// <param name="judgement"></param>
        public bool CheckConditionAndPlaySE(Judgement judgement)
        {
            if (this.Judgement != judgement) { return false; }
            SoundManager.Instance.PlaySE(audioClip);
            return true;
        }
    }

    [SerializeField] List<JudgementToSE> sounds;

    public void PlaySE(Judgement judgement)
    {
        foreach (JudgementToSE judgementToSE in sounds)
        {
            if (judgementToSE.CheckConditionAndPlaySE(judgement)) { return; }
        }
    }
}

public class DynamicJudgement
{
    List<Vector3> judgeVectors;

    public DynamicJudgement(int[] range, Vector3 rotationVector, float magnitude)
    {
        judgeVectors = new List<Vector3>();

        // それぞれの判定ベクトルを調べる
        for (int i = 0; i < range.Length; i++)
        {
            Vector3 vector = NoteJudgement.DynamicNote.CalcJudgementThresHold(10, range[i], rotationVector);

            // 各要素が0でないか調べてmagnitudeを代入
            if (vector.x > 0) { vector.x = magnitude; }
            else if (vector.x < 0) { vector.x = -magnitude; }

            if (vector.y > 0) { vector.y = magnitude; }
            else if (vector.y < 0) { vector.y = -magnitude; }

            if (vector.z > 0) { vector.z = magnitude; }
            else if (vector.z < 0) { vector.z = -magnitude; }

            judgeVectors.Add(vector);
        }

        judgeVectors = judgeVectors.Distinct().ToList();
        // Debug.Log($"List: {string.Join(", ", judgeVectors)}");
    }

    public bool Judge(Vector3 diff)
    {
        foreach (Vector3 threshold in judgeVectors)
        {
            if (NoteJudgement.DynamicNote.JudgeThreshold(threshold, diff)) { return true; }
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
    public INoteData NoteData { get; set; }

    public Judgement Judgement { get; set; }

    public float TimingError { get; set; }

    public Vector3 PositionJudged { get; set; }
}

/// <summary>
/// ホールドノーツ用の纏めクラス
/// </summary>
public class TimeToRange
{
    public float Timing { get; set; }
    public float[] Range { get; set; }
}

/// <summary>
/// スペースホールドノーツ用の纏めクラス
/// </summary>
public class TimeToVertices
{
    public float Timing { get; set; }
    public Vector2[] Vertices { get; set; }
}

/// <summary>
/// 判定一覧
/// </summary>
public enum Judgement
{
    Perfect = 1000,
    Great = 100,
    Good = 10,
    Miss = 1,
    None = 0,
}

/// <summary>
/// ノーツタイプ
/// </summary>
public enum NoteType
{
    Touch = 1,
    HoldStart = 10,
    HoldRelay = 11,
    HoldRelayHidden = 12,
    HoldEnd = 13,
    HoldEndUnjudge = 15,
    HoldMesh = 14,
    SpaceHoldMesh = 20,
    SpaceHoldRelay = 21,
    SpaceHoldRelayHidden = 22,
    DynamicGroundUpward = 30,
    DynamicGroundDownward = 31,
    DynamicGroundRightward = 32,
    DynamicGroundLeftward = 33,
    DynamicSpace = 34,
}