using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObject_HoldStart : NoteObject<NoteData_HoldStart>
{
    NoteData_HoldStart noteData;

    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldStart data)
    {
        noteData = data;

        Bind();
    }

    private void Bind()
    {
        if (noteData == null) { return; }
        if (noteData.OptionGetter.IsAutoMode) { return; }

        // 成功判定
        foreach (int index in noteData.Range)
        {
            if (noteData.SliderInput == null) { break; }
            if (noteData.Timer == null) { break; }

            noteData.SliderInput?.GetSliderInputReactiveProperty(index)
                .Where(isHoldStart => isHoldStart)
                .Where(_ => !isJudged)
                // Good判定時間に含まれているとき判定
                .Where(_ => noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => !noteData.OptionGetter.IsAutoMode)
                .Subscribe(_ =>
                {
                    NormalJudge();
                    SetDisable();
                })
                .AddTo(this.gameObject);
        }
    }

    private void Update()
    {
        // オートモード時
        if (noteData.OptionGetter.IsAutoMode && noteData.Timing <= noteData.Timer.Time)
        {
            NormalJudge();
            SetDisable();
            return;
        }

        // ミスった時
        if (noteData.JudgementWindow.IsPassJudgementRange(noteData.Timer.Time, noteData.Timing))
        {
            NormalJudge();
            SetDisable();
        }
    }


    /// <summary>
    /// 判定
    /// </summary>
    private void NormalJudge()
    {
        // 判定を得る
        Judgement judgement;
        if (noteData.OptionGetter.IsAutoMode) { judgement = Judgement.Perfect; }
        else { judgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing); }

        NoteJudgementData judgementData = new NoteJudgementData
        {
            Judgement = judgement,
            NoteData = this.noteData,
            TimingError = noteData.Timing - noteData.Timer.Time
        };

        noteData.JudgementRecorder?.RecordJudgement(judgementData);
        SoundManager.Instance.PlaySE(noteData.NoteType, judgement);
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
/// (初期化に必要な変数も含む)ホールド始点ノーツのデータ
/// </summary>
public class NoteData_HoldStart : INoteData, IClippedJudgableNote
{
    public NoteType NoteType => NoteType.HoldStart;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public int[] Range { get; set; }

    public ISliderInputGetter SliderInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public INoteSpawnDataOptionHolder OptionGetter { get; set; }
}

