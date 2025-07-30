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
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.Touch) { return false; }

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

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.TouchNoteData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.TouchNoteData)
            {
                NoteData_Touch noteData = new NoteData_Touch
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.TouchNoteData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.TouchNoteData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Touch();

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());
                noteData.SetAddress(address);

                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// 神タッチノーツ
    /// </summary>
    public class DivineTouchNoteConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.DivineTouch) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DivineTouchData == null)
            {
                dataOrigin.DivineTouchData = new List<NoteDataOrigin_DivineTouch>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DivineTouch data = new NoteDataOrigin_DivineTouch()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DivineTouchData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.DivineTouchData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DivineTouchData)
            {
                NoteData_DivineTouch noteData = new NoteData_DivineTouch
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.DivineTouchData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DivineTouchData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Touch();

                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.DivineTouch);

                onAddNoteData(noteData);
            }

            return true;
        }
    }
}