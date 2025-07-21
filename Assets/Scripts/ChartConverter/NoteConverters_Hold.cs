using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using static JudgementUtil.Hold.HoldJudgement;
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

    /// <summary>
    /// ホールド判定点
    /// </summary>
    public class HoldJudgementPointConverter : IUnchainDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToDetail>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToDetail>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            AddHoldStartData(dataOrigin.HoldStartData, dataOrigin.Bpm, timing);
            AddHoldRelayData(dataOrigin.HoldRelayData, dataOrigin.Bpm, timing);
            AddHoldMeshRelayData(dataOrigin.HoldMeshRelayData, dataOrigin.Bpm, timing);
            AddHoldEndData(dataOrigin.HoldEndData, onAddNoteData, dataOrigin.Bpm, timing);
            AddHoldEndUnjudgeData(dataOrigin.HoldEndUnjudgeData, onAddNoteData, dataOrigin.Bpm, timing);

            return true;
        }

        private bool AddHoldStartData(List<NoteDataOrigin_HoldStart> dataOrigin, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            // 始点、メッシュデータ格納リストの作成
            foreach (var noteOrigin in dataOrigin)
            {
                // 一度ディクショナリーに格納
                List<TimeToDetail> meshList;
                // ディクショナリーに登録されていなければ新規作成
                if (numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList = new List<TimeToDetail>();
                meshList.Add(new TimeToDetail(timing, noteOrigin.Range.Select(x => (float)x).ToArray(), true, bpm));
                numberToHoldMeshDataOrigin.Add(noteOrigin.HoldNumber, meshList);
            }

            return true;
        }

        private bool AddHoldRelayData(List<NoteDataOrigin_HoldRelay> dataOrigin, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToDetail(timing, noteOrigin.Range.Select(x => (float)x).ToArray(), true, bpm));
            }

            return true;
        }

        private bool AddHoldMeshRelayData(List<NoteDataOrigin_HoldMeshRelay> dataOrigin, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されていなければ新規作成
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToDetail(timing, noteOrigin.Range.Select(x => (float)x).ToArray(), false, bpm));
            }

            return true;
        }

        private bool AddHoldEndData(List<NoteDataOrigin_HoldEnd> dataOrigin, Action<INoteData> onAddNoteData, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToDetail(timing, noteOrigin.Range.Select(x => (float)x).ToArray(), true, bpm));

                foreach(var note in GenerateNoteData_JudgementPoint(meshList))
                {
                    onAddNoteData(note);
                }
            }

            return true;
        }

        private bool AddHoldEndUnjudgeData(List<NoteDataOrigin_HoldEndUnjudge> dataOrigin, Action<INoteData> onAddNoteData, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToDetail(timing, noteOrigin.Range.Select(x => (float)x).ToArray(), true, bpm));

                foreach (var note in GenerateNoteData_JudgementPoint(meshList))
                {
                    onAddNoteData(note);
                }
            }

            return true;
        }

        /// <summary>
        /// List＜HoldMeshOriginAndTiming＞ → List<NoteData_HoldRelayHidden>
        /// </summary>
        /// <param name="meshDataList"></param>
        /// <returns></returns>
        private List<NoteData_HoldRelayHidden> GenerateNoteData_JudgementPoint(List<TimeToDetail> timeToDetails)
        {
            var noteDatas = new List<NoteData_HoldRelayHidden>();

            float interval = CalcInterval(timeToDetails[0].Bpm);
            float mergin = interval / 2f;
            int index = 0;

            for (float count = timeToDetails[0].Timing; count < timeToDetails[^1].Timing; count += interval)
            {
                var detail = timeToDetails[index];

                // インターバルの更新
                while (index + 1 < timeToDetails.Count && timeToDetails[index + 1].Timing < count)
                {
                    index++;
                    interval = CalcInterval(detail.Bpm);
                    mergin = interval / 2f;
                }

                // 近くに判定点があったら追加しない
                if (IsNearJudgementPoint(timeToDetails, count, mergin)) { continue; }

                // 判定点の追加
                noteDatas.Add(ConvertNoteData(timeToDetails, count));
            }

            return noteDatas;
        }

        private NoteData_HoldRelayHidden ConvertNoteData(List<TimeToDetail> details, float time)
        {
            var noteData = new NoteData_HoldRelayHidden();

            noteData.Timing = time;
            noteData.TimeToRanges = details.Select(x => x.ToTimeToRange()).ToList();
            noteData.Range = GetJudgeRange(noteData.TimeToRanges, time).ToArray();

            return noteData;
        }

        private bool IsNearJudgementPoint(List<TimeToDetail> details, float timing, float margin)
        {
            foreach(var detail in details)
            {
                if(Mathf.Abs(detail.Timing - timing) < margin && detail.IsJudgement) { return true; }
            }

            return false;
        }

        private float CalcInterval(float bpm)
        {
            // 8分間隔
            return 30f / bpm;
        }

        private class TimeToDetail
        {
            public TimeToDetail(float timing, float[] range, bool isJudgement, float bpm)
            {
                Timing = timing;
                Range = range;
                IsJudgement = isJudgement;
                Bpm = bpm;
            }

            public float Timing { get; set; }
            public float[] Range { get; set; }
            public float Bpm { get; set; }
            public bool IsJudgement { get; set; }
            public TimeToRange ToTimeToRange()
            {
                return new TimeToRange(this.Timing, this.Range);
            }
        }
    }
}