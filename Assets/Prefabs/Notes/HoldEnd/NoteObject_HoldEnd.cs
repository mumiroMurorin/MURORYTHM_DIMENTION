using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using JudgementUtil;
using JudgementUtil.Hold;
using System.Linq;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObject_HoldEnd : NoteObject<NoteData_HoldEnd>
{
    NoteData_HoldEnd noteData;

    bool isJudged;
    List<int> judgeRange = new List<int>();
    Judgement bestJudgement = Judgement.Miss;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldEnd data)
    {
        noteData = data;
    }

    private void Update()
    {
        if (noteData == null) { return; }

        // 判定時間過ぎてるとき
        if (noteData.JudgementWindow.IsPassJudgementRange(noteData.Timer.Time, noteData.Timing))
        {
            SendJudgementData();
            SetDisable();
            return;
        }

        // 判定時間内でないとき
        if (!IsInJudgementRange()) { return; }

        if (!noteData.OptionGetter.IsAutoMode) { NormalJudgement(); }
        else { AutoJudgement(); }
    }

    /// <summary>
    /// 判定
    /// </summary>
    private void NormalJudgement()
    {
        // 判定範囲の更新
        // 前判定
        if (noteData.Timing >= noteData.Timer.Time)
        {
            judgeRange = HoldJudgement.GetJudgeRange(noteData.TimeToRanges, noteData.Timer.Time);
        }
        // 後ろ判定、判定時間時のレンジをキープ
        else
        {
            judgeRange = noteData.Range.ToList();
        }

        // 判定時間内かつスライダーが押されているとき
        if (GroundJudgement.IsTouchingSlider(noteData.SliderInput, judgeRange.ToArray()))
        {
            // 記録した判定よりいい判定だったとき判定の更新
            Judgement currentJudgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            if ((int)bestJudgement < (int)currentJudgement)
            {
                bestJudgement = currentJudgement;
            }

            // 最高判定のとき確定
            if (bestJudgement == Judgement.Perfect && noteData.Timing <= noteData.Timer.Time)
            {
                SendJudgementData();
                SetDisable();
            }
        }
    }

    /// <summary>
    /// オート判定
    /// </summary>
    private void AutoJudgement()
    {
        // 最高判定のとき確定
        if (noteData.Timing > noteData.Timer.Time) { return; }

        bestJudgement = Judgement.Perfect;
        SendJudgementData();
        SetDisable();
    }

    /// <summary>
    /// 判定データを送信
    /// </summary>
    private void SendJudgementData()
    {
        var judgementData = new NoteJudgementData(this.noteData, bestJudgement, noteData.Timer.Time - noteData.Timing);

        noteData.JudgementRecorder?.RecordJudgement(judgementData);
        SoundManager.Instance.PlaySE(noteData.NoteType, bestJudgement);
        isJudged = true;
    }

    /// <summary>
    /// ノーツを機能停止する
    /// </summary>
    private void SetDisable()
    {
        this.gameObject.SetActive(false);
        // Destroy(this.gameObject);
    }

    /// <summary>
    /// 判定範囲内か調べる
    /// </summary>
    /// <returns></returns>
    private bool IsInJudgementRange()
    {
        if (noteData == null) { return false; }
        if (noteData.Timer == null) { return false; }
        if (isJudged) { return false; }

        Judgement judgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
        if (judgement == Judgement.Miss || judgement == Judgement.None) { return false; }

        return true;
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールド終点ノーツのデータ
/// </summary>
public class NoteData_HoldEnd : INoteData, IJudgableNoteData
{
    public NoteType NoteType => NoteType.HoldEnd;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public NoteJudgementSettings JudgementSettings { get; set; }

    public int[] Range { get; set; }

    public List<TimeToRange> TimeToRanges { get; set; }

    public ISliderInputGetter SliderInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public INoteSpawnDataOptionGetter OptionGetter { get; set; }
}

