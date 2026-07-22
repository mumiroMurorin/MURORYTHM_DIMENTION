using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

public class NoteObject_SpaceHoldRelayHidden : NoteObject<NoteData_SpaceHoldRelayHidden>
{
    [SerializeField] float judgementMarginRadius = 0.25f;

    NoteData_SpaceHoldRelayHidden noteData;

    Judgement bestJudgement = Judgement.Miss;
    Vector2[] judgeRange;
    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldRelayHidden data)
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
        if (!IsInJudgementTimeRange()) { return; }

        if (!noteData.OptionGetter.IsAutoMode) 
        {
            UpdateJudgementRange();
            NormalJudgement();
        }
        else 
        {
            AutoJudgement();
        }
    }

    /// <summary>
    /// 判定
    /// </summary>
    private void NormalJudgement()
    {
        // 判定時間外なら返す
        if (!IsInJudgementTimeRange()) { return; }

        // 最高判定且つノーツが過ぎたとき判定送信
        if (bestJudgement == Judgement.Perfect && noteData.Timing <= noteData.Timer.Time)
        {
            SendJudgementData();
            SetDisable();
            return;
        }

        // 判定時間内かつ枠内に手があるとき
        bool isInRange = noteData.SpaceInput.IsInSpaceRange(judgeRange, judgementMarginRadius);
        if (!isInRange) { return; }

        var jae = noteData.JudgementWindow.GetJudgementAndError(noteData.Timer.Time, noteData.Timing);

        // 最高判定の更新
        if ((int)bestJudgement < (int)jae.Judgement)
        {
            bestJudgement = jae.Judgement;
        }

        // 遅めだった時、即時判定
        if (jae.Error > 0)
        {
            SendJudgementData();
            SetDisable();
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
        isJudged = true;
    }

    /// <summary>
    /// 判定範囲の更新
    /// </summary>
    private void UpdateJudgementRange()
    {
        // 前判定
        if (noteData.Timing >= noteData.Timer.Time)
        {
            judgeRange = InterpolatePointsByDepth(noteData.DepthToVertices, GetCurrentDepth());
        }
        // 後ろ判定、判定時間時のレンジをキープ
        else
        {
            judgeRange = InterpolatePointsByDepth(noteData.DepthToVertices, GetDepth(noteData.Timing));
        }
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
    /// ノーツを機能停止する
    /// </summary>
    private void SetDisable()
    {
        this.gameObject.SetActive(false);
    }

    private float GetCurrentDepth()
    {
        if (noteData == null) { return 0f; }
        if (noteData.Timer == null) { return 0f; }
        if (noteData.PositionCalculator == null) { return 0f; }

        return GetDepth(noteData.Timer.Time);
    }

    private float GetDepth(float time)
    {
        if (noteData == null) { return 0f; }
        if (noteData.PositionCalculator == null) { return 0f; }

        return noteData.PositionCalculator.GetPosition(time) * noteData.NoteSpeed;
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
/// </summary>
public class NoteData_SpaceHoldRelayHidden : INoteData, IJudgableNoteData, ISpaceHoldBulletEffectNoteData
{
    public NoteType NoteType => NoteType.SpaceHoldRelayHidden;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public Vector2[] Vertices { get; set; }

    public int HoldNumber { get; set; }

    public bool IsSpaceHoldEnd { get; set; }

    public List<TimeToVertices> TimeToVertices { get; set; }

    public List<DepthToVertices> DepthToVertices { get; set; }

    public INotePositionCalculator PositionCalculator { get; set; }

    public float NoteSpeed { get; set; }

    public Mesh Mesh { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public INoteSpawnDataOptionGetter OptionGetter { get; set; }
}

