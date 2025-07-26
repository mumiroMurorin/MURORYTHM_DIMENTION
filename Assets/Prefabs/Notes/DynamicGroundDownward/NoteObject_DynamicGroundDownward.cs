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

        Bind();
    }

    private void Bind()
    {
        if (noteData == null) { return; }
        if (noteData.SpaceInput == null) { return; }
        if (noteData.OptionGetter.IsAutoMode) { return; }

        // 右手
        noteData.SpaceInput?.GetSpaceInputVelocity(SpaceTrackingTag.RightHand)
            .Where(_ => noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
            .Where(_ => !isJudged)
            .Subscribe(Judge)
            .AddTo(this.gameObject);

        // 左手
        noteData.SpaceInput?.GetSpaceInputVelocity(SpaceTrackingTag.LeftHand)
            .Where(_ => noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
            .Where(_ => !isJudged)
            .Subscribe(Judge)
            .AddTo(this.gameObject);
    }

    private void Update()
    {
        // オートモード時
        if (noteData.OptionGetter.IsAutoMode && noteData.Timing <= noteData.Timer.Time)
        {
            bestJudgement = Judgement.Perfect;
            RecordJudgement();
            SetDisable();
            return;
        }

        if (JudgeMiss())
        {
            RecordJudgement();
            SetDisable();
        }
    }

    /// <summary>
    /// 判定
    /// </summary>
    private void Judge(Vector3 velocity)
    {
        //Debug.Log($"【Judge】Downward velocity:{velocity}, {dynamicJudgement.Judge(velocity)}, {this.gameObject.name}");

        // 閾値から出てるか判定
        if (!dynamicJudgement.Judge(velocity)) { return; }

        // 判定を更新
        Judgement currentJudgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
        if ((int)bestJudgement < (int)currentJudgement)
        {
            bestJudgement = currentJudgement;
        }

        // Perfectだったときは問答無用でPerfect
        if (bestJudgement == Judgement.Perfect) { RecordJudgement(); }

        // Great以下だったときはMiss判定まで待ち

        return;
    }

    /// <summary>
    /// ミス判定
    /// </summary>
    /// <returns></returns>
    private bool JudgeMiss()
    {
        if (noteData == null) { return false; }
        if (noteData.Timer == null) { return false; }
        if (noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.Miss) { return false; }
        if (isJudged) { return false; }

        return true;
    }

    /// <summary>
    /// 判定の記録
    /// </summary>
    private void RecordJudgement()
    {
        NoteJudgementData judgementData = new NoteJudgementData
        {
            Judgement = bestJudgement,
            NoteData = this.noteData,
            TimingError = noteData.Timing - noteData.Timer.Time
        };

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

