using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JudgementUtil.Hold;
using UniRx;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObject_HoldEndUnjudge : NoteObject<NoteData_HoldEndUnjudge>
{
    NoteData_HoldEndUnjudge noteData;

    bool isJudged;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldEndUnjudge data)
    {
        noteData = data;
    }

    private void Update()
    {
        // 判定時間を過ぎたとき
        if (IsPassJudgementRange())
        {
            SetDisable();
        }
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
    /// ノーツ判定範囲外？
    /// </summary>
    /// <returns></returns>
    private bool IsPassJudgementRange()
    {
        if (noteData == null) { return false; }
        if (isJudged) { return false; }

        // 判定なしなので判定時間になったら消す
        if (noteData.Timer?.Time < noteData.Timing) { return false; }

        return true;
    }
}

/// <summary>
/// (初期化に必要な変数も含む)ホールド終点(判定なし)ノーツのデータ
/// </summary>
public class NoteData_HoldEndUnjudge : INoteData
{
    public NoteType NoteType => NoteType.HoldEndUnjudge;

    public float Timing { get; set; }

    public int[] Range { get; set; }

    public ITimeGetter Timer { get; set; }

    public INoteSpawnDataOptionHolder OptionGetter { get; set; }
}

