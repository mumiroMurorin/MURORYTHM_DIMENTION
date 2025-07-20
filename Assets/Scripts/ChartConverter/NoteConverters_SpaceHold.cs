using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    /// <summary>
    /// スペースホールド始点(中継点データに変換)
    /// </summary>
    public class SpaceHoldStartConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldStartData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHold) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (backNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldStartData == null)
            {
                dataOrigin.SpaceHoldStartData = new List<NoteDataOrigin_SpaceHoldStart>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldStart data = new NoteDataOrigin_SpaceHoldStart()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldStartData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldStartData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// スペースホールド中継点
    /// </summary>
    public class SpaceHoldRelayConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldRelayData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHold) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (backNote == null || nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldRelayData == null)
            {
                dataOrigin.SpaceHoldRelayData = new List<NoteDataOrigin_SpaceHoldRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldRelay data = new NoteDataOrigin_SpaceHoldRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// スペースホールドメッシュ中継ノーツ
    /// </summary>
    public class SpaceHoldMeshRelayConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            // 特に処理なし
            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldHidden) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (backNote == null || nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldMeshRelayData == null)
            {
                dataOrigin.SpaceHoldMeshRelayData = new List<NoteDataOrigin_SpaceHoldMeshRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldMeshRelay data = new NoteDataOrigin_SpaceHoldMeshRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldMeshRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                typeChangableData.SetNoteType(DeploymentNoteType.SpaceHoldHidden);
                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// スペースホールド終点(中継点データに変換)
    /// </summary>
    public class SpaceHoldEndConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldEndData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHold) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }
            
            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldEndData == null)
            {
                dataOrigin.SpaceHoldEndData = new List<NoteDataOrigin_SpaceHoldEnd>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldEnd data = new NoteDataOrigin_SpaceHoldEnd()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldEndData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldEndData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// スペースホールドメッシュ
    /// </summary>
    public class SpaceHoldMeshConverter : IUnchainDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToVertices>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToVertices>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            AddHoldStartData(dataOrigin, timing);
            AddHoldRelayData(dataOrigin, timing);
            AddHoldMeshRelayData(dataOrigin, timing);
            AddHoldEndData(dataOrigin, onAddNoteData, timing);

            return true;
        }

        private bool AddHoldStartData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            // 始点、メッシュデータ格納リストの作成
            foreach (var noteOrigin in dataOrigin.SpaceHoldStartData)
            {
                // 一度ディクショナリーに格納
                // ディクショナリーに登録されていなければ新規作成
                if (numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList = new List<TimeToVertices>();
                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
                numberToHoldMeshDataOrigin.Add(noteOrigin.HoldNumber, meshList);
            }

            return true;
        }

        private bool AddHoldRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldRelayData)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldMeshRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldEndData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin.SpaceHoldEndData)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
                onAddNoteData(GenerateNoteData_SpaceHoldMesh(meshList));
            }

            return true;
        }

        /// <summary>
        /// List＜HoldMeshOriginAndTiming＞ → NoteData_HoldMesh
        /// </summary>
        /// <param name="meshDataList"></param>
        /// <returns></returns>
        private NoteData_SpaceHoldMesh GenerateNoteData_SpaceHoldMesh(List<TimeToVertices> timeToVertices)
        {
            var noteData = new NoteData_SpaceHoldMesh();

            noteData.Timing = timeToVertices[0].Timing;
            noteData.TimeToVertices = timeToVertices;

            return noteData;
        }
    }
}