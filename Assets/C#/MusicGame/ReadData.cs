using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;
using UnityEngine;

public class ReadData : MonoBehaviour
{
    [Header("通常ノートGoodの判定幅")] public float general_judge_time_good;
    [Header("通常ノートGreatの判定幅")] public float general_judge_time_great;
    [Header("通常ノートPerfectの判定幅")] public float general_judge_time_perfect;
    [Header("HoldノートGoodの判定幅")] public float hold_judge_time_good;
    [Header("HoldノートGreatの判定幅")] public float hold_judge_time_great;
    [Header("HoldノートPerfectの判定幅")] public float hold_judge_time_perfect;
    [Header("DynamicノートGoodの判定幅")] public float dynamic_judge_time_good;
    [Header("DynamicノートGreatの判定幅")] public float dynamic_judge_time_great;
    [Header("DynamicノートPerfectの判定幅")] public float dynamic_judge_time_perfect;

    const string GENERAL_NOTE = "g";
    const string HOLD_NOTE = "h";
    const string DYNAMIC_UP_NOTE = "d_up";
    const string DYNAMIC_DOWN_NOTE = "d_down";
    const string DYNAMIC_GROUND_RIGHT_NOTE = "d_gro_right";
    const string DYNAMIC_GROUND_LEFT_NOTE = "d_gro_left";

    private int NOTE_TIME_COLUMN;
    private int NOTE_KIND_COLUMN;
    private int NOTE_LANE_COLUMN;
    private int NOTE_JUDGE_COLUMN;
    private int NOTE_VECTOR_COLUMN;

    List<string[]> csvDatas = new List<string[]>(); // CSVの中身を入れるリスト;
    List<HoldNote> holdNotesList;     //ホールドノートを一時保存するリスト

    private bool isComplete;
    private NotesData notesData;

    //読み込むデータの書き換えとトリガー
    public void FirstFunc(TextAsset chart_file)
    {
        Init();
        csvDatas = ConvertTextAssetToStringList(chart_file);
        SetDataColumn();//その列が何を表しているか設定
        CSVDataToGameData();    //譜面データを挿入
        isComplete = true;
    }

    //初期化(譜面を読み込む毎に行う)
    private void Init()
    {
        notesData = new NotesData();
        csvDatas = new List<string[]>();
        holdNotesList = new List<HoldNote>();

        notesData.Init();
    }

    //一行ずつ読み込んで譜面データに挿入
    private void CSVDataToGameData()
    {
        string[] str;
        for (int i = 1; i < csvDatas.Count; i++)
        {
            str = csvDatas[i];
            if (str[0] == "" || str[0] == null) { break; }

            switch (ReturnKindOfNote(str))
            {
                case GENERAL_NOTE:
                    GeneralNote g = ConvertGeneralNote(str);
                    notesData.AddGeneralNote(g);
                    break;
                case HOLD_NOTE:
                    HoldNote h = ConvertHoldNote(str);
                    if (h.isStart) { notesData.AddHoldNote(h); }
                    break;
                case DYNAMIC_UP_NOTE:
                case DYNAMIC_DOWN_NOTE:
                case DYNAMIC_GROUND_RIGHT_NOTE:
                case DYNAMIC_GROUND_LEFT_NOTE:
                    DynamicNote d = ConvertDynamicNote(str);
                    notesData.AddDynamicNote(d);
                    break;
            }
        }

        //DebugAllNotesData(GENERAL_NOTE);
        EnableIsGoal();     //ホールドノートに終点判定を付与
    }

    //与えられた文字列のノーツの種類を返す
    private string ReturnKindOfNote(string[] str)
    {
        switch (str[NOTE_KIND_COLUMN])
        {
            case GENERAL_NOTE: return GENERAL_NOTE;
            case DYNAMIC_UP_NOTE: return DYNAMIC_UP_NOTE;
            case DYNAMIC_DOWN_NOTE: return DYNAMIC_DOWN_NOTE;
            case DYNAMIC_GROUND_RIGHT_NOTE: return DYNAMIC_GROUND_RIGHT_NOTE;
            case DYNAMIC_GROUND_LEFT_NOTE: return DYNAMIC_GROUND_LEFT_NOTE;
            default:
                if(str[NOTE_KIND_COLUMN][0] == HOLD_NOTE[0]) { return HOLD_NOTE; }
                else { Debug.LogError("知らないノートの種類です: " + str[NOTE_KIND_COLUMN]); }
                break;
        }
        
        return null;
    }

