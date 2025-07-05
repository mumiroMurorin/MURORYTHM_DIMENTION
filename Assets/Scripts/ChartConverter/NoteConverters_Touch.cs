using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    /// <summary>
    /// タッチノーツ
    /// </summary>
    public class TouchNoteConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.TouchNote;

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.TouchNoteData == null)
            {
                dataOrigin.TouchNoteData = new List<NoteDataOrigin_Touch>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_Touch data = new NoteDataOrigin_Touch()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.TouchNoteData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.TouchNoteData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.TouchNoteData)
            {
                NoteData_Touch noteData = new NoteData_Touch
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor)
        {
            if (dataOrigin.TouchNoteData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.TouchNoteData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Touch();

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());
                noteData.SetAddress(address);

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

}