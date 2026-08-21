using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFactory_DivineTouch : NoteFactory<NoteData_DivineTouch>
{
    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] NoteJudgementSettings judgementSettings;

    [Header("マスに応じたノーツタイル")]
    [SerializeField] GameObject singleTilePrefab;
    [SerializeField] GameObject rightEdgeTilePrefab;
    [SerializeField] GameObject centerTilePrefab;
    [SerializeField] GameObject leftEdgeTilePrefab;

    INoteSpawnDataOptionGetter optionHolder;
    ISliderInputGetter sliderInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    Transform noteParent;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.sliderInputGetter = initializingData.SliderInputGetter;
        this.judgementRecorder = initializingData.JudgementRecorder;
        this.timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_DivineTouch> Spawn(NoteData_DivineTouch data, INotePositionCalculator positionCalculator)
    {
        // 生成
        NoteObject<NoteData_DivineTouch> note = GenerateNoteInstance(ConvertNoteData(data));

        // 位置調整
        SetTransform(note, positionCalculator.GetPosition(data.Timing) * optionHolder.NoteSpeed.Value);

        // 初期化
        note.Initialize(data);

        return note;
    }

    /// <summary>
    /// ノートデータにさらなる情報を追加
    /// </summary>
    /// <param name="data"></param>
    private NoteData_DivineTouch ConvertNoteData(NoteData_DivineTouch data)
    {
        // ノーツデータにいろいろ追加
        data.SliderInput = this.sliderInputGetter;
        data.Timer = this.timer;
        data.JudgementRecorder = this.judgementRecorder;
        data.OptionGetter = optionHolder;
        data.JudgementSettings = judgementSettings;
        if (judgementSettings != null)
        {
            data.JudgementWindow = judgementSettings.CreateJudgementWindowIfMissing(data.JudgementWindow);
        }

        return data;
    }

    /// <summary>
    /// ノーツをインスタンス化して返す
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private NoteObject<NoteData_DivineTouch> GenerateNoteInstance(NoteData_DivineTouch data)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // ノーツオブジェクトを生成
        GameObject noteObj = GenerateNoteObject(data.Range.Length);

        // originにくっつける
        noteObj.transform.SetParent(origin.transform);

        // 角度(レーン)調整
        noteObj.transform.eulerAngles = new Vector3(0, 0, CalcNoteTransform.NoteAngle(data.Range));

        // コンポーネントを取得
        NoteObject<NoteData_DivineTouch> note = origin.GetComponent<NoteObject<NoteData_DivineTouch>>();

        return note;
    }

    /// <summary>
    /// ノーツタイルを組み合わせてノーツを生成
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    private GameObject GenerateNoteObject(int size)
    {
        Vector3 pos, rot;
        GameObject pre = new GameObject("NoteObjects");
        NoteLayerUtility.SetNotesLayer(pre);   //まとめ役のオブジェクト生成

        // 1マスずつ生成
        for (int i = 0; i < size; i++)
        {
            // ※ポジションと角度の計算

            float radian = (5.625f * (2 * i - size + 1) - 90f) * Mathf.Deg2Rad;
            pos = new Vector3(10 * Mathf.Cos(radian), 10 * Mathf.Sin(radian), 0);
            rot = new Vector3(0, 0, ((size - 1) / 2f - (size - i - 1)) * 11.25f);

            // 1マスノートの時
            if (size == 1) { Instantiate(singleTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            // ノート左端の時
            else if (i == 0) { Instantiate(leftEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            // ノート右端の時
            else if (i == size - 1) { Instantiate(rightEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            // ノート中の時
            else { Instantiate(centerTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
        }

        return pre;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_DivineTouch> note, float spawnZ)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(spawnZ, optionHolder.NoteCurveRadius.Value);
    }
}