    //与えられた文字列をGeneralNoteに変換
    private GeneralNote ConvertGeneralNote(string[] str)
    {
        GeneralNote g = new GeneralNote();

        //timeをfloatに変換
        if(!float.TryParse(str[NOTE_TIME_COLUMN], out g.time)) { 
            Debug.LogError("time中にfloatに変換不可な文字列がありました: " + str[NOTE_TIME_COLUMN]); 
            return null;
        }

        g.judge_time = new float[6]
        {
            g.time - general_judge_time_good,      //前Good判定
            g.time - general_judge_time_great,     //前Grat判定
            g.time - general_judge_time_perfect,   //前Perfect判定
            g.time + general_judge_time_perfect,   //後Perfect判定
            g.time + general_judge_time_great,     //後Grat判定
            g.time + general_judge_time_good,      //後Good判定
        };

        //Laneをint[]に変換
        string[] s = str[NOTE_LANE_COLUMN].Split('t');  //「0t3」を「0」と「3」に分ける
        g.judge_lane = new int[2];
        for (int i = 0; i < 2; i++){
            if (!int.TryParse(s[i], out g.judge_lane[i]))
            {
                Debug.LogError("lane中にintに変換不可な文字列がありました: " + str[NOTE_LANE_COLUMN]);
                return null;
            }
        }
        return g;
    }

    //与えられた文字列をHoldNoteに変換
    private HoldNote ConvertHoldNote(string[] str)
    {
        HoldNote h = new HoldNote();

        //timeをfloatに変換
        if (!float.TryParse(str[NOTE_TIME_COLUMN], out h.time))
        {
            Debug.LogError("time中にfloatに変換不可な文字列がありました: " + str[NOTE_TIME_COLUMN]);
            return null;
        }

        //[KIND]を、"h"と数字に分ける
        int index;
        string[] s = str[NOTE_KIND_COLUMN].Split('_');
        if (!int.TryParse(s[1], out index))
        {
            Debug.LogError("kind中にintに変換不可な文字列がありました: " + str[NOTE_KIND_COLUMN]);
            return null;
        }
        //ロングノートの新規登録はインデックス+1なので、
        //indexがholdNoteListのインデックス+1を超えたときエラー
        if (index > holdNotesList.Count)
        {
            Debug.LogError("新規登録するHoldNoteは常にholdNotesListのインデックスの+1で無ければなりません: " + str[NOTE_KIND_COLUMN]);
            return null;
        }
        //既存のホールドノートの次ノートである場合、
        //holdNoteListを入れ替え、先頭のホールドノートを変更
        if(index < holdNotesList.Count) 
        {
            holdNotesList[index].next = h;
            holdNotesList[index] = h;
            //判定
            if (str[NOTE_JUDGE_COLUMN] == "TRUE")
            {
                h.isJudge = true;
                h.judge_time = new float[6] {
                    h.time - hold_judge_time_good,      //Good判定
                    h.time - hold_judge_time_great,     //Great判定
                    h.time - hold_judge_time_perfect,   //Perfect判定
                    h.time + hold_judge_time_perfect,   //Perfect判定
                    h.time + hold_judge_time_great,     //Great判定
                    h.time + hold_judge_time_good,      //Good判定
                };
            }
        }
        //新しいホールドノートである場合、holdNoteListに新規登録する
        else if(index == holdNotesList.Count)
        {
            holdNotesList.Add(h);
            h.isStart = true;
            h.isJudge = true;
            //判定
            h.judge_time = new float[6] {
                    h.time - general_judge_time_good,      //Good判定
                    h.time - general_judge_time_great,     //Great判定
                    h.time - general_judge_time_perfect,   //Perfect判定
                    h.time + general_judge_time_perfect,   //Perfect判定
                    h.time + general_judge_time_great,     //Great判定
                    h.time + general_judge_time_good,      //Good判定
            };
        }

        //Laneをint[]に変換
        s = str[NOTE_LANE_COLUMN].Split('t');  //「0t3」を「0」と「3」に分ける
        h.judge_lane = new float[2];
        for (int i = 0; i < 2; i++)
        {
            if (!float.TryParse(s[i], out h.judge_lane[i]))
            {
                Debug.LogError("lane中にfloatに変換不可な文字列がありました: " + str[NOTE_LANE_COLUMN]);
                return null;
            }
        }

        return h;
    }

