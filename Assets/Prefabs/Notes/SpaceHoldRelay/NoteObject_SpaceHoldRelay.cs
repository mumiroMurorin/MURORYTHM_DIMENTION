using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

public class NoteObject_SpaceHoldRelay : NoteObject<NoteData_SpaceHoldRelay>
{
    [SerializeField] float judgementMarginRadius = 0.25f;

    NoteData_SpaceHoldRelay noteData;

    Judgement bestJudgement = Judgement.Miss;
    Vector2[] judgeRange;
    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldRelay data)
    {
        noteData = data;
    }

    private void Update()
    {
        if (noteData == null) { return; }

        // 判定時間過ぎてるとき
        if (IsPassJudgementRange())
        {
            SendJudgementData();
            SetDisable();
            return;
        }

        // 判定時間内でないとき
        if (!IsInJudgementTimeRange()) { return; }

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
            judgeRange = InterpolatePoints(noteData.TimeToVertices, noteData.Timer.Time);
        }
        // 後ろ判定、判定時間時のレンジをキープ
        else
        {
            judgeRange = noteData.Vertices;
        }

        // 判定時間内かつ枠内に手があるとき
        if (IsInJudgementTimeRange() && noteData.SpaceInput.IsInSpaceRange(judgeRange, judgementMarginRadius))
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
        NoteJudgementData judgementData = new NoteJudgementData
        {
            Judgement = bestJudgement,
            NoteData = this.noteData,
            PositionJudged = noteData.Vertices.First(),
            TimingError = noteData.Timing - noteData.Timer.Time
        };

        SoundManager.Instance.PlaySE(noteData.NoteType, bestJudgement);
        noteData.JudgementRecorder?.RecordJudgement(judgementData);
        isJudged = true;
    }

    /// <summary>
    /// 判定範囲内か調べる
    /// </summary>
    /// <returns></returns>
    private bool IsInJudgementTimeRange()
    {
        if (noteData == null) { return false; }
        if (noteData.Timer == null) { return false; }
        if (isJudged) { return false; }

        Judgement judgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
        if (judgement == Judgement.Miss || judgement == Judgement.None) { return false; }

        return true;
    }

    /// <summary>
    /// ノーツ判定範囲外？
    /// </summary>
    /// <returns></returns>
    private bool IsPassJudgementRange()
    {
        if (noteData == null) { return false; }
        if (noteData.Timer == null) { return false; }
        if (noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.Miss) { return false; }
        if (isJudged) { return false; }

        return true;
    }

    /// <summary>
    /// ノーツを機能停止する
    /// </summary>
    private void SetDisable()
    {
        this.gameObject.SetActive(false);
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
/// </summary>
public class NoteData_SpaceHoldRelay : INoteData, IJudgableNoteData
{
    public NoteType NoteType => NoteType.SpaceHoldRelay;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public Vector2[] Vertices { get; set; }

    public List<TimeToVertices> TimeToVertices { get; set; }

    public Mesh Mesh { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public INoteSpawnDataOptionHolder OptionGetter { get; set; }
}

