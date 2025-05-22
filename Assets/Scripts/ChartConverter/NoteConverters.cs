using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    // ノーツ変換関数

    /// <summary>
    /// ChartEditor.NoteDataを変換してSubDivisionDataOriginにぶち込む
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

    /// <summary>
    /// タッチノーツ
    /// </summary>
    public class TouchNoteConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.TouchNote;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if(noteDataInEditor.NoteType != type) { return false; }

            // 新たにインスタンス化
            if(dataOrigin.TouchNoteData == null) 
            { 
                dataOrigin.TouchNoteData = new List<NoteDataOrigin_Touch>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_Touch data = new NoteDataOrigin_Touch()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.TouchNoteData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if(dataOrigin.TouchNoteData == null) { return true; }

            foreach(var noteOrigin in dataOrigin.TouchNoteData)
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

            foreach(var noteDataOrigin in dataOrigin.TouchNoteData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Touch();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ↑ダイナミック↑ノーツ
    /// </summary>
    public class DynamicUpwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundUpward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicUpwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicUpwardData)
            {
                NoteData_DynamicGroundUpward noteData = new NoteData_DynamicGroundUpward
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
            if (dataOrigin.DynamicUpwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicUpwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicUpward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ↓ダイナミック↓ノーツ
    /// </summary>
    public class DynamicDownwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundDownward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicDownwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.DynamicDownwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicDownwardData)
            {
                NoteData_DynamicGroundDownward noteData = new NoteData_DynamicGroundDownward
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
            if (dataOrigin.DynamicDownwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicDownwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicDownward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// →ダイナミック→ノーツ
    /// </summary>
    public class DynamicRightwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundRightward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicRightwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {

            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicRightwardData)
            {
                NoteData_DynamicGroundRightward noteData = new NoteData_DynamicGroundRightward
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
            if (dataOrigin.DynamicRightwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicRightwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicRightward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ←ダイナミック←ノーツ
    /// </summary>
    public class DynamicLeftwardConverter : IOriginDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.DynamicGroundLeftward;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
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
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray()
            };

            dataOrigin.DynamicLeftwardData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.DynamicLeftwardData)
            {
                NoteData_DynamicGroundLeftward noteData = new NoteData_DynamicGroundLeftward
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
            if (dataOrigin.DynamicLeftwardData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.DynamicLeftwardData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_DynamicLeftward();

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);

                Debug.Log(address.BarIndex);
                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());

                dataInChartEditor.AddNote(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドスタートノーツ
    /// </summary>
    public class HoldStartConverter : IOriginDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly DeploymentNoteType type = DeploymentNoteType.Hold;

        int currentHoldNumber = 0;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != type) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }

            IGroundChainNoteData backNote = ((IGroundChainNoteData)noteDataInEditor).BackNote.Value;
            IGroundChainNoteData nextNote = ((IGroundChainNoteData)noteDataInEditor).NextNote.Value;
            if (backNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }
            if (backNote != null && nextNote != null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldStartData == null)
            {
                dataOrigin.HoldStartData = new List<NoteDataOrigin_HoldStart>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldStart data = new NoteDataOrigin_HoldStart()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = currentHoldNumber
            };

            dataOrigin.HoldStartData.Add(data);
            nextNoteToNumber.Add(nextNote, currentHoldNumber++);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                NoteData_HoldStart noteData = new NoteData_HoldStart
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldStartData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // ディクショナリーへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber,out var startNote))
                {
                    Debug.LogWarning($"【Converter】HoldStartの変換の際、既にHoldNumberが存在しました: {noteDataOrigin.HoldNumber}");
                    return false;
                }
                else
                {
                    numberToStartNote.Add(noteDataOrigin.HoldNumber, chainData);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールド中継ノーツ
    /// </summary>
    public class HoldRelayConverter : IOriginDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.Hold) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }
            if (noteDataInEditor is not ITypeChangableNoteData) { return false; }

            IGroundChainNoteData thisNote = (IGroundChainNoteData)noteDataInEditor;
            IGroundChainNoteData backNote = thisNote.BackNote.Value;
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】HoldRelayの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                nextNoteToNumber.Remove(thisNote);
                nextNoteToNumber.Add(nextNote, number);
            }

            // 新たにインスタンス化
            if (dataOrigin.HoldRelayData == null) { dataOrigin.HoldRelayData = new List<NoteDataOrigin_HoldRelay>(); }

            // 追加するデータのインスタンス化
            DeploymentNoteType noteType = noteDataInEditor.NoteType;
            NoteDataOrigin_HoldRelay data = new NoteDataOrigin_HoldRelay()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = number
            };

            dataOrigin.HoldRelayData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldRelayData)
            {
                NoteData_HoldRelay noteData = new NoteData_HoldRelay
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldRelayData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;
                ((ITypeChangableNoteData)noteData).SetNoteType(DeploymentNoteType.Hold);

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData);
                }
                else
                {
                    Debug.LogWarning($"【Converter】HoldRelayの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドエンドノーツ
    /// </summary>
    public class HoldEndConverter : IOriginDataToRhythmGameConvertable, IChainNoteConvertable
    {
        const DeploymentNoteType NoteType = DeploymentNoteType.Hold;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != NoteType) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }

            IGroundChainNoteData thisNote = (IGroundChainNoteData)noteDataInEditor;
            IGroundChainNoteData backNote = thisNote.BackNote.Value;
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }
            if (backNote != null && nextNote != null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】HoldEndの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                // 終点なのでAddしない
                nextNoteToNumber.Remove(thisNote);
            }

            // 新たにインスタンス化
            if (dataOrigin.HoldEndData == null)
            {
                dataOrigin.HoldEndData = new List<NoteDataOrigin_HoldEnd>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldEnd data = new NoteDataOrigin_HoldEnd()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = number
            };

            dataOrigin.HoldEndData.Add(data);
            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldEndData)
            {
                NoteData_HoldEnd noteData = new NoteData_HoldEnd
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldEndData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData);
                }
                else
                {
                    Debug.LogWarning($"【Converter】HoldEndの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドメッシュ中継ノーツ
    /// </summary>
    public class HoldMeshRelayConverter : IChainNoteConvertable
    {
        const DeploymentNoteType NoteType = DeploymentNoteType.HoldHidden;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != NoteType) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }

            IGroundChainNoteData thisNote = (IGroundChainNoteData)noteDataInEditor;
            IGroundChainNoteData backNote = thisNote.BackNote.Value;
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】HoldMeshRelayの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                // 終点なのでAddしない
                nextNoteToNumber.Remove(thisNote);
            }

            // 新たにインスタンス化
            if (dataOrigin.HoldMeshRelayData == null)
            {
                dataOrigin.HoldMeshRelayData = new List<NoteDataOrigin_HoldMeshRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldMeshRelay data = new NoteDataOrigin_HoldMeshRelay()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = number
            };

            dataOrigin.HoldMeshRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldMeshRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldMeshRelayData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;
                ((ITypeChangableNoteData)noteData).SetNoteType(DeploymentNoteType.HoldHidden);

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData);
                }
                else
                {
                    Debug.LogWarning($"【Converter】HoldMeshRelayの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ホールド判定点
    /// </summary>
    public class HoldHiddenJudgedRelay : IChainNoteConvertable
    {
        const DeploymentNoteType NoteType = DeploymentNoteType.HoldHiddenJudged;

        public bool AddDataForOrigin(IGroundNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin, ref Dictionary<IGroundChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != NoteType) { return false; }
            if (noteDataInEditor is not IGroundChainNoteData) { return false; }

            IGroundChainNoteData thisNote = (IGroundChainNoteData)noteDataInEditor;
            IGroundChainNoteData backNote = thisNote.BackNote.Value;
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】HoldHiddenJudgedの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                // 終点なのでAddしない
                nextNoteToNumber.Remove(thisNote);
            }

            // 新たにインスタンス化
            if (dataOrigin.HoldHiddenJudgedRelayData == null)
            {
                dataOrigin.HoldHiddenJudgedRelayData = new List<NoteDataOrigin_HoldHiddenJudgedRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldHiddenJudgedRelay data = new NoteDataOrigin_HoldHiddenJudgedRelay()
            {
                Range = noteDataInEditor.Range.Select(x => (int)x).ToArray(),
                HoldNumber = number
            };

            dataOrigin.HoldHiddenJudgedRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor, ref Dictionary<int, IGroundChainNoteData> numberToStartNote)
        {
            if (dataOrigin.HoldHiddenJudgedRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldHiddenJudgedRelayData)
            {
                IGroundNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IGroundChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }

                // データのセット
                AddressInChart address = new AddressInChart(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range[0]);
                IGroundChainNoteData chainData = (IGroundChainNoteData)noteData;
                ((ITypeChangableNoteData)noteData).SetNoteType(DeploymentNoteType.HoldHiddenJudged);

                noteData.SetAddress(address);
                noteData.SetRange(noteDataOrigin.Range.Select(x => (float)x).ToList());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData);
                }
                else
                {
                    Debug.LogWarning($"【Converter】HoldHiddenJudgedの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }

    }

    /// <summary>
    /// ホールドメッシュ
    /// </summary>
    public class HoldMeshConverter : IOriginDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToRange>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToRange>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            AddHoldStartData(dataOrigin, timing);
            AddHoldRelayData(dataOrigin, timing);
            AddHoldMeshRelayData(dataOrigin, timing);
            AddHoldHiddenJudgedRelay(dataOrigin, timing);
            AddHoldEndData(dataOrigin, chartData, timing);

            return true;
        }

        private bool AddHoldStartData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            // 始点、メッシュデータ格納リストの作成
            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                // 一度ディクショナリーに格納
                List<TimeToRange> meshList;
                // ディクショナリーに登録されていなければ新規作成
                if (numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList = new List<TimeToRange>();
                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
                numberToHoldMeshDataOrigin.Add(noteOrigin.HoldNumber, meshList);
            }

            return true;
        }

        private bool AddHoldRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldRelayData)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldMeshRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.HoldMeshRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldMeshRelayData)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldHiddenJudgedRelay(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.HoldHiddenJudgedRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldHiddenJudgedRelayData)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldEndData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin.HoldEndData)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToRange { Range = noteOrigin.Range.Select(x => (float)x).ToArray(), Timing = timing });
                chartData.AddNoteData(GenerateNoteData_HoldMesh(meshList));
            }

            return true;
        }

        /// <summary>
        /// List＜HoldMeshOriginAndTiming＞ → NoteData_HoldMesh
        /// </summary>
        /// <param name="meshDataList"></param>
        /// <returns></returns>
        private NoteData_HoldMesh GenerateNoteData_HoldMesh(List<TimeToRange> timeToRanges)
        {
            var noteData = new NoteData_HoldMesh();

            noteData.Timing = timeToRanges[0].Timing;
            noteData.TimeToRanges = timeToRanges;

            return noteData;
        }
    }

}