    //与えられた文字列をDynamicNoteに変換
    private DynamicNote ConvertDynamicNote(string[] str)
    {
        DynamicNote d = new DynamicNote();

        //timeをfloatに変換
        if (!float.TryParse(str[NOTE_TIME_COLUMN], out d.time))
        {
            Debug.LogError("time中にfloatに変換不可な文字列がありました: " + str[NOTE_TIME_COLUMN]);
            return null;
        }

        d.judge_time = new float[6]
        {
            d.time - dynamic_judge_time_good,      //前Good判定
            d.time - dynamic_judge_time_great,     //前Grat判定
            d.time - dynamic_judge_time_perfect,   //前Perfect判定
            d.time + dynamic_judge_time_perfect,   //後Perfect判定
            d.time + dynamic_judge_time_great,     //後Grat判定
            d.time + dynamic_judge_time_good,      //後Good判定
        };

        //Laneをint[]に変換
        string[] s = str[NOTE_LANE_COLUMN].Split('t');  //「0t3」を「0」と「3」に分ける
        d.judge_lane = new int[2];
        for (int i = 0; i < 2; i++)
        {
            if (!int.TryParse(s[i], out d.judge_lane[i]))
            {
                Debug.LogError("lane中にintに変換不可な文字列がありました: " + str[NOTE_LANE_COLUMN]);
                return null;
            }
        }

        //種類別に分ける
        d.kind = str[NOTE_KIND_COLUMN];

        //VECTORをvector3に変換
        //d.judge_vector = StringToVector3(str[NOTE_VECTOR_COLUMN]);
        d.judge_vector = PositionToVector3(d.judge_lane, d.kind);

        return d;
    }

    //holdNotesListに残っているノーツを終点判定にする
    private void EnableIsGoal()
    {
        for(int i = 0; i < holdNotesList.Count; i++)
        {
            holdNotesList[i].isGoal = true;
        }
        holdNotesList.Clear();
    }

    //その列が何を表しているか設定
    private void SetDataColumn()
    {
        for (int i = 0; i < csvDatas[0].Length; i++)
        {
            switch (csvDatas[0][i]) {
                case "[TIME]":
                    NOTE_TIME_COLUMN = i;
                    break;
                case "[KIND]":
                    NOTE_KIND_COLUMN = i;
                    break;
                case "[LANE]":
                    NOTE_LANE_COLUMN = i;
                    break;
                case "[JUDGE]":
                    NOTE_JUDGE_COLUMN = i;
                    break;
                case "[VECTOR]":
                    NOTE_VECTOR_COLUMN = i;
                    break;
                default:
                    Debug.LogError("知らない文字列が入ってきた: " + csvDatas[0][i]);
                    break;
            }
        }
    }

    //---------------その他------------------

    //準備完了かどうか返す
    public bool isReturnReady()
    {
        return isComplete;
    }

    //出来上がった譜面データを返す
    public List<NotesBlock> ReturnGameData()
    {
        return notesData.ReturnScoreData();
    }

    //string「(0:0:0)」をVector3に変換
    public static Vector3 StringToVector3(string input)
    {
        //文字列から読み取る
        var elements = input.Trim('(', ')').Split(':'); // 前後に丸括弧があれば削除し、カンマで分割
        var result = Vector3.zero;
        var elementCount = Mathf.Min(elements.Length, 3); // ループ回数をelementsの数以下かつ3以下にする

        for (var i = 0; i < elementCount; i++)
        {
            float value;
            value = float.Parse(elements[i]);
            //float.TryParse(elements[i], out value); // 変換に失敗したときに例外が出る方が望ましければ、Parseを使うのがいいでしょう
            result[i] = value;
        }
        return result;
    }

