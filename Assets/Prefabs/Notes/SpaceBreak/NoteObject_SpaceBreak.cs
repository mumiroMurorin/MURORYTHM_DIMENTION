using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

public class NoteObject_SpaceBreak : NoteObject<NoteData_SpaceBreak>
{
    [SerializeField] float judgementMarginRadius = 0.25f;

    NoteData_SpaceBreak noteData;

    Judgement bestJudgement = Judgement.Miss;
    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceBreak data)
    {
        noteData = data;
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

        if (!noteData.OptionGetter.IsAutoMode) 
        {
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
            StartDestroyAnimation();
            SetDisable();
            return;
        }

        // 判定時間内かつ枠内に手があるとき
        bool isInRange = noteData.SpaceInput.IsInSpaceRange(noteData.Vertices, judgementMarginRadius);
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
            StartDestroyAnimation();
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
        StartDestroyAnimation();
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

        Judgement judgement = noteData.JudgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
        if (judgement == Judgement.Miss || judgement == Judgement.None) { return false; }

        return true;
    }

    private void StartDestroyAnimation()
    {
        var bombObj = noteData.FlagmentBomb.gameObject;
        bombObj.transform.SetParent(null);
        bombObj.transform.position = new Vector3(bombObj.transform.position.x, bombObj.transform.position.y, 0f);

        var center = new Vector3(noteData.Mesh.vertices.Center().x, noteData.Mesh.vertices.Center().y, bombObj.transform.position.z);

        noteData.FlagmentBomb.Explosion(center);
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
public class NoteData_SpaceBreak : INoteData, IJudgableNoteData
{
    public NoteType NoteType => NoteType.SpaceBreak;

    public float Timing { get; set; }

    public JudgementWindow JudgementWindow { get; set; }

    public Vector2[] Vertices { get; set; }

    public Mesh Mesh { get; set; }

    public FragmentsBomb FlagmentBomb { get; set; }

    public ISpaceInputGetter SpaceInput { get; set; }

    public ITimeGetter Timer { get; set; }

    public IJudgementRecorder JudgementRecorder { get; set; }

    public INoteSpawnDataOptionHolder OptionGetter { get; set; }
}

