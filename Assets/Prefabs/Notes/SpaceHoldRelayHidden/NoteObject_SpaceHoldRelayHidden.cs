using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using JudgementUtil.SpacaHold;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObject_SpaceHoldRelayHidden : NoteObject<NoteData_SpaceHoldRelayHidden>
{
    NoteData_SpaceHoldRelayHidden noteData;

    Judgement bestJudgement = Judgement.Miss;
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
        // 判定時間内かつスライダーが押されているとき
        if (IsInJudgementTimeRange() && IsInSpaceRange())
        {
            // 記録した判定よりいい判定だったとき判定の更新
            Judgement currentJudgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            if ((int)bestJudgement < (int)currentJudgement)
            {
                bestJudgement = currentJudgement;
            }

            // 最高判定のとき確定
            if (bestJudgement == Judgement.Perfect)
            {
                SendJudgementData();
            }
        }
        // 判定時間を過ぎたとき
        else if (IsPassJudgementRange())
        {
            SendJudgementData();
            SetDisable();
        }
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
    /// ノーツ範囲内に手があるか判定
    /// </summary>
    /// <returns></returns>
    private bool IsInSpaceRange()
    {
        if (noteData.SpaceInput == null) { return false; }
        if (noteData.Timer == null) { return false; }

        // 右手の判定
        int rightCount = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand).Count;
        if(rightCount < 2) { return false; }

        var rightPos1 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand)[rightCount - 1].Pos;
        var rightPos2 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand)[rightCount - 2].Pos;
        bool isRightIn = SpaceHoldJudgement.IsSegmentIntersectingOrInsidePolygon(rightPos1, rightPos2, noteData.Vertices);

        // 左手の判定
        int leftCount = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.RightHand).Count;
        if (leftCount < 2) { return false; }

        var leftPos1 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.LeftHand)[leftCount - 1].Pos;
        var leftPos2 = noteData.SpaceInput.GetSpaceInput(SpaceTrackingTag.LeftHand)[leftCount - 2].Pos;
        bool isLeftIn = SpaceHoldJudgement.IsSegmentIntersectingOrInsidePolygon(leftPos1, leftPos2, noteData.Vertices);

        return isRightIn || isLeftIn;
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
public class NoteData_SpaceHoldRelayHidden : INoteData, IJudgableNoteData
{
    public NoteType NoteType => NoteType.SpaceHoldRelayHidden;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public Vector2[] Vertices { get; set; }

    public Mesh Mesh { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }
}

