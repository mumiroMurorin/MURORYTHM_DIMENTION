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
    Touch,
    HoldStart,
    HoldRelay,
    HoldEnd,
    HoldMesh,
    SpaceHoldMesh,
    SpaceHoldRelay,
    DynamicGroundUpward,
    DynamicGroundDownward,
    DynamicGroundRightward,
    DynamicGroundLeftward,
    DynamicSpace
}

public enum JudgementType
{
    None,
    General,
    Clipped
}