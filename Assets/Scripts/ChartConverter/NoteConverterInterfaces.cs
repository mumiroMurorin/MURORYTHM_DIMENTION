using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    // ÉmÅ[Écïœä∑ä÷êî

    /// <summary>
    /// ChartEditor.NoteDataÇïœä∑ÇµÇƒSubDivisionDataOriginÇ…Ç‘ÇøçûÇﬁ
    /// </summary>
    public interface IOriginDataToRhythmGameConvertable
    {
        bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing);
    }

    public interface IUnchainedNoteConvertable
    {
        bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);

        bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor);
    }

    public interface IChainNoteConvertable
    {
        bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, 
            ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber);

        bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,
            ref Dictionary<int, IGroundChainNoteData> numberToStartNote);
    }
}
