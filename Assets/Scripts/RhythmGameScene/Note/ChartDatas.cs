using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 各種ノーツデータのListをまとめたもの
/// </summary>
public class ChartData
{
    public int NoteNum 
    { 
        get
        {
            int count = 0;
            foreach (var noteList in AllNoteDataLists)
            {
                count += noteList.Count;
            }

            return count;
        }
    }

    public int MaxCombo { 
        get {
            int count = 0;
            foreach (var noteList in AllNoteDataLists) 
            {
                if (noteList.Count > 0 && noteList[0].JudgementType != JudgementType.None)
                {
                    count += noteList.Count;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// 全てのノーツ(リスト)を纏めたプロパティ
    /// </summary>
    private List<List<INoteData>> AllNoteDataLists = new List<List<INoteData>>();

    /// <summary>
    /// ノーツデータの追加
    /// </summary>
    /// <param name="noteData"></param>
    public void AddNoteData(INoteData noteData)
    {
        // 引数のデータのノーツリストが存在するか確認
        foreach(var noteList in AllNoteDataLists)
        {
            // 存在する場合は追加して終了
            if (noteList.Count > 0 && noteList[0].NoteType == noteData.NoteType)
            {
                noteList.Add(noteData);
                return;
            }
        }

        // 存在しない場合は新しくListを作って追加
        AllNoteDataLists.Add(new List<INoteData>() { noteData });
    }

    /// <summary>
    /// ノーツリストを返す
    /// </summary>
    /// <param name="noteType"></param>
    /// <returns></returns>
    public List<INoteData> GetNoteDataList(NoteType noteType)
    {
        // 引数のデータのノーツリストが存在するか確認
        foreach (var noteList in AllNoteDataLists)
        {
            // 存在する場合は追加して終了
            if (noteList.Count > 0 && noteList[0].NoteType == noteType)
            {
                return noteList;
            }
        }

        // 存在しない場合は空のリストを返す
        return new List<INoteData>();
    }

}