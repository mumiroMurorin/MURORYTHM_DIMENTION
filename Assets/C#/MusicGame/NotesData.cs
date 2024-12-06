using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralNote
{
    public float time;
    public float[] judge_time;  //[6]
    public int[] judge_lane;    //[2]
    public GameObject obj;
}

public class HoldNote
{
    public float time;
    public bool isStart;
    public bool isGoal;
    public bool isJudge;
    public float[] judge_time;    //[6]
    public float[] judge_lane;    //[2]
    public GameObject obj;
    public HoldNote next;
}

public class DynamicNote
{
    public string kind;
    public float time;
    public float[] judge_time;  //[6]
    public int[] judge_lane;    //[2]
    public Vector3 judge_vector;
    public GameObject obj;
}

public class NotesBlock
{
    public float time;
    public List<GeneralNote> general_list;
    public List<HoldNote> hold_list;
    public List<DynamicNote> dynamic_list;
}

public class NotesData
{
    List<NotesBlock> score;   //譜面データ

    //初期化
    public void Init()
    {
        score = new List<NotesBlock>();
    }

    //譜面データに通常ノートを追加
    public bool AddGeneralNote(GeneralNote g)
    {
        //既に譜面データに同じtimeのデータがある場合、挿入
        int index = ReturnExistTimeIndex(g.time);
        if (index != -1){ score[index].general_list.Add(g); }
        else    //上記を満たさなかった場合、新たにそのtimeのNotesBlockを生成して譜面データに追加
        {
            NotesBlock notesBlock = ReturnVoidNotesBlock();
            notesBlock.time = g.time;
            notesBlock.general_list = new List<GeneralNote> { g };
            score.Add(notesBlock);
        }

        //ノート判定が重複したときの処理
        if(score.Count >= 2) {
            List<GeneralNote> formers;
            for (int i = score.Count - 2; i >= 0; i--)
            {
                //時間を過ぎたらbreak
                if (g.judge_time[0] > score[i].time) { break; }
                //通常ノートが無かったらcontinue
                if (score[i].general_list.Count == 0) { continue; }
                //時間が修正されたらbreak
                formers = score[i].general_list;
                if (AdjustmentNoteJudgeTime(ref formers, ref g)) { break; }
            }
        }

        return true;
    }

    //譜面データの通常ノートの判定被りを修正する。修正した場合はtrue、そうでない場合falseを返す
    private bool AdjustmentNoteJudgeTime(ref List<GeneralNote> formers, ref GeneralNote latter)
    {
        GeneralNote former;
        for (int i = 0; i < formers.Count; i++)
        {
            former = formers[i];
            //時間が被っている && 判定ラインが被っているかをチェック
            if (IsReturnJudgeTimeLapping(former, latter) && IsReturnJudgeLaneOverLapping(former, latter))
            {
                former.judge_time[5] = former.time + (latter.time - former.time) / 2f;   //前ノートの後ろ判定をずらす
                if (former.judge_time[5] < former.judge_time[4])
                {     //前ノートの後ろgood判定よりも後ろgreat判定が遅かったとき調整
                    former.judge_time[4] = former.judge_time[5];
                }
                if (former.judge_time[4] < former.judge_time[3])
                {      //前ノートの後ろgreat判定よりも後ろperfect判定が遅かったとき調整
                    former.judge_time[3] = former.judge_time[4];
                }

                latter.judge_time[0] = former.time + (latter.time - former.time) / 2f; //後ノーツの前判定をずらす
                if (former.judge_time[0] > former.judge_time[1])
                {   //後ノートの前good判定よりも前great判定が早かったとき調整
                    former.judge_time[1] = former.judge_time[0];
                }
                if (former.judge_time[1] > former.judge_time[2])
                {   //後ノートの前great判定よりも前perfect判定が早かったとき調整
                    former.judge_time[2] = former.judge_time[1];
                }
                return true;
            }
        }
        
        return false;
    }

    //判定時間が被っているか返す
    private bool IsReturnJudgeTimeLapping(GeneralNote former, GeneralNote latter)
    {
        if(former.judge_time[5] > latter.judge_time[0]) { return true; }
        return false;
    }

    //判定ラインが被っているかどうか返す
    private bool IsReturnJudgeLaneOverLapping(GeneralNote former, GeneralNote latter)
    {
        if (former.judge_lane[0] <= latter.judge_lane[1] && former.judge_lane[1] >= latter.judge_lane[0]) { return true; }
        return false;
    }

    //譜面データにホールドノートを追加
    public bool AddHoldNote(HoldNote h)
    {
        //既に譜面データに同じtimeのデータがある場合、挿入
        int index = ReturnExistTimeIndex(h.time);
        if (index != -1) { score[index].hold_list.Add(h); }
        else    //上記を満たさなかった場合、新たにそのtimeのNotesBlockを生成して譜面データに追加
        {
            NotesBlock notesBlock = ReturnVoidNotesBlock();
            notesBlock.time = h.time;
            notesBlock.hold_list = new List<HoldNote> { h };
            score.Add(notesBlock);
        }

        return true;
    }

    //譜面データに通常ノートを追加
    public bool AddDynamicNote(DynamicNote d)
    {
        //既に譜面データに同じtimeのデータがある場合、挿入
        int index = ReturnExistTimeIndex(d.time);
        if (index != -1) { score[index].dynamic_list.Add(d); }
        else    //上記を満たさなかった場合、新たにそのtimeのNotesBlockを生成して譜面データに追加
        {
            NotesBlock notesBlock = ReturnVoidNotesBlock();
            notesBlock.time = d.time;
            notesBlock.dynamic_list = new List<DynamicNote> { d };
            score.Add(notesBlock);
        }

        return true;
    }

    //初期化されたNotesBlockを返す
    private NotesBlock ReturnVoidNotesBlock()
    {
        return new NotesBlock 
        {
            time = 0, 
            general_list = new List<GeneralNote>(),
            hold_list = new List<HoldNote>(), 
            dynamic_list = new List<DynamicNote>() 
        };
    }

    //引数のtimeのNotesBlockが譜面データに存在した場合、そのIndexを返す
    private int ReturnExistTimeIndex(float time)
    {
        for (int i = 0; i < score.Count; i++){
            if (score[i].time == time) { return i; }
        }

        return -1;
    }

    //ホールドノート始点を通常ノートに変換する
    public GeneralNote ConvertHoldStarToGeneralNote(HoldNote h)
    {
        if (!h.isStart) //エラー
        {
            Debug.LogError("始点ではありません");
            return null;
        }

        GeneralNote g = new GeneralNote();
        g.judge_lane = Array.ConvertAll(h.judge_lane, f => (int)f); //判定レーン

        g.judge_time = h.judge_time; //判定時間
        g.time = h.time;             //時間
        return g;
    }

    //譜面データを返す
    public List<NotesBlock> ReturnScoreData()
    {
        return score;
    }

}
