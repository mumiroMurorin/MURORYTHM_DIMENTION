using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartDataOrigin
{
    public int MainBpm { get; set; }

}

public enum ChartTopicType
{
    SetDivisionNum, // 分割数(〇分音符)
    SetBPM,         // BPM変化
    SetBeat,        // 拍子変化

    // ノーツ
    Touch,
    HoldStart,
    HoldRelay,
    HoldEnd,
    HoldMesh,
    SpaceHoldMesh,
    SpaceHoldRelay,
    DynamicGroundUpward,
    DynamicGroundDownward,
    DynamicGroundRightward,
    DynamicGroundLeftward,
    DynamicSpace
}

/// <summary>
/// 各種ノーツデータのListをまとめたもの
/// </summary>
public class ChartData
{
    public int MaxCombo { get; set; }

    public List<NoteData_Touch> noteData_Touches { get; set; }

    public List<NoteData_HoldStart> noteData_HoldStarts { get; set; }

    public List<NoteData_HoldRelay> noteData_HoldRelays { get; set; }

    public List<NoteData_HoldEnd> noteData_HoldEnds { get; set; }

    public List<NoteData_HoldMesh> noteData_HoldMeshes { get; set; }



    public List<NoteData_DynamicGroundUpward> noteData_DynamicGroundUpwards { get; set; }

    public List<NoteData_DynamicGroundRightward> noteData_DynamicGroundRightwards { get; set; }

    public List<NoteData_DynamicGroundLeftward> noteData_DynamicGroundLeftwards { get; set; }

    public List<NoteData_DynamicGroundDownward> noteData_DynamicGroundDownwards { get; set; }

    public List<NoteData_SpaceHoldMesh> noteData_SpaceHoldMeshes { get; set; }

    public List<NoteData_SpaceHoldRelay> noteData_SpaceHoldRelays { get; set; }

    //public List<NoteData_DynamicSpace>
    //{ get; set; }
}