    //ノーツ座標からVector3を生成
    public static Vector3 PositionToVector3(int[] lane, string kind)
    {
        //文字列から読み取る
        var result = Vector3.zero;
        float center = (lane[1] + lane[0]) / 2.0f;
        switch (kind)
        {
            case DYNAMIC_UP_NOTE:
                if(center <= 7.5f) { result = new Vector3(-0.2f, 0.2f, 0); }
                else { result = new Vector3(0.2f, 0.2f, 0); }
                break;
            case DYNAMIC_GROUND_RIGHT_NOTE:
                if (center <= 7.5f) { result = new Vector3(0.2f, -0.2f, 0); }
                else { result = new Vector3(0.2f, 0.2f, 0); }
                break;
            case DYNAMIC_GROUND_LEFT_NOTE:
                if (center <= 7.5f) { result = new Vector3(-0.2f, 0.2f, 0); }
                else { result = new Vector3(-0.2f, -0.2f, 0); }
                break;
        }

        return result;
    }

    //数字から難易度を返す
    private string ReturnDifficulityName(int num)
    {
        switch (num)
        {
            case 0:
                return "initiate";
            case 1:
                return "fanatic";
            case 2:
                return "skyclad";
            case 3:
                return "dream";
            default:
                Debug.LogError("0～3以外の数字が来たよ:" + num);
                return "";
        }
    }


    //--------------デバッグ用---------------

    //[デバッグ用]全てのノートデータを表示
    private void DebugAllNotesData(string s)
    {
        foreach (NotesBlock n in notesData.ReturnScoreData())
        {
            switch (s)
            {
                case GENERAL_NOTE:
                    if (n.general_list.Count == 0) { continue; }
                    foreach (GeneralNote g in n.general_list) { DebugGeneralNoteData(g); }
                    break;
                case HOLD_NOTE:
                    if (n.hold_list.Count == 0) { continue; }
                    foreach (HoldNote h in n.hold_list) { DebugHoldNoteData(h); }
                    break;
                case DYNAMIC_UP_NOTE:
                case DYNAMIC_DOWN_NOTE:
                case DYNAMIC_GROUND_RIGHT_NOTE:
                case DYNAMIC_GROUND_LEFT_NOTE:
                    if (n.dynamic_list.Count == 0) { continue; }
                    foreach (DynamicNote d in n.dynamic_list) { DebugDynamicNoteData(d); }
                    break;
            }

        }
    }

    //[デバッグ用]引数の通常ノートデータを表示
    private void DebugGeneralNoteData(GeneralNote g)
    {
        Debug.Log("[time: " + g.time + "]  [judge" + g.judge_time[0] + " ~ " + g.judge_time[5] + "]  [lane:" + g.judge_lane[0] + " ~ " + g.judge_lane[1] + "]");
    }

    //[デバッグ用]引数のホールドノートデータを表示
    private void DebugHoldNoteData(HoldNote h)
    {
        Debug.Log("[time: " + h.time + "]  [lane:" + h.judge_lane[0] + " ~ " + h.judge_lane[1] + "]" +
            "] [isStart: " + h.isStart + "] [isGoal:" + h.isGoal + "] [isJudge:" + h.isJudge + "] [next: " + h.next + "]");
    }

    //[デバッグ用]引数のダイナミックノートデータを表示
    private void DebugDynamicNoteData(DynamicNote d)
    {
        Debug.Log("[time: " + d.time + "]  [judge" + d.judge_time[0] + " ~ " + d.judge_time[5] 
            + "]  [lane:" + d.judge_lane[0] + " ~ " + d.judge_lane[1] + "] [vector " + d.judge_vector + "]");
    }

    /// <summary>
    /// CSVデータ(textAsset)をList<string>に変換
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cancellation_token"></param>
    /// <returns></returns>
    private List<string[]> ConvertTextAssetToStringList(TextAsset text)
    {
        List<string[]> list = new List<string[]>();
        StringReader reader = new StringReader(text.text);
        while (reader.Peek() != -1)
        {
            string line = reader.ReadLine();
            list.Add(line.Split(','));
        }

        return list;
    }
}