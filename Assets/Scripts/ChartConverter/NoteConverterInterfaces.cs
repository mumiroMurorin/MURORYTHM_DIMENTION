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
    public interface IUnchainDataToRhythmGameConvertable
    {
        bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing);
    }

    public interface IHoldDataToRhythmGameConvertable
    {
        bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing, Dictionary<int, List<TimeToRange>> numberToRanges);
    }

    public interface ISpaceHoldDataToRhythmGameConvertable
    {
        bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing);
    }

    public interface IUnchainedNoteConvertable
    {
        bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);

        bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData);
    }

    public interface IChainNoteConvertable
    {
        bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin);

        bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData);
    }
}
