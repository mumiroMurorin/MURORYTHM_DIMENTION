using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// 各種ノーツデータのListをまとめたもの
/// </summary>
public class ChartData
{
    public int NoteNum { get; private set; } = 0;

    public int MaxCombo { get; private set; } = 0;

    /// <summary>
    /// 全てのノーツ(リスト)を纏めたプロパティ
    /// </summary>
    List<List<INoteData>> allNoteDataLists = new List<List<INoteData>>();

    public IEnumerable<IEnumerable<INoteData>> AllNoteDataLists { get { return allNoteDataLists; } }

    /// <summary>
    /// ソフランデータ
    /// </summary>
    public PositionGraph PositionGraph { get; } = new();

    /// <summary>
    /// ノーツデータの追加
    /// </summary>
    /// <param name="noteData"></param>
    public void AddNoteData(INoteData noteData)
    {
        if (noteData is IJudgableNoteData) { MaxCombo++; }
        else if (noteData is IClippedJudgableNote) { MaxCombo++; }

        NoteNum++;

        // 引数のデータのノーツリストが存在するか確認
        foreach(var noteList in allNoteDataLists)
        {
            // 存在する場合は追加して終了
            if (noteList.Count > 0 && noteList[0].NoteType == noteData.NoteType)
            {
                noteList.Add(noteData);
                return;
            }
        }

        // 存在しない場合は新しくListを作って追加
        allNoteDataLists.Add(new List<INoteData>() { noteData });
    }

    /// <summary>
    /// スピード倍率データの追加
    /// </summary>
    /// <param name="speedRatioData"></param>
    public void AddSpeedRatioData(SpeedRatioData speedRatioData)
    {
        PositionGraph.AddSegment(speedRatioData.Timing, speedRatioData.Ratio);
    }

    /// <summary>
    /// ノーツリストを返す
    /// </summary>
    /// <param name="noteType"></param>
    /// <returns></returns>
    public IEnumerable<INoteData> GetNoteDataList(NoteType noteType)
    {
        // 引数のデータのノーツリストが存在するか確認
        foreach (var noteList in allNoteDataLists)
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

    public IEnumerable<T> GetNoteDataList<T>(T noteData) where T : INoteData
    {
        // 引数のデータのノーツリストが存在するか確認
        foreach (var noteList in allNoteDataLists)
        {
            // 存在する場合は追加して終了
            if (noteList.Count > 0 && noteList[0].GetType() == noteData.GetType())
            {
                return noteList.Select(x => (T)x).ToList();
            }
        }

        // 存在しない場合は空のリストを返す
        return null;
    }
}