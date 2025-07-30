using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{

    /// <summary>
    /// ↑ダイナミック↑ノーツ
    /// </summary>
    public class DynamicUpwardConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundUpward;

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicUpwardData == null)
            {
                dataOrigin.DynamicUpwardData = new List<NoteDataOrigin_DynamicUpward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicUpward data = new NoteDataOrigin_DynamicUpward()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicUpwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicUpwardData)
            {
                NoteData_DynamicGroundUpward noteData = new NoteData_DynamicGroundUpward
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
            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicUpwardData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_DynamicUpward();

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());
                noteData.SetAddress(address);

                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ↓ダイナミック↓ノーツ
    /// </summary>
    public class DynamicDownwardConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundDownward;

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicDownwardData == null)
            {
                dataOrigin.DynamicDownwardData = new List<NoteDataOrigin_DynamicDownward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicDownward data = new NoteDataOrigin_DynamicDownward()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicDownwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.DynamicDownwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicDownwardData)
            {
                NoteData_DynamicGroundDownward noteData = new NoteData_DynamicGroundDownward
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
            if (dataOrigin.DynamicDownwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicDownwardData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_DynamicDownward();

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                noteData.SetAddress(address);

                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// →ダイナミック→ノーツ
    /// </summary>
    public class DynamicRightwardConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundRightward;

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicRightwardData == null)
            {
                dataOrigin.DynamicRightwardData = new List<NoteDataOrigin_DynamicRightward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicRightward data = new NoteDataOrigin_DynamicRightward()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicRightwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {

            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicRightwardData)
            {
                NoteData_DynamicGroundRightward noteData = new NoteData_DynamicGroundRightward
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
            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicRightwardData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_DynamicRightward();

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                noteData.SetAddress(address);

                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ←ダイナミック←ノーツ
    /// </summary>
    public class DynamicLeftwardConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundLeftward;

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.DynamicLeftwardData == null)
            {
                dataOrigin.DynamicLeftwardData = new List<NoteDataOrigin_DynamicLeftward>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_DynamicLeftward data = new NoteDataOrigin_DynamicLeftward()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicLeftwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicLeftwardData)
            {
                NoteData_DynamicGroundLeftward noteData = new NoteData_DynamicGroundLeftward
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
            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicLeftwardData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_DynamicLeftward();

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                noteData.SetAddress(address);

                onAddNoteData(noteData);
            }

            return true;
        }
    }
}