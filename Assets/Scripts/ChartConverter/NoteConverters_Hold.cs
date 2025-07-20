using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{

    /// <summary>
    /// ホールドスタートノーツ
    /// </summary>
    public class HoldStartConverter : IHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.Hold) { return false; }
            if (noteDataInEditor is not IChainNoteData thisNote) { return false; }

            // 前後ノーツチェック
            var backNote = thisNote.NoteObject.BackNote.Value;
            var nextNote = thisNote.NoteObject.NextNote.Value;
            if (backNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldStartData == null)
            {
                dataOrigin.HoldStartData = new List<NoteDataOrigin_HoldStart>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldStart data = new NoteDataOrigin_HoldStart()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray(),
                HoldNumber = thisNote.ChainIndex.Value
            };

            dataOrigin.HoldStartData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor ,Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldStartData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.Hold);
                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToRange>> holdNumberToRanges)
        {
            if (dataOrigin.HoldStartData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldStartData)
            {
                if (holdNumberToRanges.TryGetValue(noteOrigin.HoldNumber, out var timeToRange)) 
                {
                    Debug.LogWarning($"【Converter】HoldStartの変換の際、既にHoldNumberが存在しました: {noteOrigin.HoldNumber}");
                    return false;
                }

                holdNumberToRanges.Add(noteOrigin.HoldNumber, new List<TimeToRange>() { new TimeToRange(timing, noteOrigin.Range.Select(x => (float)x).ToArray()) });

                NoteData_HoldStart noteData = new NoteData_HoldStart
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// ホールド中継ノーツ
    /// </summary>
    public class HoldRelayConverter : IHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.Hold) { return false; }
            if (noteDataInEditor is not IChainNoteData thisNote) { return false; }

            // 前後ノーツチェック
            var backNote = thisNote.NoteObject.BackNote.Value;
            var nextNote = thisNote.NoteObject.NextNote.Value;
            if (backNote == null || nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldRelayData == null)
            {
                dataOrigin.HoldRelayData = new List<NoteDataOrigin_HoldRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldRelay data = new NoteDataOrigin_HoldRelay()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray(),
                HoldNumber = thisNote.ChainIndex.Value
            };

            dataOrigin.HoldRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.Hold);
                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToRange>> holdNumberToRanges)
        {
            if (dataOrigin.HoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldRelayData)
            {
                if (!holdNumberToRanges.TryGetValue(noteOrigin.HoldNumber, out var timeToRange))
                {
                    Debug.LogWarning($"【Converter】HoldRelayの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                timeToRange.Add(new TimeToRange(timing, noteOrigin.Range.Select(x => (float)x).ToArray()));

                NoteData_HoldRelay noteData = new NoteData_HoldRelay
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    TimeToRanges = timeToRange,
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドメッシュ中継ノーツ
    /// </summary>
    public class HoldMeshRelayConverter : IHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.HoldHidden) { return false; }
            if (noteDataInEditor is not IChainNoteData thisNote) { return false; }

            // 前後ノーツチェック
            var backNote = thisNote.NoteObject.BackNote.Value;
            var nextNote = thisNote.NoteObject.NextNote.Value;
            if (backNote == null || nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldMeshRelayData == null)
            {
                dataOrigin.HoldMeshRelayData = new List<NoteDataOrigin_HoldMeshRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldMeshRelay data = new NoteDataOrigin_HoldMeshRelay()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray(),
                HoldNumber = thisNote.ChainIndex.Value
            };

            dataOrigin.HoldMeshRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.HoldMeshRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldMeshRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.HoldHidden);
                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToRange>> holdNumberToRanges)
        {
            if (dataOrigin.HoldMeshRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldMeshRelayData)
            {
                if (!holdNumberToRanges.TryGetValue(noteOrigin.HoldNumber, out var timeToRange))
                {
                    Debug.LogWarning($"【Converter】HoldMeshRelayの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                timeToRange.Add(new TimeToRange(timing, noteOrigin.Range.Select(x => (float)x).ToArray()));
            }

            return true;
        }
    }

    /// <summary>
    /// ホールドエンドノーツ
    /// </summary>
    public class HoldEndConverter : IHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.Hold) { return false; }
            if (noteDataInEditor is not IChainNoteData thisNote) { return false; }

            // 前後ノーツチェック
            var backNote = thisNote.NoteObject.BackNote.Value;
            var nextNote = thisNote.NoteObject.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldEndData == null)
            {
                dataOrigin.HoldEndData = new List<NoteDataOrigin_HoldEnd>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldEnd data = new NoteDataOrigin_HoldEnd()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray(),
                HoldNumber = thisNote.ChainIndex.Value
            };

            dataOrigin.HoldEndData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldEndData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.Hold);
                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToRange>> holdNumberToRanges)
        {
            if (dataOrigin.HoldEndData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldEndData)
            {
                if (!holdNumberToRanges.TryGetValue(noteOrigin.HoldNumber, out var timeToRange))
                {
                    Debug.LogWarning($"【Converter】HoldEndの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                timeToRange.Add(new TimeToRange(timing, noteOrigin.Range.Select(x => (float)x).ToArray()));

                NoteData_HoldEnd noteData = new NoteData_HoldEnd
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    TimeToRanges = timeToRange,
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// ホールドエンド(判定なし)ノーツ
    /// </summary>
    public class HoldEndUnjudgeConverter : IHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.HoldEndUnjudge) { return false; }
            if (noteDataInEditor is not IChainNoteData thisNote) { return false; }

            // 前後ノーツチェック
            var backNote = thisNote.NoteObject.BackNote.Value;
            var nextNote = thisNote.NoteObject.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.HoldEndUnjudgeData == null)
            {
                dataOrigin.HoldEndUnjudgeData = new List<NoteDataOrigin_HoldEndUnjudge>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_HoldEndUnjudge data = new NoteDataOrigin_HoldEndUnjudge()
            {
                Range = noteDataInEditor.Address.Range.Select(x => (int)x).ToArray(),
                HoldNumber = thisNote.ChainIndex.Value
            };

            dataOrigin.HoldEndUnjudgeData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.HoldEndUnjudgeData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.HoldEndUnjudgeData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_Hold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, noteDataOrigin.Range.Select(x => (float)x).ToList());

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.HoldEndUnjudge);
                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToRange>> holdNumberToRanges)
        {
            if (dataOrigin.HoldEndUnjudgeData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.HoldEndUnjudgeData)
            {
                if (!holdNumberToRanges.TryGetValue(noteOrigin.HoldNumber, out var timeToRange))
                {
                    Debug.LogWarning($"【Converter】HoldEndUnjudgeの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                timeToRange.Add(new TimeToRange(timing, noteOrigin.Range.Select(x => (float)x).ToArray()));

                NoteData_HoldEndUnjudge noteData = new NoteData_HoldEndUnjudge
                {
                    Range = (int[])noteOrigin.Range.Clone(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// ホールドメッシュ
    /// </summary>
    public class HoldMeshConverter : IUnchainDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToRange>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToRange>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            AddHoldStartData(dataOrigin, timing);
            AddHoldRelayData(dataOrigin, timing);
            AddHoldMeshRelayData(dataOrigin, timing);
            AddHoldEndData(dataOrigin, onAddNoteData, timing);
            AddHoldEndUnjudgeData(dataOrigin, onAddNoteData, timing);

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
                meshList.Add(new TimeToRange(timing,noteOrigin.Range.Select(x => (float)x).ToArray()));
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

                meshList.Add(new TimeToRange(timing,noteOrigin.Range.Select(x => (float)x).ToArray()));
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

                meshList.Add(new TimeToRange(timing,noteOrigin.Range.Select(x => (float)x).ToArray()));
            }

            return true;
        }

        private bool AddHoldEndData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
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

                meshList.Add(new TimeToRange(timing,noteOrigin.Range.Select(x => (float)x).ToArray()));
                onAddNoteData(GenerateNoteData_HoldMesh(meshList));
            }

            return true;
        }

        private bool AddHoldEndUnjudgeData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.HoldEndUnjudgeData == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin.HoldEndUnjudgeData)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToRange(timing,noteOrigin.Range.Select(x => (float)x).ToArray()));
                onAddNoteData(GenerateNoteData_HoldMesh(meshList));
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