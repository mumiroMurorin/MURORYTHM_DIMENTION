using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using JudgementUtil.Dynamic;
using System.Linq;

/// <summary>
/// ダイナミックグラウンドノーツにアタッチされるクラス
/// </summary>
public class NoteObject_DynamicGroundDownward : NoteObject<NoteData_DynamicGroundDownward>
{
    Vector3 JudgeVector => Vector3.down;

    [SerializeField] float judgeMagnitude;

    NoteData_DynamicGroundDownward noteData;
    DynamicJudgementHandler dynamicJudgement;
    Judgement bestJudgement = Judgement.Miss;
    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_DynamicGroundDownward data)
    {
        noteData = data;

        dynamicJudgement = new DynamicJudgementHandler(noteData.Range, JudgeVector, judgeMagnitude);
    }

    private void Update()
    {
        if (noteData == null) { return; }
        if (isJudged) { return; }

        // 判定時間過ぎてるとき
        if (noteData.JudgementWindow.IsPassJudgementRange(noteData.Timer.Time, noteData.Timing))
        {
            SendJudgementData();
            SetDisable();
            return;
        }

        // 判定時間内でないとき
        if (!IsInJudgementTimeRange()) { return; }

        // 通常時判定
        if (!noteData.OptionGetter.IsAutoMode)
        {
            NormalJudgement();
        }
        // オートモード時判定
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

        // 判定時間内かつ閾値を超えているとき
        bool isOverThresholdRight = dynamicJudgement.Judge(noteData.SpaceInput.GetSpaceInputVelocity(SpaceTrackingTag.RightHand).Value);
        bool isOverThresholdLeft = dynamicJudgement.Judge(noteData.SpaceInput.GetSpaceInputVelocity(SpaceTrackingTag.LeftHand).Value);
        if (!isOverThresholdRight && !isOverThresholdLeft) { return; }

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
    /// 判定の記録
    /// </summary>
    private void SendJudgementData()
    {
        var judgementData = new NoteJudgementData(this.noteData, bestJudgement, noteData.Timer.Time - noteData.Timing);

        noteData.JudgementRecorder?.RecordJudgement(judgementData);
        SoundManager.Instance.PlaySE(noteData.NoteType, bestJudgement);
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
        // Destroy(this.gameObject);
    }

}

/// <summary>
/// (初期化に必要な変数も含む)ダイナミックノーツ(アップ)のデータ
/// </summary>
public class NoteData_DynamicGroundDownward : INoteData, IJudgableNoteData
{
    public NoteType NoteType => NoteType.DynamicGroundDownward;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public int[] Range { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public INoteSpawnDataOptionHolder OptionGetter { get; set; }
}

