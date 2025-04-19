using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 各種ノーツデータのListをまとめたもの
/// </summary>
public class ChartData
{
    public int NoteNum 
    { 
        get { 
            return noteData_Touches.Count + noteData_HoldStarts.Count + noteData_HoldRelays.Count +
                noteData_HoldEnds.Count + noteData_HoldMeshes.Count + noteData_DynamicGroundUpwards.Count +
                noteData_DynamicGroundRightwards.Count + noteData_DynamicGroundLeftwards.Count + noteData_DynamicGroundDownwards.Count +
                noteData_SpaceHoldMeshes.Count + noteData_SpaceHoldRelays.Count;
        }
    }

    public int MaxCombo { 
        get {
            return noteData_Touches.Count + noteData_HoldStarts.Count + noteData_HoldRelays.Count +
                noteData_HoldEnds.Count + noteData_DynamicGroundUpwards.Count +
                noteData_DynamicGroundRightwards.Count + noteData_DynamicGroundLeftwards.Count + noteData_DynamicGroundDownwards.Count +
                noteData_SpaceHoldRelays.Count;
        }
    }

    public List<NoteData_Touch> noteData_Touches { get; set; } = new List<NoteData_Touch>();

    public List<NoteData_HoldStart> noteData_HoldStarts { get; set; } = new List<NoteData_HoldStart>();

    public List<NoteData_HoldRelay> noteData_HoldRelays { get; set; } = new List<NoteData_HoldRelay>();

    public List<NoteData_HoldEnd> noteData_HoldEnds { get; set; } = new List<NoteData_HoldEnd>();

    public List<NoteData_HoldMesh> noteData_HoldMeshes { get; set; } = new List<NoteData_HoldMesh>();



    public List<NoteData_DynamicGroundUpward> noteData_DynamicGroundUpwards { get; set; } = new List<NoteData_DynamicGroundUpward>();

    public List<NoteData_DynamicGroundRightward> noteData_DynamicGroundRightwards { get; set; } = new List<NoteData_DynamicGroundRightward>();

    public List<NoteData_DynamicGroundLeftward> noteData_DynamicGroundLeftwards { get; set; } = new List<NoteData_DynamicGroundLeftward>();

    public List<NoteData_DynamicGroundDownward> noteData_DynamicGroundDownwards { get; set; } = new List<NoteData_DynamicGroundDownward>();

    public List<NoteData_SpaceHoldMesh> noteData_SpaceHoldMeshes { get; set; } = new List<NoteData_SpaceHoldMesh>();

    public List<NoteData_SpaceHoldRelay> noteData_SpaceHoldRelays { get; set; } = new List<NoteData_SpaceHoldRelay>();

    //public List<NoteData_DynamicSpace>
    //{ get; set; }
}